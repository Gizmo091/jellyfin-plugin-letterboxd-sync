using System.Threading.Tasks;
using LetterboxdSync;
using Xunit;

namespace LetterboxdSync.Tests
{
    public class LetterboxdApiTests
    {
        private readonly LetterboxdApi _api;

        public LetterboxdApiTests()
        {
            _api = new LetterboxdApi();
        }

        [Fact]
        public async Task SearchFilmByTmdbId_WithValidTmdbId_ShouldReturnFilmResult()
        {
            // Fight Club — public lookup, only needs the signed API key (no member login).
            var result = await _api.SearchFilmByTmdbId(550);

            Assert.NotNull(result);
            Assert.NotEmpty(result!.FilmId); // Letterboxd LID
            Assert.Equal("550", result.TmdbId);
            Assert.Contains("fight-club", result.FilmSlug);

            await Task.Delay(1000);
        }

        [Fact]
        public async Task SearchFilmByTmdbId_WithValidTmdbId_Incredibles2_ShouldReturnFilmResult()
        {
            var result = await _api.SearchFilmByTmdbId(260513); // Incredibles 2

            Assert.NotNull(result);
            Assert.NotEmpty(result!.FilmId);
            Assert.Equal("260513", result.TmdbId);
            Assert.Contains("incredibles-2", result.FilmSlug);

            await Task.Delay(1000);
        }

        [Fact]
        public async Task SearchFilmByTmdbId_WithUnknownTmdbId_ShouldReturnNull()
        {
            var result = await _api.SearchFilmByTmdbId(999999999);

            Assert.Null(result);

            await Task.Delay(1000);
        }

        [Fact]
        public async Task ResolveWatchlistInput_PlainUsername_ShouldTargetWatchlist()
        {
            var target = await LetterboxdApi.ResolveWatchlistInput("someuser");

            Assert.Equal("someuser", target.Username);
            Assert.Null(target.ListSlug);
        }

        [Fact]
        public async Task ResolveWatchlistInput_ListUrl_ShouldParseUsernameAndSlug()
        {
            var target = await LetterboxdApi.ResolveWatchlistInput("https://letterboxd.com/dave/list/official-top-250/");

            Assert.Equal("dave", target.Username);
            Assert.Equal("official-top-250", target.ListSlug);
        }
    }
}
