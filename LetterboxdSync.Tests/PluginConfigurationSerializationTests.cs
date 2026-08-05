using System.Text.Json;
using Jellyfin.Plugin.LetterboxdSync.Configuration;
using Xunit;

namespace Jellyfin.Plugin.LetterboxdSync.Tests
{
    /// <summary>
    /// Guards the plugin configuration against System.Text.Json deserialization dropping the
    /// read-only collections — which is exactly what Jellyfin does when saving the config from the
    /// dashboard. Without the [JsonObjectCreationHandling(Populate)] attributes, accounts and their
    /// watchlists are silently lost on save.
    /// </summary>
    public class PluginConfigurationSerializationTests
    {
        [Fact]
        public void Config_RoundTripsThroughSystemTextJson()
        {
            var config = new PluginConfiguration();
            var account = new Account { UserLetterboxd = "bob", Enable = true };
            account.WatchlistUsernames.Add("alice");
            account.WatchlistUsernames.Add("carol");
            config.Accounts.Add(account);

            var json = JsonSerializer.Serialize(config);
            var restored = JsonSerializer.Deserialize<PluginConfiguration>(json)!;

            Assert.Single(restored.Accounts);
            Assert.Equal("bob", restored.Accounts[0].UserLetterboxd);
            Assert.Equal(new[] { "alice", "carol" }, restored.Accounts[0].WatchlistUsernames);
        }

        /// <summary>
        /// Jellyfin caches XML serializers in a single dictionary shared by every loaded plugin,
        /// keyed by <c>type.FullName</c> (Emby.Server.Implementations.Serialization.MyXmlSerializer).
        /// A plugin whose configuration type has the same FullName as another plugin's gets handed
        /// the wrong serializer and its config endpoint answers 500 — that is issue #7, where
        /// Jellyscribe also shipped LetterboxdSync.Configuration.PluginConfiguration. Keep this type
        /// under a namespace no other plugin would plausibly use.
        /// </summary>
        [Fact]
        public void ConfigTypeName_IsNamespacedToThisPlugin()
        {
            Assert.Equal(
                "Jellyfin.Plugin.LetterboxdSync.Configuration.PluginConfiguration",
                typeof(PluginConfiguration).FullName);
        }
    }
}
