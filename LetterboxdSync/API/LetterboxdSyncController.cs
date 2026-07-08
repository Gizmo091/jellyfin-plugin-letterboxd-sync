using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using LetterboxdSync.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LetterboxdSync.API;

[ApiController]
[Authorize]
[Produces(MediaTypeNames.Application.Json)]
public class LetterboxdSyncController : ControllerBase
{
    private static readonly object _configLock = new();

    [HttpGet("Jellyfin.Plugin.LetterboxdSync/ClientScript")]
    [AllowAnonymous]
    [Produces("application/javascript")]
    public ActionResult GetClientScript()
    {
        var resourceName = $"{typeof(Plugin).Namespace}.Web.plugin.js";
        var stream = typeof(Plugin).Assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return NotFound();
        }

        return File(stream, "application/javascript");
    }

    [HttpPost("Jellyfin.Plugin.LetterboxdSync/Authenticate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ActionResult> Authenticate([FromBody] Account body)
        => LinkCredentials(body);

    [HttpPost("Jellyfin.Plugin.LetterboxdSync/UserAuthenticate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ActionResult> UserAuthenticate([FromBody] Account body)
        => LinkCredentials(body);

    [HttpGet("Jellyfin.Plugin.LetterboxdSync/UserConfig")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult GetUserConfig()
    {
        var userIdStr = GetCurrentUserId().ToString("N");

        var account = Plugin.Instance!.Configuration.Accounts.FirstOrDefault(a =>
            string.Equals(a.UserJellyfin, userIdStr, StringComparison.OrdinalIgnoreCase));

        // Never expose secrets (refresh token / password) to the browser.
        return Ok(new
        {
            userJellyfin = userIdStr,
            userLetterboxd = account?.UserLetterboxd,
            enable = account?.Enable ?? false,
            sendFavorite = account?.SendFavorite ?? false,
            sendRating = account?.SendRating ?? true,
            enableRealtimeSync = account?.EnableRealtimeSync ?? true,
            importDiary = account?.ImportDiary ?? false,
            enableDateFilter = account?.EnableDateFilter ?? false,
            dateFilterDays = account?.DateFilterDays ?? 7,
            watchlists = (account?.GetEffectiveWatchlists() ?? new List<WatchlistEntry>())
                .Select(w => new { input = w.Input, autoRequest = w.AutoRequest }),
            seerrConfigured = !string.IsNullOrWhiteSpace(Plugin.Instance!.Configuration.SeerrUrl)
                && !string.IsNullOrWhiteSpace(Plugin.Instance!.Configuration.SeerrApiKey),
            isLinked = !string.IsNullOrWhiteSpace(account?.RefreshToken),
        });
    }

    [HttpPost("Jellyfin.Plugin.LetterboxdSync/UserConfig")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> SaveUserConfig([FromBody] Account body)
    {
        var userIdStr = GetCurrentUserId().ToString("N");

        // Force the account to the authenticated user's ID for security.
        body.UserJellyfin = userIdStr;

        var existing = Plugin.Instance!.Configuration.Accounts.FirstOrDefault(a =>
            string.Equals(a.UserJellyfin, userIdStr, StringComparison.OrdinalIgnoreCase));

        // If a password was supplied, exchange it for a refresh token now; otherwise keep the
        // previously linked token so the user can edit settings without re-entering credentials.
        if (!string.IsNullOrWhiteSpace(body.PasswordLetterboxd) && !string.IsNullOrWhiteSpace(body.UserLetterboxd))
        {
            var api = new LetterboxdApi();
            try
            {
                await api.AuthenticateWithPassword(body.UserLetterboxd!, body.PasswordLetterboxd!).ConfigureAwait(false);
                body.RefreshToken = api.RefreshToken;
            }
            catch (Exception ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }
        else
        {
            body.RefreshToken = existing?.RefreshToken;
        }

        // Never persist the plaintext password.
        body.PasswordLetterboxd = null;

        lock (_configLock)
        {
            var config = Plugin.Instance!.Configuration;
            for (int i = config.Accounts.Count - 1; i >= 0; i--)
            {
                if (string.Equals(config.Accounts[i].UserJellyfin, userIdStr, StringComparison.OrdinalIgnoreCase))
                {
                    config.Accounts.RemoveAt(i);
                }
            }

            config.Accounts.Add(body);
            Plugin.Instance.SaveConfiguration();
        }

        return Ok();
    }

    /// <summary>Validates credentials by performing the OAuth grant and returns the resulting refresh token.</summary>
    private async Task<ActionResult> LinkCredentials(Account body)
    {
        var api = new LetterboxdApi();
        try
        {
            if (!string.IsNullOrWhiteSpace(body.RefreshToken))
            {
                await api.AuthenticateWithRefreshToken(body.RefreshToken!).ConfigureAwait(false);
            }
            else
            {
                await api.AuthenticateWithPassword(body.UserLetterboxd ?? string.Empty, body.PasswordLetterboxd ?? string.Empty).ConfigureAwait(false);
            }

            return Ok(new { refreshToken = api.RefreshToken });
        }
        catch (Exception ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("Jellyfin-UserId");
        if (claim != null && Guid.TryParse(claim.Value, out var userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException();
    }
}
