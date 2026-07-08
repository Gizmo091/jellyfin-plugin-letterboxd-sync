using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LetterboxdSync;

/// <summary>Outcome of a Seerr media request.</summary>
public enum SeerrRequestOutcome
{
    /// <summary>A new request was created.</summary>
    Created,

    /// <summary>The film was already requested or already available; nothing to do.</summary>
    AlreadyExists,

    /// <summary>The request failed (permission, quota, network, …); see the logs.</summary>
    Failed,
}

/// <summary>
/// Minimal client for a Seerr / Jellyseerr / Overseerr instance. Resolves a Jellyfin user to its
/// Seerr account and creates movie requests on that user's behalf, letting Seerr apply its own
/// per-user approval and quota rules.
/// </summary>
public class SeerrClient
{
    private static readonly HttpClient Client = CreateClient();

    private readonly string _baseUrl;
    private readonly string _apiKey;
    private readonly ILogger _logger;

    public SeerrClient(string baseUrl, string apiKey, ILogger logger)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _apiKey = apiKey;
        _logger = logger;
    }

    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler { CheckCertificateRevocationList = true };
        var client = new HttpClient(handler);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Letterboxd-JellyfinSync/2.0");
        return client;
    }

    /// <summary>
    /// Resolves the Seerr user id whose <c>jellyfinUserId</c> matches <paramref name="jellyfinUserId"/>
    /// (falling back to a username match). Returns null when no Seerr account maps to the user.
    /// </summary>
    public async Task<int?> ResolveUserIdByJellyfin(Guid jellyfinUserId, string? jellyfinUsername)
    {
        var wantedId = Normalize(jellyfinUserId.ToString("N"));
        var wantedName = jellyfinUsername?.Trim();
        int? nameFallback = null;

        const int take = 100;
        var skip = 0;

        while (true)
        {
            var url = $"{_baseUrl}/api/v1/user?take={take}&skip={skip.ToString(CultureInfo.InvariantCulture)}";
            var (status, body) = await SendAsync(HttpMethod.Get, url, null).ConfigureAwait(false);
            if (status != HttpStatusCode.OK)
            {
                _logger.LogWarning("Seerr user lookup failed ({Status}).", (int)status);
                return nameFallback;
            }

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
            {
                return nameFallback;
            }

            var count = 0;
            foreach (var u in results.EnumerateArray())
            {
                count++;
                if (!u.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.Number)
                {
                    continue;
                }

                var id = idEl.GetInt32();

                if (u.TryGetProperty("jellyfinUserId", out var jf) && jf.ValueKind == JsonValueKind.String
                    && Normalize(jf.GetString()) == wantedId && wantedId.Length > 0)
                {
                    return id;
                }

                if (nameFallback == null && !string.IsNullOrEmpty(wantedName)
                    && (NameMatches(u, "jellyfinUsername", wantedName) || NameMatches(u, "username", wantedName) || NameMatches(u, "displayName", wantedName)))
                {
                    nameFallback = id;
                }
            }

            if (count < take)
            {
                return nameFallback;
            }

            skip += take;
        }
    }

    /// <summary>Requests a movie by TMDB id, on behalf of <paramref name="userId"/> when supplied.</summary>
    public async Task<SeerrRequestOutcome> RequestMovieAsync(int tmdbId, int? userId)
    {
        var payload = new StringBuilder();
        payload.Append("{\"mediaType\":\"movie\",\"mediaId\":").Append(tmdbId.ToString(CultureInfo.InvariantCulture));
        if (userId is { } uid)
        {
            payload.Append(",\"userId\":").Append(uid.ToString(CultureInfo.InvariantCulture));
        }

        payload.Append('}');

        using var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
        var (status, body) = await SendAsync(HttpMethod.Post, $"{_baseUrl}/api/v1/request", content).ConfigureAwait(false);

        // 201 Created (200 on some builds) = requested; 409 = already requested/available.
        if (status is HttpStatusCode.Created or HttpStatusCode.OK)
        {
            return SeerrRequestOutcome.Created;
        }

        if (status == HttpStatusCode.Conflict)
        {
            return SeerrRequestOutcome.AlreadyExists;
        }

        _logger.LogWarning("Seerr request for tmdb:{TmdbId} failed ({Status}). {Body}", tmdbId, (int)status, Preview(body));
        return SeerrRequestOutcome.Failed;
    }

    private async Task<(HttpStatusCode Status, string Body)> SendAsync(HttpMethod method, string url, HttpContent? content)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.TryAddWithoutValidation("X-Api-Key", _apiKey);
        if (content != null)
        {
            request.Content = content;
        }

        using var response = await Client.SendAsync(request).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return (response.StatusCode, body);
    }

    private static bool NameMatches(JsonElement user, string property, string wanted)
        => user.TryGetProperty(property, out var el) && el.ValueKind == JsonValueKind.String
           && string.Equals(el.GetString(), wanted, StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string? value)
        => value == null ? string.Empty : value.Replace("-", string.Empty, StringComparison.Ordinal).ToLowerInvariant();

    private static string Preview(string body)
        => string.IsNullOrWhiteSpace(body) ? string.Empty : body.Length > 200 ? body.Substring(0, 200) : body;
}
