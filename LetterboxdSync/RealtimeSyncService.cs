using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LetterboxdSync;

/// <summary>
/// Logs a film to Letterboxd the moment a user finishes watching it (or manually marks it played),
/// instead of waiting for the daily <see cref="LetterboxdSyncTask"/>. Subscribes to Jellyfin's
/// user-data-saved event for the lifetime of the server.
/// </summary>
public class RealtimeSyncService : IHostedService
{
    private readonly ILogger<RealtimeSyncService> _logger;
    private readonly IUserDataManager _userDataManager;
    private readonly IUserManager _userManager;

    public RealtimeSyncService(
        ILogger<RealtimeSyncService> logger,
        IUserDataManager userDataManager,
        IUserManager userManager)
    {
        _logger = logger;
        _userDataManager = userDataManager;
        _userManager = userManager;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _userDataManager.UserDataSaved += OnUserDataSaved;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _userDataManager.UserDataSaved -= OnUserDataSaved;
        return Task.CompletedTask;
    }

    private void OnUserDataSaved(object? sender, UserDataSaveEventArgs e)
    {
        // Only react when a film has just become "watched" (finished playback or a manual mark).
        if (e.SaveReason is not (UserDataSaveReason.PlaybackFinished or UserDataSaveReason.TogglePlayed))
        {
            return;
        }

        if (e.Item is not Movie movie)
        {
            return;
        }

        if (e.UserData is not { Played: true } userData)
        {
            return;
        }

        var userIdN = e.UserId.ToString("N");
        var account = Plugin.Instance?.Configuration.Accounts
            .FirstOrDefault(a => a.UserJellyfin == userIdN && a.Enable && a.EnableRealtimeSync);

        if (account == null)
        {
            return;
        }

        var user = _userManager.GetUserById(e.UserId);
        if (user == null)
        {
            return;
        }

        // Do the network work off the event thread; never let it throw back into Jellyfin.
        _ = Task.Run(async () =>
        {
            try
            {
                var api = new LetterboxdApi(_logger);
                await LetterboxdAuthenticator.AuthenticateAsync(api, account, _logger).ConfigureAwait(false);
                await LetterboxdMovieSync.LogMovieAsync(api, movie, user, userData, account, _logger).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Real-time Letterboxd sync failed for {Movie} (user {UserId}).",
                    movie.Name,
                    userIdN);
            }
        });
    }
}
