using System.Collections.ObjectModel;
using MediaBrowser.Model.Plugins;

namespace LetterboxdSync.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public Collection<Account> Accounts { get; } = new();
}
