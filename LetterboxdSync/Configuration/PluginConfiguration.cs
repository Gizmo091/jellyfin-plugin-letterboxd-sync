using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using MediaBrowser.Model.Plugins;

namespace LetterboxdSync.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    // Populate is required so System.Text.Json fills this read-only collection when Jellyfin
    // deserializes the updated plugin configuration (otherwise all accounts are dropped on save).
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Collection<Account> Accounts { get; } = new();

    /// <summary>
    /// Gets or sets the base URL of a Seerr / Jellyseerr / Overseerr instance (e.g.
    /// <c>http://localhost:5055</c>). Required for watchlist auto-requesting; leave empty to disable.
    /// </summary>
    public string? SeerrUrl { get; set; }

    /// <summary>
    /// Gets or sets the Seerr API key (Settings → General → API Key). Used to request films on behalf
    /// of the mapped Jellyfin user; Seerr then applies that user's own approval/quota rules.
    /// </summary>
    public string? SeerrApiKey { get; set; }
}
