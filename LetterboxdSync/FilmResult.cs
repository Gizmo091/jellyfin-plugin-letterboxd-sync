namespace LetterboxdSync;

/// <summary>
/// Result of resolving a film on Letterboxd.
/// </summary>
public class FilmResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilmResult"/> class.
    /// </summary>
    /// <param name="filmSlug">The film's URL slug.</param>
    /// <param name="filmId">The Letterboxd film LID.</param>
    public FilmResult(string filmSlug, string filmId)
    {
        FilmSlug = filmSlug;
        FilmId = filmId;
    }

    /// <summary>Gets or sets the film's URL slug (e.g. "the-godfather"). May be empty.</summary>
    public string FilmSlug { get; set; } = string.Empty;

    /// <summary>Gets or sets the Letterboxd film LID (used to log the film).</summary>
    public string FilmId { get; set; } = string.Empty;

    /// <summary>Gets or sets the film's TMDB id, when known (used to match against the Jellyfin library).</summary>
    public string? TmdbId { get; set; }
}
