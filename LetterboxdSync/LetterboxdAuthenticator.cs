using System;
using System.Threading.Tasks;
using LetterboxdSync.Configuration;
using Microsoft.Extensions.Logging;

namespace LetterboxdSync;

/// <summary>
/// Shared authentication for an <see cref="Account"/>: prefers the stored refresh token and falls
/// back to username/password, persisting any newly obtained/rotated refresh token (and dropping the
/// plaintext password). Used by every code path that needs an authenticated <see cref="LetterboxdApi"/>.
/// </summary>
internal static class LetterboxdAuthenticator
{
    private static readonly object ConfigSaveLock = new();

    /// <summary>
    /// Authenticates <paramref name="api"/> for <paramref name="account"/>. Throws
    /// <see cref="LetterboxdApiException"/> when no valid credentials are available.
    /// </summary>
    public static async Task AuthenticateAsync(LetterboxdApi api, Account account, ILogger logger)
    {
        var previousRefreshToken = account.RefreshToken;
        var authenticated = false;

        if (!string.IsNullOrWhiteSpace(account.RefreshToken))
        {
            try
            {
                await api.AuthenticateWithRefreshToken(account.RefreshToken!).ConfigureAwait(false);
                authenticated = true;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Letterboxd refresh token rejected; falling back to password login if available.");
            }
        }

        if (!authenticated)
        {
            if (string.IsNullOrWhiteSpace(account.UserLetterboxd) || string.IsNullOrWhiteSpace(account.PasswordLetterboxd))
            {
                throw new LetterboxdApiException("No valid Letterboxd credentials (refresh token expired and no password to re-authenticate).");
            }

            await api.AuthenticateWithPassword(account.UserLetterboxd!, account.PasswordLetterboxd!).ConfigureAwait(false);
        }

        // Persist a newly obtained / rotated refresh token, and drop the plaintext password.
        if (!string.IsNullOrEmpty(api.RefreshToken) && api.RefreshToken != previousRefreshToken)
        {
            lock (ConfigSaveLock)
            {
                account.RefreshToken = api.RefreshToken;
                account.PasswordLetterboxd = null;
                Plugin.Instance!.SaveConfiguration();
            }
        }
    }
}
