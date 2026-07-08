using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace LetterboxdSync.Configuration;

public class Account
{
    public string? UserJellyfin { get; set; }

    public string? UserLetterboxd { get; set; }

    /// <summary>
    /// Transient Letterboxd password. Only used to obtain a <see cref="RefreshToken"/> and is
    /// cleared before the account is persisted — it is never stored on disk.
    /// </summary>
    public string? PasswordLetterboxd { get; set; }

    /// <summary>
    /// Long-lived OAuth2 refresh token obtained from Letterboxd. This is what the plugin stores and
    /// uses to authenticate; it only expires if Letterboxd explicitly revokes it.
    /// </summary>
    public string? RefreshToken { get; set; }

    public bool Enable { get; set; }

    public bool SendFavorite { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Jellyfin personal rating (0-10) is sent to
    /// Letterboxd as a star rating (0.5-5.0). Defaults to true. Ratings are only applied when a film
    /// is first logged (Letterboxd's log-entry endpoint is idempotent), so this never overwrites an
    /// existing Letterboxd rating.
    /// </summary>
    public bool SendRating { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether a film is logged to Letterboxd immediately when the
    /// user finishes watching it, instead of waiting for the daily task. Defaults to true.
    /// </summary>
    public bool EnableRealtimeSync { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the member's Letterboxd diary is imported back into
    /// Jellyfin (matching films are marked watched with their diary date). Defaults to false because
    /// it modifies Jellyfin's own watched state.
    /// </summary>
    public bool ImportDiary { get; set; }

    public bool EnableDateFilter { get; set; }

    public int DateFilterDays { get; set; } = 7;

    // Populate is required so System.Text.Json fills this read-only collection when Jellyfin
    // deserializes the updated plugin configuration (otherwise saved watchlists are dropped).
    // Legacy: watchlists used to be plain strings. Kept for backward-compatible migration into
    // Watchlists (see GetEffectiveWatchlists); the UI now writes Watchlists and leaves this empty.
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Collection<string> WatchlistUsernames { get; } = new();

    /// <summary>Gets the watchlists to mirror, each carrying its own auto-request flag.</summary>
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Collection<WatchlistEntry> Watchlists { get; } = new();

    /// <summary>
    /// Returns the effective watchlists, migrating any legacy <see cref="WatchlistUsernames"/> entries
    /// (with auto-request off) when <see cref="Watchlists"/> has not been populated yet.
    /// </summary>
    public IReadOnlyList<WatchlistEntry> GetEffectiveWatchlists()
    {
        if (Watchlists.Count > 0)
        {
            return Watchlists.Where(w => !string.IsNullOrWhiteSpace(w.Input)).ToList();
        }

        return WatchlistUsernames
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => new WatchlistEntry { Input = u, AutoRequest = false })
            .ToList();
    }
}
