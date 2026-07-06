using System;

namespace LetterboxdSync;

/// <summary>
/// Error raised for a failed Letterboxd API operation.
/// </summary>
public class LetterboxdApiException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LetterboxdApiException"/> class.
    /// </summary>
    public LetterboxdApiException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LetterboxdApiException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public LetterboxdApiException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LetterboxdApiException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public LetterboxdApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
