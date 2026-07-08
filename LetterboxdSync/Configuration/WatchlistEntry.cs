namespace LetterboxdSync.Configuration;

/// <summary>
/// A single watchlist/list to mirror into a Jellyfin playlist, plus whether films missing from the
/// library should be auto-requested in Seerr.
/// </summary>
public class WatchlistEntry
{
    /// <summary>Gets or sets the watchlist input: a Letterboxd username, a profile/watchlist URL, a
    /// boxd.it short link, or a list URL.</summary>
    public string? Input { get; set; }

    /// <summary>Gets or sets a value indicating whether films on this watchlist that are absent from
    /// the Jellyfin library are automatically requested in Seerr (on behalf of the mapped user).</summary>
    public bool AutoRequest { get; set; }
}
