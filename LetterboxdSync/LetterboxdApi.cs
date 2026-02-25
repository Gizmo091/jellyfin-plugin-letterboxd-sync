using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace LetterboxdSync;

public class LetterboxdApi
{
    private string csrf = string.Empty;
    private string username = string.Empty;

    public string Csrf => csrf;

    // Reused for the lifetime of this LetterboxdApi instance (one sync run)
    private readonly CookieContainer cookieContainer = new CookieContainer();
    private readonly HttpClientHandler handler;
    private readonly HttpClient client;

    private static readonly Uri BaseUri = new Uri("https://letterboxd.com/");

    private bool HasCookie(string name)
    {
        var cookies = cookieContainer.GetCookies(BaseUri);
        return !string.IsNullOrWhiteSpace(cookies[name]?.Value);
    }

    private bool HasAuthenticatedSession()
    {
        var cookies = cookieContainer.GetCookies(new Uri("https://letterboxd.com/"));
        return !string.IsNullOrWhiteSpace(cookies["letterboxd.user.CURRENT"]?.Value);
    }

    public void SetRawCookies(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return;

        var baseUri = new Uri("https://letterboxd.com/");
        // Split cookie header like: name=value; name2=value2; ...
        foreach (var part in raw.Split(';'))
        {
            var kv = part.Trim();
            if (string.IsNullOrEmpty(kv)) continue;
            var eq = kv.IndexOf('=');
            if (eq <= 0) continue;
            var name = kv.Substring(0, eq).Trim();
            var val = kv.Substring(eq + 1).Trim();
            try
            {
                // URL-decode value if needed
                val = WebUtility.UrlDecode(val);
                var cookie = new Cookie(name, val, "/", "letterboxd.com")
                {
                    HttpOnly = false,
                };
                cookieContainer.Add(baseUri, cookie);

                var dotCookie = new Cookie(name, val, "/", ".letterboxd.com")
                {
                    HttpOnly = false,
                };
                cookieContainer.Add(baseUri, dotCookie);

                if (string.Equals(name, "com.xk72.webparts.csrf", StringComparison.OrdinalIgnoreCase))
                {
                    this.csrf = val;
                }
            }
            catch
            {
                // ignore malformed cookie entries
            }
        }
    }

    private async Task RefreshCsrfFromFilmPageAsync(string filmSlug)
    {
        // Film page typically contains the CSRF token expected by actions related to that film.
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/film/{filmSlug}/");
        SetNavigationHeaders(request.Headers, "same-origin");
        using var response = await client.SendAsync(request).ConfigureAwait(false);
        var html = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        var fresh = ExtractHiddenInput(html, "__csrf");
        if (string.IsNullOrWhiteSpace(fresh))
            throw new Exception($"Could not extract __csrf from film page /film/{filmSlug}/");

        this.csrf = fresh!;
    }


    private string GetCsrfFromCookie()
    {
        var cookies = cookieContainer.GetCookies(new Uri("https://letterboxd.com/"));
        // This is the token Letterboxd expects in the "__csrf" form field
        return cookies["com.xk72.webparts.csrf"]?.Value ?? string.Empty;
    }

    private async Task RefreshCsrfCookieAsync()
    {
        // Touch a page to ensure the CSRF cookie exists / is fresh
        using (var request = new HttpRequestMessage(HttpMethod.Get, "/"))
        {
            SetNavigationHeaders(request.Headers);
            using var _ = await client.SendAsync(request).ConfigureAwait(false);
        }

        var token = GetCsrfFromCookie();
        if (string.IsNullOrWhiteSpace(token))
            throw new Exception("Could not read CSRF cookie 'com.xk72.webparts.csrf' after refreshing.");
        this.csrf = token;
    }

    public LetterboxdApi()
    {
        handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            AllowAutoRedirect = true
        };

        client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://letterboxd.com")
        };

        // Use a Firefox UA if you're copying cookies from Firefox.
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:134.0) Gecko/20100101 Firefox/134.0");

        // Keep headers minimal. Sending Chrome-only "sec-ch-ua" headers while claiming Firefox
        // can make bot detection more likely.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml", 0.9));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.8));
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br");
        client.DefaultRequestHeaders.Connection.Add("keep-alive");

        // Remove these (they were Chrome-specific):
        // client.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua", ...);
        // client.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua-mobile", ...);
        // client.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua-platform", ...);
        client.DefaultRequestHeaders.TryAddWithoutValidation("sec-fetch-dest", "document");
        client.DefaultRequestHeaders.TryAddWithoutValidation("sec-fetch-mode", "navigate");
        client.DefaultRequestHeaders.TryAddWithoutValidation("sec-fetch-site", "none");
        client.DefaultRequestHeaders.TryAddWithoutValidation("sec-fetch-user", "?1");
        client.DefaultRequestHeaders.TryAddWithoutValidation("upgrade-insecure-requests", "1");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Priority", "u=0, i");
    }

    private void SetNavigationHeaders(HttpRequestHeaders headers, string site = "none", string? referrer = null)
    {
        headers.Accept.Clear();
        headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
        headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml", 0.9));
        headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/avif"));
        headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/webp"));
        headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/apng"));
        headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.8));

        headers.TryAddWithoutValidation("sec-fetch-dest", "document");
        headers.TryAddWithoutValidation("sec-fetch-mode", "navigate");
        headers.TryAddWithoutValidation("sec-fetch-site", site);
        headers.TryAddWithoutValidation("sec-fetch-user", "?1");
        headers.TryAddWithoutValidation("upgrade-insecure-requests", "1");

        // Use the same Priority header consistently
        if (headers.Contains("Priority")) headers.Remove("Priority");
        headers.TryAddWithoutValidation("Priority", "u=0, i");

        if (referrer != null)
        {
            headers.Referrer = new Uri(referrer);
        }
    }

    public async Task Authenticate(string username, string password)
    {
        this.username = username;

        // If user injected real browser cookies, don't try the login POST (Cloudflare blocks it).
        if (HasAuthenticatedSession())
        {
            try
            {
                await RefreshCsrfCookieAsync().ConfigureAwait(false);
                return;
            }
            catch
            {
                // If refreshing CSRF fails (e.g. cookies expired), proceed to normal login.
            }
        }

        // 0) Initial delay to avoid "speeding"
        await Task.Delay(500 + Random.Shared.Next(1000)).ConfigureAwait(false);

        // 1) GET /sign-in/ to obtain cookies + __csrf
        using (var signInRequest = new HttpRequestMessage(HttpMethod.Get, "/sign-in/"))
        {
            SetNavigationHeaders(signInRequest.Headers);

            using (var signInResponse = await client.SendAsync(signInRequest).ConfigureAwait(false))
            {
                if (signInResponse.StatusCode == HttpStatusCode.Forbidden)
                {
                    // If user injected Cloudflare clearance cookie, warm up and retry once
                    if (cookieContainer.GetCookies(new Uri("https://letterboxd.com/")).Cast<Cookie>().Any(c => c.Name.Equals("cf_clearance", StringComparison.OrdinalIgnoreCase)))
                    {
                        await Task.Delay(1500).ConfigureAwait(false);
                        using var warmup = new HttpRequestMessage(HttpMethod.Get, "/");
                        SetNavigationHeaders(warmup.Headers);
                        using var _ = await client.SendAsync(warmup).ConfigureAwait(false);

                        using var retryReq = new HttpRequestMessage(HttpMethod.Get, "/sign-in/");
                        SetNavigationHeaders(retryReq.Headers);
                        using var retryRes = await client.SendAsync(retryReq).ConfigureAwait(false);
                        if (retryRes.StatusCode == HttpStatusCode.Forbidden)
                        {
                            var rbody = await retryRes.Content.ReadAsStringAsync().ConfigureAwait(false);
                            if (rbody.Length > 300) rbody = rbody.Substring(0, 300);
                            throw new Exception("Letterboxd returned 403 on /sign-in/ even after using provided Cloudflare cookies. Body: " + rbody);
                        }

                        var retryHtml = await retryRes.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var csrfFromRetry = ExtractHiddenInput(retryHtml, "__csrf");
                        if (string.IsNullOrWhiteSpace(csrfFromRetry))
                            throw new Exception("Could not find __csrf token on /sign-in/ after retry.");
                        this.csrf = csrfFromRetry;
                        goto AfterSignIn;
                    }

                    var body = await signInResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (body.Length > 300) body = body.Substring(0, 300);
                    throw new Exception(
                        "Letterboxd returned 403 on /sign-in/. This is likely Cloudflare protection. " +
                        "Body: " + body);
                }

                if (signInResponse.StatusCode != HttpStatusCode.OK)
                {
                    var body = await signInResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (body.Length > 300) body = body.Substring(0, 300);
                    throw new Exception($"Letterboxd returned {(int)signInResponse.StatusCode} on /sign-in/. Body: " + body);
                }

                var signInHtml = await signInResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                var csrfFromSignIn = ExtractHiddenInput(signInHtml, "__csrf");
                if (string.IsNullOrWhiteSpace(csrfFromSignIn))
                {
                    throw new Exception("Could not find __csrf token on /sign-in/ (login flow likely changed).");
                }

                this.csrf = csrfFromSignIn;
            }
        }
        AfterSignIn:;

        // 2) POST /user/login.do with credentials + __csrf
        await Task.Delay(3000 + Random.Shared.Next(4000)).ConfigureAwait(false); // Mimic human typing/thinking time
        using (var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/user/login.do"))
        {
            loginRequest.Headers.Accept.Clear();
            loginRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            loginRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/javascript"));
            loginRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.01));
            loginRequest.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");

            loginRequest.Headers.Referrer = new Uri("https://letterboxd.com/sign-in/");
            loginRequest.Headers.TryAddWithoutValidation("Origin", "https://letterboxd.com");
            loginRequest.Headers.TryAddWithoutValidation("sec-fetch-dest", "empty");
            loginRequest.Headers.TryAddWithoutValidation("sec-fetch-mode", "cors");
            loginRequest.Headers.TryAddWithoutValidation("sec-fetch-site", "same-origin");
            loginRequest.Headers.TryAddWithoutValidation("Priority", "u=1, i");
            loginRequest.Headers.Remove("sec-fetch-user");
            loginRequest.Headers.Remove("upgrade-insecure-requests");

            loginRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "username", username },
                { "password", password },
                { "__csrf", this.csrf },
                { "remember", "true" },
                { "authenticationCode", "" }
            });

            // Ensure all previously set cookies are sent.
            // CookieContainer handles this automatically as long as it's the same client/handler.
            
            using (var loginResponse = await client.SendAsync(loginRequest).ConfigureAwait(false))
            {
                if (loginResponse.StatusCode == HttpStatusCode.Forbidden)
                {
                    var body = await loginResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (body.Length > 300) body = body.Substring(0, 300);
                    throw new Exception(
                        "Letterboxd returned 403 during login. This is likely reCAPTCHA/anti-bot enforcement. " +
                        "Body: " + body
                    );
                }

                if (!loginResponse.IsSuccessStatusCode)
                {
                    var body = await loginResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (body.Length > 300) body = body.Substring(0, 300);
                    throw new Exception($"Letterboxd returned {(int)loginResponse.StatusCode} during login. Body: " + body);
                }

                var loginBody = await loginResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                using (JsonDocument doc = JsonDocument.Parse(loginBody))
                {
                    var json = doc.RootElement;
                    if (json.TryGetProperty("result", out var resultEl) && resultEl.GetString() == "error")
                    {
                        var msg = "Login failed";
                        if (json.TryGetProperty("messages", out var msgsEl))
                        {
                            var sb = new StringBuilder();
                            foreach (var m in msgsEl.EnumerateArray())
                                sb.Append(m.GetString()).Append(' ');
                            msg = sb.ToString().Trim();
                        }
                        throw new Exception("Letterboxd login error: " + msg);
                    }
                }
            }
        }

        // 3) Refresh CSRF cookie after login
        await RefreshCsrfCookieAsync().ConfigureAwait(false);

        // 4) Refresh CSRF after login (best-effort)
        // Some sites rotate CSRF tokens; we try to extract it from the homepage.
        try
        {
            using var homeRequest = new HttpRequestMessage(HttpMethod.Get, "/");
            SetNavigationHeaders(homeRequest.Headers, "same-origin", "https://letterboxd.com/sign-in/");
            using var homeResponse = await client.SendAsync(homeRequest).ConfigureAwait(false);
            var homeHtml = await homeResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            var csrfAfter = ExtractHiddenInput(homeHtml, "__csrf");
            if (!string.IsNullOrWhiteSpace(csrfAfter))
                this.csrf = csrfAfter;
        }
        catch
        {
            // Keep the sign-in csrf if this fails.
        }
    }

    public async Task<FilmResult> SearchFilmByTmdbId(int tmdbid)
    {
        // Add a small initial delay to avoid bursting
        await Task.Delay(500 + Random.Shared.Next(500)).ConfigureAwait(false);

        // Reuse the authenticated client + cookies from Authenticate.
        var tmdbPath = $"/tmdb/{tmdbid}";

        using (var searchRequest = new HttpRequestMessage(HttpMethod.Get, tmdbPath))
        {
            SetNavigationHeaders(searchRequest.Headers, "same-origin");

            using (var res = await client.SendAsync(searchRequest).ConfigureAwait(false))
            {
                if (res.StatusCode == HttpStatusCode.Forbidden)
                {
                    var body = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (body.Length > 300) body = body.Substring(0, 300);
                    
                    // If we got a 403, it's likely Cloudflare blocking automated lookups.
                    // We'll throw but suggest raw cookies/delays in the log elsewhere.
                    throw new Exception($"TMDB lookup returned 403 (Forbidden) for https://letterboxd.com/tmdb/{tmdbid}. This usually means Cloudflare is blocking the request. Body: " + body);
                }

                if (!res.IsSuccessStatusCode)
                {
                    var body = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (body.Length > 300) body = body.Substring(0, 300);
                    throw new Exception($"TMDB lookup returned {(int)res.StatusCode} for https://letterboxd.com/tmdb/{tmdbid}. Body: " + body);
                }

                // IMPORTANT:
                // Letterboxd may not 302 redirect here anymore; the final RequestUri can remain /tmdb/<id>.
                // So we parse the returned HTML to find a film link / canonical URL.
                var html = await res.Content.ReadAsStringAsync().ConfigureAwait(false);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                // Best case: canonical link points at the film page.
                string filmUrl = htmlDoc.DocumentNode
                    .SelectSingleNode("//link[@rel='canonical']")
                    ?.GetAttributeValue("href", string.Empty) ?? string.Empty;

                // Fallback: any anchor to /film/<slug>/
                if (string.IsNullOrWhiteSpace(filmUrl))
                {
                    var a = htmlDoc.DocumentNode.SelectSingleNode("//a[starts-with(@href, '/film/')]");
                    var href = a?.GetAttributeValue("href", string.Empty) ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(href))
                        filmUrl = href.StartsWith("/") ? "https://letterboxd.com" + href : href;
                }

                if (string.IsNullOrWhiteSpace(filmUrl))
                {
                    // Helpful debug: show what URL we actually fetched/ended at
                    var finalUri = res?.RequestMessage?.RequestUri?.ToString() ?? string.Empty;
                    throw new Exception($"The search returned no results (Could not resolve film URL from TMDB page). FinalUrl='{finalUri}'");
                }

                // Extract slug from film URL
                var filmUri = new Uri(filmUrl, UriKind.Absolute);
                var segments = filmUri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length < 2 || !segments[0].Equals("film", StringComparison.OrdinalIgnoreCase))
                    throw new Exception($"TMDB page resolved to non-film URL: '{filmUrl}'");

                string filmSlug = segments[1];

                // Load film page and extract filmId
                using (var filmRequest = new HttpRequestMessage(HttpMethod.Get, $"/film/{filmSlug}/"))
                {
                    SetNavigationHeaders(filmRequest.Headers, "same-origin", $"https://letterboxd.com/tmdb/{tmdbid}");
                    using (var filmRes = await client.SendAsync(filmRequest).ConfigureAwait(false))
                    {
                        if (!filmRes.IsSuccessStatusCode)
                        {
                            var body = await filmRes.Content.ReadAsStringAsync().ConfigureAwait(false);
                            if (body.Length > 300) body = body.Substring(0, 300);
                            throw new Exception($"Film page lookup returned {(int)filmRes.StatusCode} for https://letterboxd.com/film/{filmSlug}/. Body: " + body);
                        }

                        var filmHtml = await filmRes.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var filmDoc = new HtmlDocument();
                        filmDoc.LoadHtml(filmHtml);

                        HtmlNode? elForId =
                            filmDoc.DocumentNode.SelectSingleNode($"//div[@data-film-slug='{filmSlug}']") ??
                            filmDoc.DocumentNode.SelectSingleNode($"//div[@data-item-link='/film/{filmSlug}/']") ??
                            filmDoc.DocumentNode.SelectSingleNode("//div[@data-film-id]");

                        if (elForId == null)
                            throw new Exception("The search returned no results (No html element found to get letterboxd filmId)");

                        string filmId = elForId.GetAttributeValue("data-film-id", string.Empty);
                        if (string.IsNullOrEmpty(filmId))
                            throw new Exception("The search returned no results (data-film-id attribute is empty)");

                        return new FilmResult(filmSlug, filmId);
                    }
                }
            }
        }
    }


    public async Task MarkAsWatched(string filmSlug, string filmId, DateTime? date, string[] tags, bool liked = false)
    {
        string url = "/s/save-diary-entry";
        DateTime viewingDate = date == null ? DateTime.Now : (DateTime)date;

        for (int attempt = 0; attempt < 3; attempt++)
        {
            // IMPORTANT: Refresh CSRF from the film page (scoped token).
            await RefreshCsrfCookieAsync().ConfigureAwait(false);

            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                // Make the request look like the browser flow for this action
                request.Headers.Referrer = new Uri($"https://letterboxd.com/film/{filmSlug}/");
                request.Headers.TryAddWithoutValidation("Origin", "https://letterboxd.com");
                request.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
                request.Headers.TryAddWithoutValidation("sec-fetch-dest", "empty");
                request.Headers.TryAddWithoutValidation("sec-fetch-mode", "cors");
                request.Headers.TryAddWithoutValidation("sec-fetch-site", "same-origin");
                request.Headers.Remove("sec-fetch-user");
                request.Headers.Remove("upgrade-insecure-requests");
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/javascript"));
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.01));

                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "__csrf", this.csrf },
                    { "json", "true" },
                    { "viewingId", string.Empty },
                    { "filmId", filmId },
                    { "specifiedDate", date == null ? "false" : "true" },
                    { "viewingDateStr", viewingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) },
                    { "review", string.Empty },
                    { "tags", date != null && tags.Length > 0 ? $"[{string.Join(",", tags)}]" : string.Empty },
                    { "rating", "0" },
                    { "liked", liked.ToString().ToLowerInvariant() } // some endpoints are picky about casing
                });

                using (var response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var bodyPreview = body.Length > 300 ? body.Substring(0, 300) : body;
                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        throw new Exception(
                            "Letterboxd returned 403 during diary submission. This is likely reCAPTCHA/anti-bot enforcement. " +
                            "Body: " + bodyPreview
                        );
                    }
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception($"Letterboxd returned {(int)response.StatusCode}. Body: " + bodyPreview);
                    }

                    using (JsonDocument doc = JsonDocument.Parse(body))
                    {
                        var json = doc.RootElement;

                        // If response includes rotated CSRF, keep it
                        if (json.TryGetProperty("csrf", out var csrfEl) && csrfEl.ValueKind == JsonValueKind.String)
                        {
                            var newCsrf = csrfEl.GetString();
                            if (!string.IsNullOrWhiteSpace(newCsrf))
                                this.csrf = newCsrf!;
                        }

                        if (SuccessOperation(json, out string message))
                            return;

                        // Retry on transient errors (expired CSRF, rate-limiting)
                        if (attempt < 2 &&
                            (message.Contains("expired", StringComparison.OrdinalIgnoreCase) ||
                             message.Contains("try again", StringComparison.OrdinalIgnoreCase)))
                        {
                            await Task.Delay((attempt + 1) * 5000 + Random.Shared.Next(3000)).ConfigureAwait(false);
                            continue;
                        }

                        throw new Exception(message);
                    }
                }
            }
        }

        throw new Exception("Failed to submit diary entry after retries.");
    }




    public async Task<DateTime?> GetDateLastLog(string filmSlug)
    {
        // Uses same authenticated cookie container via client.
        string url = $"/{this.username}/film/{filmSlug}/diary/";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        SetNavigationHeaders(request.Headers, "same-origin");
        using var response = await client.SendAsync(request).ConfigureAwait(false);
        var responseHtml = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(responseHtml);

        var monthElements = htmlDoc.DocumentNode.SelectNodes("//a[contains(@class, 'month')]");
        var dayElements = htmlDoc.DocumentNode.SelectNodes("//a[contains(@class, 'date') or contains(@class, 'daydate')]");
        var yearElements = htmlDoc.DocumentNode.SelectNodes("//a[contains(@class, 'year')]");

        var lstDates = new List<DateTime>();

        if (monthElements != null && dayElements != null && yearElements != null)
        {
            var minCount = Math.Min(Math.Min(monthElements.Count, dayElements.Count), yearElements.Count);

            for (int i = 0; i < minCount; i++)
            {
                var month = monthElements[i].InnerText?.Trim();
                var day = dayElements[i].InnerText?.Trim();
                var year = yearElements[i].InnerText?.Trim();

                if (!string.IsNullOrEmpty(month) && !string.IsNullOrEmpty(day) && !string.IsNullOrEmpty(year))
                {
                    var dateString = $"{day} {month} {year}";
                    if (DateTime.TryParse(dateString, out DateTime parsedDate))
                        lstDates.Add(parsedDate);
                }
            }
        }

        return lstDates.Count > 0 ? lstDates.Max() : null;
    }

    private bool SuccessOperation(JsonElement json, out string message)
    {
        message = string.Empty;

        if (json.TryGetProperty("messages", out JsonElement messagesElement))
        {
            StringBuilder errorMessages = new StringBuilder();
            foreach (var i in messagesElement.EnumerateArray())
                errorMessages.Append(i.GetString());
            message = errorMessages.ToString();
        }

        if (json.TryGetProperty("result", out JsonElement statusElement))
        {
            switch (statusElement.ValueKind)
            {
                case JsonValueKind.String:
                    return statusElement.GetString() == "error" ? false : true;
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Resolved target for a watchlist/list input.
    /// </summary>
    public record WatchlistTarget(string PlaylistName, string BasePath);

    /// <summary>
    /// Resolves a watchlist input (short URL, full URL, or plain username) to a scraping target.
    /// Supports: "username", "https://boxd.it/QKjHO", "https://letterboxd.com/user/watchlist/",
    /// "https://letterboxd.com/user/list/list-slug/", "letterboxd.com/user".
    /// </summary>
    public static async Task<WatchlistTarget> ResolveWatchlistInput(string input)
    {
        input = input.Trim();

        // Plain username — no dots or slashes
        if (!input.Contains('/') && !input.Contains('.'))
        {
            return new WatchlistTarget($"{input}'s Watchlist", $"/{input}/watchlist");
        }

        // Normalize missing scheme
        if (!input.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            input = "https://" + input;
        }

        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            return new WatchlistTarget($"{input}'s Watchlist", $"/{input}/watchlist");
        }

        // Short URL (boxd.it) — read Location header without following redirect (avoids Cloudflare)
        if (uri.Host.Equals("boxd.it", StringComparison.OrdinalIgnoreCase))
        {
            using var redirectHandler = new HttpClientHandler { AllowAutoRedirect = false };
            using var httpClient = new HttpClient(redirectHandler);
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:134.0) Gecko/20100101 Firefox/134.0");
            using var response = await httpClient.GetAsync(uri).ConfigureAwait(false);
            var location = response.Headers.Location;
            if (location != null)
            {
                uri = location.IsAbsoluteUri ? location : new Uri(uri, location);
            }
        }

        // Extract path info from letterboxd.com URL
        if (uri.Host.Contains("letterboxd.com", StringComparison.OrdinalIgnoreCase))
        {
            var segments = uri.AbsolutePath.Trim('/').Split('/');

            // List URL: /username/list/list-slug/
            if (segments.Length >= 3 && segments[1] == "list")
            {
                var username = segments[0];
                var listSlug = segments[2];
                return new WatchlistTarget(
                    $"{username} - {listSlug}",
                    $"/{username}/list/{listSlug}");
            }

            // Watchlist or profile URL: /username/ or /username/watchlist/
            if (segments.Length > 0 && !string.IsNullOrEmpty(segments[0]))
            {
                var username = segments[0];
                return new WatchlistTarget($"{username}'s Watchlist", $"/{username}/watchlist");
            }
        }

        return new WatchlistTarget($"{input}'s Watchlist", $"/{input}/watchlist");
    }

    public async Task<List<FilmResult>> GetFilmsFromList(string basePath, int pageNum)
    {
        var films = new List<FilmResult>();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{basePath}/page/{pageNum}/");
        SetNavigationHeaders(request.Headers);

        using var response = await client.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        var posters = htmlDoc.DocumentNode.SelectNodes("//div[@data-component-class='LazyPoster']");

        if (posters == null)
        {
            return films;
        }

        foreach (var poster in posters)
        {
            var filmSlug = poster.GetAttributeValue("data-item-slug", string.Empty);
            if (string.IsNullOrEmpty(filmSlug))
            {
                continue;
            }

            var film = await GetFilmTmdbIdFromSlug(filmSlug).ConfigureAwait(false);
            if (film != null)
            {
                films.Add(film);
            }

            await Task.Delay(2000 + Random.Shared.Next(1000)).ConfigureAwait(false);
        }

        bool isNextPage = htmlDoc.DocumentNode.SelectNodes($"//li[a/text() = '{pageNum + 1}']") is not null;

        if (isNextPage)
        {
            var nextPageFilms = await GetFilmsFromList(basePath, pageNum + 1).ConfigureAwait(false);
            films.AddRange(nextPageFilms);
        }

        return films;
    }

    public async Task<FilmResult?> GetFilmTmdbIdFromSlug(string filmSlug)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/film/{filmSlug}/");
        SetNavigationHeaders(request.Headers);

        using var response = await client.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        var body = htmlDoc.DocumentNode.SelectSingleNode("//body");
        if (body == null)
        {
            return null;
        }

        string filmId = body.GetAttributeValue("data-tmdb-id", string.Empty);
        if (string.IsNullOrEmpty(filmId))
        {
            return null;
        }

        return new FilmResult(filmSlug, filmId);
    }

    private static string? ExtractHiddenInput(string html, string name)
    {
        // Matches: <input type="hidden" name="__csrf" value="...">
        var pattern = $@"<input[^>]*\bname\s*=\s*[""']{Regex.Escape(name)}[""'][^>]*\bvalue\s*=\s*[""']([^""']*)[""'][^>]*>";
        var m = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return m.Success ? WebUtility.HtmlDecode(m.Groups[1].Value) : null;
    }
}

public class FilmResult
{
    public string filmSlug = string.Empty;
    public string filmId = string.Empty;

    public FilmResult(string filmSlug, string filmId)
    {
        this.filmSlug = filmSlug;
        this.filmId = filmId;
    }
}
