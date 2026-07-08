using System;
using System.Threading.Tasks;
using LetterboxdSync;
using Xunit;

namespace LetterboxdSync.Tests
{
    /// <summary>
    /// Integration tests for the authenticated (write) path. They are opt-in and only run when a
    /// throwaway Letterboxd test account is provided via LETTERBOXD_TEST_USERNAME /
    /// LETTERBOXD_TEST_PASSWORD *and* LETTERBOXD_RUN_WRITE_TESTS=true. This keeps them off ordinary
    /// pushes (they run only on the manual CI trigger and during a release), so the test account is
    /// not hit on every commit. Any diary entry created is deleted again so the account stays clean.
    /// </summary>
    [Trait("Category", "Integration")]
    public class LetterboxdApiWriteTests
    {
        private static string? User => Environment.GetEnvironmentVariable("LETTERBOXD_TEST_USERNAME");

        private static string? Password => Environment.GetEnvironmentVariable("LETTERBOXD_TEST_PASSWORD");

        private static bool Enabled =>
            !string.IsNullOrWhiteSpace(User) &&
            !string.IsNullOrWhiteSpace(Password) &&
            string.Equals(Environment.GetEnvironmentVariable("LETTERBOXD_RUN_WRITE_TESTS"), "true", StringComparison.OrdinalIgnoreCase);

        private const string SkipReason = "Opt-in: set LETTERBOXD_TEST_USERNAME, LETTERBOXD_TEST_PASSWORD and LETTERBOXD_RUN_WRITE_TESTS=true to run the authenticated write tests.";

        [SkippableFact]
        public async Task PasswordGrant_ThenRefreshGrant_ShouldYieldWorkingTokens()
        {
            Skip.IfNot(Enabled, SkipReason);

            var api = new LetterboxdApi();
            await api.AuthenticateWithPassword(User!, Password!);

            Assert.True(api.IsAuthenticated);
            Assert.False(string.IsNullOrWhiteSpace(api.RefreshToken));

            // The stored refresh token must authenticate a brand-new client without the password.
            var refreshed = new LetterboxdApi();
            await refreshed.AuthenticateWithRefreshToken(api.RefreshToken!);
            Assert.True(refreshed.IsAuthenticated);

            var film = await refreshed.SearchFilmByTmdbId(550);
            Assert.NotNull(film);
        }

        [SkippableFact]
        public async Task MarkAsWatched_ThenDelete_ShouldRoundTrip()
        {
            Skip.IfNot(Enabled, SkipReason);

            var api = new LetterboxdApi();
            await api.AuthenticateWithPassword(User!, Password!);

            var film = await api.SearchFilmByTmdbId(550); // Fight Club
            Assert.NotNull(film);
            Assert.NotEmpty(film!.FilmId);

            // Create a diary entry (or 204 if it already exists), then delete it to keep the account tidy.
            var logEntryId = await api.MarkAsWatched(film.FilmId, DateTime.UtcNow.Date, tags: null, liked: false);

            if (!string.IsNullOrEmpty(logEntryId))
            {
                await api.DeleteLogEntry(logEntryId!);
            }
        }

        [SkippableFact]
        public async Task Diary_ResolveMemberAndReadEntries_ShouldParse()
        {
            Skip.IfNot(Enabled, SkipReason);

            var api = new LetterboxdApi();
            await api.AuthenticateWithPassword(User!, Password!);

            // Validates the live /me and /log-entries?where=HasDiaryDate endpoints used by the diary
            // import: the member resolves, the diary read returns 200, and every entry parses with a
            // TMDB id and a diary date. (Read-only: a just-created entry is not immediately visible on
            // the list endpoint, so we don't assert write-then-read visibility here.)
            var memberId = await api.GetAuthenticatedMemberId();
            Assert.False(string.IsNullOrEmpty(memberId));

            var entries = await api.GetDiaryEntries(memberId!);
            Assert.NotNull(entries);
            Assert.All(entries, e =>
            {
                Assert.False(string.IsNullOrEmpty(e.TmdbId));
                Assert.NotNull(e.DiaryDate);
            });
        }
    }
}
