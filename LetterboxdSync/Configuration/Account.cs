using System.Collections.ObjectModel;

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

    public bool EnableDateFilter { get; set; }

    public int DateFilterDays { get; set; } = 7;

    public Collection<string> WatchlistUsernames { get; } = new();
}
