using System;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using LetterboxdSync.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace LetterboxdSync;

/// <summary>
/// Shared logic for logging a single played Jellyfin movie to Letterboxd. Used by both the daily
/// scheduled task and the real-time (playback-finished) sync so they behave identically.
/// </summary>
internal static class LetterboxdMovieSync
{
    /// <summary>
    /// Converts a Jellyfin personal rating (0-10 scale) to a Letterboxd star rating (0.5-5.0 in 0.5
    /// steps). Returns 0 when there is no usable rating (Letterboxd's minimum is half a star).
    /// </summary>
    public static double ToLetterboxdRating(double? jellyfinRating)
    {
        if (jellyfinRating is not { } r || r <= 0)
        {
            return 0;
        }

        // Jellyfin 0-10 maps onto Letterboxd half-stars (0-10), so one star = round(rating) / 2.
        var stars = Math.Round(r, MidpointRounding.AwayFromZero) / 2.0;
        return Math.Clamp(stars, 0.5, 5.0);
    }

    /// <summary>
    /// Logs <paramref name="movie"/> to Letterboxd for <paramref name="account"/> using the supplied
    /// (already authenticated) <paramref name="api"/>. Returns true when a log request was sent (the
    /// entry may already have existed server-side), false when the film was skipped (no TMDB id, or
    /// not found on Letterboxd).
    /// </summary>
    public static async Task<bool> LogMovieAsync(
        LetterboxdApi api,
        BaseItem movie,
        User user,
        UserItemData userData,
        Account account,
        ILogger logger)
    {
        var title = movie.OriginalTitle ?? movie.Name;

        if (!int.TryParse(movie.GetProviderId(MetadataProvider.Tmdb), out var tmdbId))
        {
            logger.LogWarning(
                "Film does not have TmdbId. User: {Username} ({UserId}) Movie: {Movie}",
                user.Username,
                user.Id.ToString("N"),
                title);
            return false;
        }

        var filmResult = await api.SearchFilmByTmdbId(tmdbId).ConfigureAwait(false);
        if (filmResult == null)
        {
            logger.LogWarning(
                "Film not found on Letterboxd. User: {Username} ({UserId}) Movie: {Movie} ({TmdbId})",
                user.Username,
                user.Id.ToString("N"),
                title,
                tmdbId);
            return false;
        }

        var favorite = account.SendFavorite && movie.IsFavoriteOrLiked(user, userData);
        var rating = account.SendRating ? ToLetterboxdRating(userData.Rating) : 0;
        var rewatch = userData.PlayCount > 1;

        // Letterboxd's diary needs a date; fall back to today if Jellyfin has none.
        var viewingDate = (userData.LastPlayedDate ?? DateTime.Now).Date;

        // MarkAsWatched is idempotent server-side (204 = already logged), so no pre-check is needed.
        await api.MarkAsWatched(filmResult.FilmId, viewingDate, tags: null, liked: favorite, rating: rating, rewatch: rewatch).ConfigureAwait(false);

        logger.LogInformation(
            "Film logged in Letterboxd. User: {Username} ({UserId}) Movie: {Movie} ({TmdbId}) Date: {ViewingDate} Rating: {Rating} Rewatch: {Rewatch}",
            user.Username,
            user.Id.ToString("N"),
            title,
            tmdbId,
            viewingDate,
            rating,
            rewatch);

        return true;
    }
}
