using System;

namespace LetterboxdSync;

/// <summary>
/// Callback invoked by the File Transformation plugin to inject the Letterboxd client script into
/// jellyfin-web's index.html. The plugin passes a payload with the current file contents and expects
/// the (possibly) modified contents back as the return value.
/// </summary>
public static class IndexHtmlTransformer
{
    private const string ScriptTag = "<script plugin=\"LetterboxdSync\" src=\"/Jellyfin.Plugin.LetterboxdSync/ClientScript\" defer></script>";

    /// <summary>
    /// Transformation callback. Receives the current index.html contents and returns them with the
    /// plugin script tag injected before &lt;/body&gt; (idempotently).
    /// </summary>
    public static string TransformIndexHtml(FileTransformationPayload payload)
    {
        var contents = payload?.Contents ?? string.Empty;

        if (string.IsNullOrEmpty(contents) || contents.Contains("plugin=\"LetterboxdSync\"", StringComparison.Ordinal))
        {
            return contents;
        }

        return contents.Replace("</body>", $"{ScriptTag}\n</body>", StringComparison.Ordinal);
    }
}
