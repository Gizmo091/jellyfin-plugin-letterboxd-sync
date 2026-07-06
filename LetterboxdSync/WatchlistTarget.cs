namespace LetterboxdSync;

/// <summary>
/// Resolved target for a watchlist/list input.
/// </summary>
/// <param name="DisplayName">The playlist name to use in Jellyfin.</param>
/// <param name="Username">The Letterboxd username, if resolved.</param>
/// <param name="ListSlug">The list slug when the input is a custom list, otherwise null.</param>
public record WatchlistTarget(string DisplayName, string? Username, string? ListSlug);
