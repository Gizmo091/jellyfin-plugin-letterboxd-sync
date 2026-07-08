using LetterboxdSync;
using Xunit;

namespace LetterboxdSync.Tests
{
    /// <summary>
    /// Unit tests for the Jellyfin (0-10) → Letterboxd (0.5-5.0) rating conversion. Pure logic, no network.
    /// </summary>
    public class LetterboxdRatingTests
    {
        [Theory]
        [InlineData(null, 0.0)]   // no rating → nothing sent
        [InlineData(0.0, 0.0)]    // explicit zero → nothing sent
        [InlineData(10.0, 5.0)]   // max
        [InlineData(9.0, 4.5)]
        [InlineData(8.0, 4.0)]
        [InlineData(7.0, 3.5)]
        [InlineData(5.0, 2.5)]
        [InlineData(2.0, 1.0)]
        [InlineData(1.0, 0.5)]    // min half-star
        [InlineData(7.5, 4.0)]    // rounds half away from zero (7.5 → 8 half-stars → 4.0)
        [InlineData(6.4, 3.0)]    // 6.4 → 6 half-stars → 3.0
        [InlineData(0.4, 0.5)]    // tiny positive rating is floored to the Letterboxd minimum
        [InlineData(11.0, 5.0)]   // out-of-range clamps to 5.0
        public void ToLetterboxdRating_MapsJellyfinScale(double? jellyfin, double expected)
        {
            Assert.Equal(expected, LetterboxdMovieSync.ToLetterboxdRating(jellyfin));
        }
    }
}
