using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace LetterboxdSync;

public class LetterboxdApi
{
    private string cookie = string.Empty;
    private string csrf = string.Empty;

    private string username = string.Empty;

    public string Csrf => csrf;

    public async Task Authenticate(string username, string password)
    {
        string url = "https://letterboxd.com/user/login.do";

        var cookieContainer = new CookieContainer();
        this.username = username;

        using (var client = new HttpClient(new HttpClientHandler { CookieContainer = cookieContainer }))
        {
            var response = await client.PostAsync(url, new FormUrlEncodedContent(new Dictionary<string, string> { })).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception($"Letterbox return {(int)response.StatusCode}");

            this.cookie = CookieToString(cookieContainer.GetCookies(new Uri(url)));
            using (JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false)))
                this.csrf = GetElementFromJson(doc.RootElement, "csrf");
        }

        using (var client = new HttpClient(new HttpClientHandler { CookieContainer = cookieContainer }))
        {
            client.DefaultRequestHeaders.Add("DNT", "1");
            client.DefaultRequestHeaders.Add("Host", "letterboxd.com");
            client.DefaultRequestHeaders.Add("Origin", "https://letterboxd.com");
            client.DefaultRequestHeaders.Add("Priority", "u=0");
            client.DefaultRequestHeaders.Add("Referer", "https://letterboxd.com/");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
            client.DefaultRequestHeaders.Add("Sec-GPC", "1");
            client.DefaultRequestHeaders.Add("TE", "trailers");
            client.DefaultRequestHeaders.Add("Cookie", this.cookie);

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "username", username },
                { "password", password },
                { "__csrf", this.csrf },
                { "authenticationCode", " " }
            });

            var response = await client.PostAsync(url, content).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception($"Letterbox return {(int)response.StatusCode}");

            this.cookie = CookieToString(cookieContainer.GetCookies(new Uri(url)));

            using (JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false)))
            {
                var json = doc.RootElement;
                if (SucessOperation(json, out string message))
                    this.csrf = GetElementFromJson(doc.RootElement, "csrf");
                else
                    throw new Exception(message);
            }
        }
    }

    public async Task<FilmResult> SearchFilmByTmdbId(int tmdbid)
    {
        string tmdbUrl = $"https://letterboxd.com/tmdb/{tmdbid}";

        var handler = new HttpClientHandler()
        {
            AllowAutoRedirect = true
        };

        using (var client = new HttpClient(handler))
        {
            var res = await client.GetAsync(tmdbUrl).ConfigureAwait(false);

            string letterboxdUrl = res?.RequestMessage?.RequestUri?.ToString();
            var filmSlugRegex = Regex.Match(letterboxdUrl, @"https:\/\/letterboxd\.com\/film\/([^\/]+)\/");

            string filmSlug = filmSlugRegex.Groups[1].Value;
            if (string.IsNullOrEmpty(filmSlug))
                throw new Exception("The search returned no results (Final url does not match the regex)");

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(await res.Content.ReadAsStringAsync().ConfigureAwait(false));

            var span = htmlDoc.DocumentNode.SelectSingleNode("//div[@data-film-slug='" + filmSlug + "']");
            var div = htmlDoc.DocumentNode.SelectSingleNode("//div[@data-item-link='/film/"+filmSlug+"/']");
            HtmlNode? el_for_id = null;
            if (span != null) {
                el_for_id = span;
                }
            else if (div != null) {
                el_for_id = div;
            }

            if (el_for_id == null) {
                throw new Exception("The search returned no results (No html element found to get letterboxd filmId)");
                }

            string filmId = el_for_id.GetAttributeValue("data-film-id", string.Empty);
            if (string.IsNullOrEmpty(filmId))
                throw new Exception("The search returned no results (data-film-id attribute is empty)");

            return new FilmResult(filmSlug, filmId);
        }
    }


    public async Task MarkAsWatched(string filmId, DateTime? date, string[] tags, bool liked = false)
    {
        string url = $"https://letterboxd.com/s/save-diary-entry";
        DateTime viewingDate = date == null ? DateTime.Now : (DateTime) date;

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Cookie", this.cookie);

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
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
                { "liked", liked.ToString() }
            });

            var response = await client.PostAsync(url, content).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception($"Letterbox return {(int)response.StatusCode}");

            using (JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false)))
            {
                if (!SucessOperation(doc.RootElement, out string message))
                    throw new Exception(message);
            }
        }
    }

    public async Task<DateTime?> GetDateLastLog(string filmSlug)
    {
        string url = $"https://letterboxd.com/{this.username}/film/{filmSlug}/diary/";

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Cookie", this.cookie);
            var lstDates = new List<DateTime>();

            var response = await client.GetStringAsync(url).ConfigureAwait(false);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(response);

            var trReviews = htmlDoc.DocumentNode.SelectNodes("//tr[contains(@class, 'diary-entry-row')]");
            if (trReviews == null)
                return null;

            foreach (var trReview in trReviews)
            {
                var tdDayReview = trReview.SelectSingleNode("//td[contains(@class, 'td-day')]");
                if (tdDayReview == null)
                    break;

                var linkNode = tdDayReview.SelectSingleNode(".//a");
                if (linkNode == null)
                    break;

                string linkReview = $"https://letterboxd.com{linkNode.GetAttributeValue("href", "")}";

                response = await client.GetStringAsync(linkReview).ConfigureAwait(false);

                htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(response);

                var section = htmlDoc.DocumentNode.SelectSingleNode("//section[@class='film-viewing-info-wrapper']");
                if (section == null)
                    break;

                var meta = section.SelectSingleNode("meta");
                if (meta == null)
                    break;

                var date = meta.GetAttributeValue("content", string.Empty);
                if (date == null)
                    break;

                lstDates.Add(DateTime.Parse(date, CultureInfo.InvariantCulture));
            }

            return lstDates.Max();
        }
    }

    private string CookieToString(CookieCollection cookies)
    {
        StringBuilder cookieString = new StringBuilder();
        foreach (Cookie cookie in cookies)
        {
            cookieString.Append(new CultureInfo("en-US"), $"{cookie.Name}={cookie.Value}");
            cookieString.Append("; ");
        }

        return cookieString.ToString();
    }

    private string GetElementFromJson(JsonElement json, string property)
    {
        if (json.TryGetProperty(property, out JsonElement element))
            return element.GetString() ?? string.Empty;
        return string.Empty;
    }

    private bool SucessOperation(JsonElement json, out string message)
    {
        message = string.Empty;

        if (json.TryGetProperty("messages", out JsonElement messagesElement))
        {
            StringBuilder erroMessages = new StringBuilder();
            foreach (var i in messagesElement.EnumerateArray())
                erroMessages.Append(i.GetString());
            message = erroMessages.ToString();
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
}

public class FilmResult {
    public string filmSlug = string.Empty;
    public string filmId = string.Empty;

    public FilmResult(string filmSlug, string filmId){
        this.filmSlug = filmSlug;
        this.filmId = filmId;
    }
}
