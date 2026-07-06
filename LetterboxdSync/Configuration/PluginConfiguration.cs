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
}
