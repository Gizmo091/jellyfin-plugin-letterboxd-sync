using System;
using System.Threading.Tasks;
using Jellyfin.Plugin.LetterboxdSync.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.LetterboxdSync;

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
        var hadRefreshToken = !string.IsNullOrWhiteSpace(account.RefreshToken);
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
                // Distinguish "the token was refused" from "there was never a token": the second
                // case points at a configuration that was lost, not at an expiry.
                throw new LetterboxdApiException(hadRefreshToken
                    ? "Letterboxd rejected the stored token and no password is available to re-authenticate. Re-enter the account password in the plugin settings."
                    : "No Letterboxd token is stored for this account. Enter the account credentials in the plugin settings to link it.");
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
