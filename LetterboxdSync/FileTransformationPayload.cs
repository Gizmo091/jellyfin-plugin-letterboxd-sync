namespace LetterboxdSync;

/// <summary>
/// Shape of the payload the File Transformation plugin deserializes into the transformation callback.
/// Only the file contents are needed. Property matching is case-insensitive (the plugin sends
/// <c>contents</c>).
/// </summary>
public sealed class FileTransformationPayload
{
    /// <summary>Gets or sets the current contents of the file being transformed.</summary>
    public string Contents { get; set; } = string.Empty;
}
