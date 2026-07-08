using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using LetterboxdSync.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace LetterboxdSync;

public class LetterboxdSyncTask : IScheduledTask
{
    private readonly ILogger _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IUserDataManager _userDataManager;

    public LetterboxdSyncTask(
        IUserManager userManager,
        ILoggerFactory loggerFactory,
        ILibraryManager libraryManager,
        IUserDataManager userDataManager)
    {
        _logger = loggerFactory.CreateLogger<LetterboxdSyncTask>();
        _userManager = userManager;
        _libraryManager = libraryManager;
        _userDataManager = userDataManager;
    }

    public string Name => "Played media sync with letterboxd";

    public string Key => "LetterboxdSync";

    public string Description => "Sync movies with Letterboxd";

    public string Category => "LetterboxdSync";

    private static PluginConfiguration Configuration =>
        Plugin.Instance!.Configuration;

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var lstUsers = _userManager.GetUsers();
        foreach (var user in lstUsers)
        {
            var account = Configuration.Accounts.FirstOrDefault(account => account.UserJellyfin == user.Id.ToString("N") && account.Enable);

            if (account == null)
            {
                continue;
            }

            var lstMoviesPlayed = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new List<BaseItemKind>() { BaseItemKind.Movie }.ToArray(),
                IsVirtualItem = false,
                IsPlayed = true,
            });

            if (lstMoviesPlayed.Count == 0)
            {
                continue;
            }

            // Apply date filtering if enabled
            if (account.EnableDateFilter)
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-account.DateFilterDays);
                lstMoviesPlayed = lstMoviesPlayed.Where(movie =>
                {
                    var userItemData = _userDataManager.GetUserData(user, movie);
                    return userItemData?.LastPlayedDate is { } lastPlayed && lastPlayed >= cutoffDate;
                }).ToList();
            }

            var api = new LetterboxdApi(_logger);
            try
            {
                await LetterboxdAuthenticator.AuthenticateAsync(api, account, _logger).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "{Message} User: {Username} ({UserId})",
                    ex.Message,
                    user.Username,
                    user.Id.ToString("N"));

                continue;
            }

            foreach (var movie in lstMoviesPlayed)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var userItemData = _userDataManager.GetUserData(user, movie);
                if (userItemData is null)
                {
                    continue;
                }

                try
                {
                    var logged = await LetterboxdMovieSync.LogMovieAsync(api, movie, user, userItemData, account, _logger).ConfigureAwait(false);

                    // Only pause between films we actually hit the API for, to stay well-mannered.
                    if (logged)
                    {
                        await Task.Delay(1000 + Random.Shared.Next(1000), cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        "{Message} User: {Username} ({UserId}) Movie: {Movie} StackTrace: {StackTrace}",
                        ex.Message,
                        user.Username,
                        user.Id.ToString("N"),
                        movie.OriginalTitle ?? movie.Name,
                        ex.StackTrace);
                }
            }
        }

        progress.Report(100);
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => new[]
    {
        new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.IntervalTrigger,
            IntervalTicks = TimeSpan.FromDays(1).Ticks
        }
    };
}
