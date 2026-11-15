using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.LetterboxdSync;
using Xunit;

namespace Jellyfin.Plugin.LetterboxdSync.Tests;

public class LetterboxdDiaryDuplicateTests
{
    [Fact]
    public async Task HasDiaryEntry_FiltersByFilmAndDate()
    {
        var handler = new StubHandler(request => JsonResponse(request.RequestUri!.AbsolutePath switch
        {
            "/api/v0/auth/token" => "{\"access_token\":\"access\",\"refresh_token\":\"refresh\"}",
            "/api/v0/log-entries" => "{\"items\":[{\"film\":{\"id\":\"film-lid\"},\"diaryDetails\":{\"diaryDate\":\"2026-11-14\"}}]}",
            _ => throw new InvalidOperationException($"Unexpected request: {request.RequestUri}")
        }));
        var api = new LetterboxdApi(null, handler);
        await api.AuthenticateWithRefreshToken("refresh");

        var found = await api.HasDiaryEntry("member-lid", "film-lid", new DateTime(2026, 11, 14, 23, 59, 0));

        Assert.True(found);
        var diaryRequest = Assert.Single(handler.Requests.Where(r => r.RequestUri!.AbsolutePath == "/api/v0/log-entries"));
        Assert.Contains("member=member-lid", diaryRequest.RequestUri!.Query);
        Assert.Contains("film=film-lid", diaryRequest.RequestUri.Query);
        Assert.Contains("where=HasDiaryDate", diaryRequest.RequestUri.Query);
    }

    [Fact]
    public async Task HasDiaryEntry_DoesNotTreatDifferentDateAsDuplicate()
    {
        var handler = new StubHandler(request => JsonResponse(request.RequestUri!.AbsolutePath switch
        {
            "/api/v0/auth/token" => "{\"access_token\":\"access\",\"refresh_token\":\"refresh\"}",
            "/api/v0/log-entries" => "{\"items\":[{\"film\":{\"id\":\"film-lid\"},\"diaryDetails\":{\"diaryDate\":\"2026-11-14\"}}]}",
            _ => throw new InvalidOperationException($"Unexpected request: {request.RequestUri}")
        }));
        var api = new LetterboxdApi(null, handler);
        await api.AuthenticateWithRefreshToken("refresh");

        var found = await api.HasDiaryEntry("member-lid", "film-lid", new DateTime(2026, 11, 15));

        Assert.False(found);
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        {
            _respond = respond;
        }

        public System.Collections.Generic.List<HttpRequestMessage> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(_respond(request));
        }
    }
}
