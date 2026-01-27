using System;
using System.Collections.Generic;
using System.Globalization;
using LetterboxdSync.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace LetterboxdSync;

/// <summary>
/// The main plugin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        // Disable TLS session resumption so each Letterboxd sync run gets a fresh TLS handshake.
        // Set as early as possible (plugin load) so it takes effect before any Letterboxd connection.
        AppContext.SetSwitch("System.Net.Security.DisableTlsResume", true);
    }

    /// <inheritdoc />
    public override string Name => "LetterboxdSync";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("b1fb3d98-3336-4b87-a5c9-8a948bd87233");

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "configLetterboxd",
                EmbeddedResourcePath = $"{GetType().Namespace}.Web.configLetterboxd.html"
            },
            new PluginPageInfo
            {
                Name = "configLetterboxdjs",
                EmbeddedResourcePath = $"{GetType().Namespace}.Web.configLetterboxd.js"
            }
        };
    }
}
