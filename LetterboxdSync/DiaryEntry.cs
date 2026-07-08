using System;

namespace LetterboxdSync;

/// <summary>
/// A single entry from a member's Letterboxd diary, reduced to what the plugin needs to mirror the
/// watch back into Jellyfin.
/// </summary>
public class DiaryEntry
{
    /// <summary>Gets or sets the film's TMDB id (used to match against the Jellyfin library).</summary>
    public string? TmdbId { get; set; }

    /// <summary>Gets or sets the film's URL slug (e.g. "the-godfather"). May be empty.</summary>
    public string FilmSlug { get; set; } = string.Empty;

    /// <summary>Gets or sets the date the film was watched, as recorded in the Letterboxd diary.</summary>
    public DateTime? DiaryDate { get; set; }

    /// <summary>Gets or sets the Letterboxd star rating (0.5-5.0), when the entry carries one.</summary>
    public double? Rating { get; set; }
}
