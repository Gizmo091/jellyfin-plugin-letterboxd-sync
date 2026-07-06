using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LetterboxdSync;

/// <summary>
/// Thin client for the official Letterboxd REST API (api.letterboxd.com).
/// Replaces the old HTML-scraping implementation which was blocked by Cloudflare.
/// See docs/LETTERBOXD_API.md for protocol details and how to re-extract the app key if it rotates.
/// </summary>
public class LetterboxdApi
{
    // First-party application credentials, extracted from the official Letterboxd Android app.
    // The API requires every request to be signed with these (apikey + HMAC-SHA256 signature).
    // If these stop working (401 "invalid API key or computed signature"), see docs/LETTERBOXD_API.md §3.
    private const string ApiKey = "ebe3d27ec52a35fc8d1835c6531c37bd72b7a54337666d5bd759379b72ae16f0";
    private const string BaseUrl = "https://api.letterboxd.com/api/v0";

    private static readonly byte[] ApiSecret = Encoding.ASCII.GetBytes("c60ce045d25bc90cb56026a8dd621eebeef995cbecc51951192da75348c977cd");
    private static readonly HttpClient Client = CreateClient();

    private readonly ILogger? _logger;

    private string? _accessToken;
    private string? _refreshToken;

    public LetterboxdApi(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>Gets the current refresh token (populated after authentication). Persist it to avoid re-login.</summary>
    public string? RefreshToken => _refreshToken;

    /// <summary>Gets a value indicating whether a member access token is available.</summary>
    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);

    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            CheckCertificateRevocationList = true,
        };

        var client = new HttpClient(handler);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Letterboxd-JellyfinSync/2.0");
        return client;
    }

    // ===== Authentication ========================================================================

    /// <summary>Authenticate with Letterboxd username + password (OAuth2 password grant).</summary>
    public Task AuthenticateWithPassword(string username, string password)
    {
        var form = $"grant_type=password&username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}";
        return RequestTokenAsync(form);
    }

    /// <summary>Exchange a stored refresh token for a fresh access token (OAuth2 refresh grant).</summary>
    public Task AuthenticateWithRefreshToken(string refreshToken)
    {
        var form = $"grant_type=refresh_token&refresh_token={Uri.EscapeDataString(refreshToken)}";
        return RequestTokenAsync(form);
    }

    private async Task RequestTokenAsync(string form)
    {
        using var content = new StringContent(form, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        var (status, body) = await SendAsync(HttpMethod.Post, "/auth/token", null, content, form, auth: false).ConfigureAwait(false);

        if (status != HttpStatusCode.OK)
        {
            throw new LetterboxdApiException($"Letterboxd authentication failed ({(int)status}). {ExtractError(body)}");
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        _accessToken = root.TryGetProperty("access_token", out var at) ? at.GetString() : null;
        if (root.TryGetProperty("refresh_token", out var rt))
        {
            _refreshToken = rt.GetString();
        }

        if (string.IsNullOrEmpty(_accessToken))
        {
            throw new LetterboxdApiException("Letterboxd authentication succeeded but returned no access token.");
        }
    }

    // ===== Films =================================================================================

    /// <summary>Resolve a TMDB id to a Letterboxd film. Returns null if no film matches.</summary>
    public async Task<FilmResult?> SearchFilmByTmdbId(int tmdbId)
    {
        var query = new List<KeyValuePair<string, string>>
        {
            new("filmId", $"tmdb:{tmdbId.ToString(CultureInfo.InvariantCulture)}"),
            new("perPage", "1"),
        };

        var (status, body) = await SendAsync(HttpMethod.Get, "/films", query, null, string.Empty, auth: IsAuthenticated).ConfigureAwait(false);

        if (status != HttpStatusCode.OK)
        {
            throw new LetterboxdApiException($"Letterboxd film lookup failed ({(int)status}) for tmdb:{tmdbId}. {ExtractError(body)}");
        }

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("items", out var items) || items.GetArrayLength() == 0)
        {
            return null;
        }

        return ParseFilmSummary(items[0], tmdbId.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Create a diary entry (mark a film as watched) via POST /log-entries.
    /// The API is idempotent: a 204 response means the entry already existed.
    /// Returns the created log entry LID, or null if the entry already existed (204).
    /// </summary>
    public async Task<string?> MarkAsWatched(string filmId, DateTime? date, string[]? tags, bool liked = false, double rating = 0)
    {
        if (!IsAuthenticated)
        {
            throw new LetterboxdApiException("Cannot log a film without an authenticated member (call AuthenticateWith* first).");
        }

        var payload = new Dictionary<string, object?>
        {
            ["filmId"] = filmId,
        };

        if (date != null)
        {
            payload["diaryDetails"] = new Dictionary<string, object?>
            {
                ["diaryDate"] = date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["rewatch"] = false,
            };
        }

        if (liked)
        {
            payload["like"] = true;
        }

        if (rating >= 0.5)
        {
            payload["rating"] = rating;
        }

        var cleanTags = tags?.Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
        if (cleanTags is { Length: > 0 })
        {
            payload["tags"] = cleanTags;
        }

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var (status, body) = await SendAsync(HttpMethod.Post, "/log-entries", null, content, json, auth: true).ConfigureAwait(false);

        // 201 Created = logged (body is the new LogEntry); 204 No Content = already logged (idempotent).
        if (status == HttpStatusCode.Created)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                return doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        if (status == HttpStatusCode.NoContent)
        {
            return null;
        }

        throw new LetterboxdApiException($"Letterboxd log-entry failed ({(int)status}). {ExtractError(body)}");
    }

    /// <summary>Delete a log entry by its LID (used for cleanup in integration tests).</summary>
    public async Task DeleteLogEntry(string logEntryId)
    {
        if (!IsAuthenticated)
        {
            throw new LetterboxdApiException("Cannot delete a log entry without an authenticated member.");
        }

        var (status, body) = await SendAsync(HttpMethod.Delete, $"/log-entry/{logEntryId}", null, null, string.Empty, auth: true).ConfigureAwait(false);

        if (status is HttpStatusCode.OK or HttpStatusCode.NoContent)
        {
            return;
        }

        throw new LetterboxdApiException($"Letterboxd delete log-entry failed ({(int)status}). {ExtractError(body)}");
    }

    // ===== Watchlists / lists ====================================================================

    /// <summary>Get all films on a member's public watchlist (paginated), as film results carrying TMDB ids.</summary>
    public Task<List<FilmResult>> GetWatchlist(string memberId)
        => GetPagedFilms($"/member/{memberId}/watchlist", film => film);

    /// <summary>Get all films in a list (paginated). List entries wrap a film summary.</summary>
    public Task<List<FilmResult>> GetListEntries(string listId)
        => GetPagedFilms($"/list/{listId}/entries", entry => entry.TryGetProperty("film", out var film) ? film : entry);

    private async Task<List<FilmResult>> GetPagedFilms(string path, Func<JsonElement, JsonElement> filmSelector)
    {
        var results = new List<FilmResult>();
        string? cursor = null;

        do
        {
            var query = new List<KeyValuePair<string, string>> { new("perPage", "100") };
            if (!string.IsNullOrEmpty(cursor))
            {
                query.Add(new("cursor", cursor));
            }

            var (status, body) = await SendAsync(HttpMethod.Get, path, query, null, string.Empty, auth: IsAuthenticated).ConfigureAwait(false);

            if (status != HttpStatusCode.OK)
            {
                throw new LetterboxdApiException($"Letterboxd list read failed ({(int)status}) for {path}. {ExtractError(body)}");
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var film = filmSelector(item);
                    if (film.ValueKind == JsonValueKind.Object)
                    {
                        results.Add(ParseFilmSummary(film));
                    }
                }
            }

            cursor = root.TryGetProperty("next", out var next) && next.ValueKind == JsonValueKind.String ? next.GetString() : null;

            if (!string.IsNullOrEmpty(cursor))
            {
                await Task.Delay(500 + Random.Shared.Next(500)).ConfigureAwait(false);
            }
        }
        while (!string.IsNullOrEmpty(cursor));

        return results;
    }

    /// <summary>Resolve a Letterboxd username to its member LID via search. Returns null if not found.</summary>
    public async Task<string?> ResolveMemberId(string username)
    {
        var item = await SearchFirst(username, "MemberSearchItem").ConfigureAwait(false);
        if (item == null)
        {
            return null;
        }

        return item.Value.TryGetProperty("member", out var member) && member.TryGetProperty("id", out var id)
            ? id.GetString()
            : null;
    }

    /// <summary>Resolve a username + list slug to a list LID via search. Best-effort. Returns null if not found.</summary>
    public async Task<string?> ResolveListId(string username, string listSlug)
    {
        var expectedPath = $"/{username}/list/{listSlug}/".ToLowerInvariant();
        var searchTerm = listSlug.Replace('-', ' ');

        var query = new List<KeyValuePair<string, string>>
        {
            new("input", searchTerm),
            new("searchMethod", "Autocomplete"),
            new("include", "ListSearchItem"),
            new("perPage", "10"),
        };

        var (status, body) = await SendAsync(HttpMethod.Get, "/search", query, null, string.Empty, auth: IsAuthenticated).ConfigureAwait(false);
        if (status != HttpStatusCode.OK)
        {
            return null;
        }

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("items", out var items))
        {
            return null;
        }

        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("list", out var list))
            {
                continue;
            }

            // Match by the list's canonical Letterboxd URL path (/{username}/list/{slug}/).
            if (list.TryGetProperty("links", out var links))
            {
                foreach (var link in links.EnumerateArray())
                {
                    if (link.TryGetProperty("type", out var t) && t.GetString() == "letterboxd"
                        && link.TryGetProperty("url", out var urlEl)
                        && Uri.TryCreate(urlEl.GetString(), UriKind.Absolute, out var url)
                        && url.AbsolutePath.ToLowerInvariant().TrimEnd('/').Equals(expectedPath.TrimEnd('/'), StringComparison.Ordinal))
                    {
                        return list.TryGetProperty("id", out var id) ? id.GetString() : null;
                    }
                }
            }
        }

        return null;
    }

    private async Task<JsonElement?> SearchFirst(string input, string include)
    {
        var query = new List<KeyValuePair<string, string>>
        {
            new("input", input),
            new("searchMethod", "Autocomplete"),
            new("include", include),
            new("perPage", "5"),
        };

        var (status, body) = await SendAsync(HttpMethod.Get, "/search", query, null, string.Empty, auth: IsAuthenticated).ConfigureAwait(false);
        if (status != HttpStatusCode.OK)
        {
            return null;
        }

        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
        {
            // Clone so the element stays valid after the JsonDocument is disposed.
            return items[0].Clone();
        }

        return null;
    }

    // ===== Watchlist input parsing (shared with the watchlist sync task) =========================

    /// <summary>
    /// Resolves a watchlist input (plain username, full URL, short boxd.it URL, or list URL) to a target.
    /// </summary>
    public static async Task<WatchlistTarget> ResolveWatchlistInput(string input)
    {
        input = input.Trim();

        // Plain username — no dots or slashes.
        if (!input.Contains('/', StringComparison.Ordinal) && !input.Contains('.', StringComparison.Ordinal))
        {
            return new WatchlistTarget($"{input}'s Watchlist", input, null);
        }

        if (!input.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            input = "https://" + input;
        }

        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            return new WatchlistTarget($"{input}'s Watchlist", input, null);
        }

        // Short URL (boxd.it) — read the Location header without following it (no Cloudflare on boxd.it).
        if (uri.Host.Equals("boxd.it", StringComparison.OrdinalIgnoreCase))
        {
            using var redirectHandler = new HttpClientHandler { AllowAutoRedirect = false, CheckCertificateRevocationList = true };
            using var httpClient = new HttpClient(redirectHandler);
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Letterboxd-JellyfinSync/2.0");
            using var response = await httpClient.GetAsync(uri).ConfigureAwait(false);
            var location = response.Headers.Location;
            if (location != null)
            {
                uri = location.IsAbsoluteUri ? location : new Uri(uri, location);
            }
        }

        if (uri.Host.Contains("letterboxd.com", StringComparison.OrdinalIgnoreCase))
        {
            var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

            // List URL: /username/list/list-slug/
            if (segments.Length >= 3 && segments[1].Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                return new WatchlistTarget($"{segments[0]} - {segments[2]}", segments[0], segments[2]);
            }

            // Watchlist or profile URL: /username/ or /username/watchlist/
            if (segments.Length > 0 && !string.IsNullOrEmpty(segments[0]))
            {
                return new WatchlistTarget($"{segments[0]}'s Watchlist", segments[0], null);
            }
        }

        return new WatchlistTarget($"{input}'s Watchlist", input, null);
    }

    // ===== Low-level plumbing ====================================================================

    private static FilmResult ParseFilmSummary(JsonElement film, string? tmdbFallback = null)
    {
        var lid = film.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? string.Empty : string.Empty;
        var slug = string.Empty;
        var tmdb = tmdbFallback;

        if (film.TryGetProperty("links", out var links))
        {
            foreach (var link in links.EnumerateArray())
            {
                var type = link.TryGetProperty("type", out var t) ? t.GetString() : null;
                if (type == "tmdb" && link.TryGetProperty("id", out var tmdbEl))
                {
                    tmdb = tmdbEl.GetString();
                }
                else if (type == "letterboxd" && link.TryGetProperty("url", out var urlEl))
                {
                    slug = ExtractFilmSlug(urlEl.GetString());
                }
            }
        }

        return new FilmResult(slug, lid) { TmdbId = tmdb };
    }

    private static string ExtractFilmSlug(string? letterboxdUrl)
    {
        if (string.IsNullOrEmpty(letterboxdUrl) || !Uri.TryCreate(letterboxdUrl, UriKind.Absolute, out var uri))
        {
            return string.Empty;
        }

        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 2 && segments[0].Equals("film", StringComparison.OrdinalIgnoreCase) ? segments[1] : string.Empty;
    }

    private async Task<(HttpStatusCode Status, string Body)> SendAsync(
        HttpMethod method,
        string path,
        IReadOnlyList<KeyValuePair<string, string>>? query,
        HttpContent? content,
        string bodyForSignature,
        bool auth)
    {
        var url = BuildSignedUrl(method.Method, path, query, bodyForSignature);

        using var request = new HttpRequestMessage(method, url);
        request.Headers.Accept.ParseAdd("application/json");

        if (auth && !string.IsNullOrEmpty(_accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }

        if (content != null)
        {
            request.Content = content;
        }

        using var response = await Client.SendAsync(request).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return (response.StatusCode, body);
    }

    /// <summary>
    /// Builds a fully-signed request URL. The signature is HMAC-SHA256 over
    /// METHOD + \0 + URL + \0 + BODY, keyed by the API secret, appended as the `signature` query param.
    /// </summary>
    private static string BuildSignedUrl(string method, string path, IReadOnlyList<KeyValuePair<string, string>>? query, string body)
    {
        var parameters = new List<KeyValuePair<string, string>>();
        if (query != null)
        {
            parameters.AddRange(query);
        }

        parameters.Add(new("apikey", ApiKey));
        parameters.Add(new("nonce", Guid.NewGuid().ToString()));
        parameters.Add(new("timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)));

        var queryString = new StringBuilder();
        foreach (var kv in parameters)
        {
            if (queryString.Length > 0)
            {
                queryString.Append('&');
            }

            queryString.Append(Uri.EscapeDataString(kv.Key)).Append('=').Append(Uri.EscapeDataString(kv.Value));
        }

        var url = $"{BaseUrl}{path}?{queryString}";
        var signingBase = $"{method.ToUpperInvariant()}\0{url}\0{body ?? string.Empty}";

        using var hmac = new HMACSHA256(ApiSecret);
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(signingBase))).ToLowerInvariant();

        return $"{url}&signature={signature}";
    }

    private static string ExtractError(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        // Letterboxd error bodies are usually { "message": "...", "type": "..." } or { "messages": [...] }.
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
            {
                return m.GetString() ?? string.Empty;
            }

            if (root.TryGetProperty("messages", out var ms) && ms.ValueKind == JsonValueKind.Array)
            {
                return string.Join(" ", ms.EnumerateArray().Select(x => x.GetString()));
            }
        }
        catch (JsonException)
        {
            // Fall through to raw preview.
        }

        return body.Length > 300 ? body.Substring(0, 300) : body;
    }
}
