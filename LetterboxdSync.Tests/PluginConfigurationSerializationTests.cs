using System.Text.Json;
using LetterboxdSync.Configuration;
using Xunit;

namespace LetterboxdSync.Tests
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
    }
}
