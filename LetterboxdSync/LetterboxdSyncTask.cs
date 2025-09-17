using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using LetterboxdSync.Configuration;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using System.Linq;
using MediaBrowser.Model.Entities;
using System.Globalization;
using MediaBrowser.Model.Activity;

namespace LetterboxdSync;

public class LetterboxdSyncTask : IScheduledTask
{
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IUserDataManager _userDataManager;
    private readonly IActivityManager _activityManager;

    public LetterboxdSyncTask(
            IUserManager userManager,
            ILoggerFactory loggerFactory,
            ILibraryManager libraryManager,
            IActivityManager activityManager,
            IUserDataManager userDataManager)
        {
            _logger = loggerFactory.CreateLogger<LetterboxdSyncTask>();
            _loggerFactory = loggerFactory;
            _userManager = userManager;
            _libraryManager = libraryManager;
            _activityManager = activityManager;
            _userDataManager = userDataManager;
        }

    private static PluginConfiguration Configuration =>
            Plugin.Instance!.Configuration;

    public string Name => "Played media sync with letterboxd";

    public string Key => "LetterboxdSync";

    public string Description => "Sync movies with Letterboxd";

    public string Category => "LetterboxdSync";


    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var lstUsers = _userManager.Users;
        foreach (var user in lstUsers)
        {
            var account = Configuration.Accounts.FirstOrDefault(account => account.UserJellyfin == user.Id.ToString("N") && account.Enable);

            if (account == null)
                continue;

            var lstMoviesPlayed = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new List<BaseItemKind>() { BaseItemKind.Movie }.ToArray(),
                IsVirtualItem = false,
                IsPlayed = true,
            });

            if (lstMoviesPlayed.Count == 0)
                continue;

            var api = new LetterboxdApi();
            try
            {
                await api.Authenticate(account.UserLetterboxd, account.PasswordLetterboxd).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    @"{Message}
                    User: {Username} ({UserId})",
                    ex.Message,
                    user.Username, user.Id.ToString("N"));

                continue;
            }

            foreach (var movie in lstMoviesPlayed)
            {
                int tmdbid;
                string title = movie.OriginalTitle;
                bool favorite = movie.IsFavoriteOrLiked(user) && account.SendFavorite;
                DateTime? viewingDate = _userDataManager.GetUserData(user, movie).LastPlayedDate;
                string[] tags = new List<string>() { "" }.ToArray();

                if (int.TryParse(movie.GetProviderId(MetadataProvider.Tmdb), out tmdbid))
                {
                    try
                    {
                        var filmResult = await api.SearchFilmByTmdbId(tmdbid).ConfigureAwait(false);

                        var dateLastLog = await api.GetDateLastLog(filmResult.filmSlug).ConfigureAwait(false);
                        viewingDate = new DateTime(viewingDate.Value.Year, viewingDate.Value.Month, viewingDate.Value.Day);

                        if (dateLastLog != null && dateLastLog.Value.Date == viewingDate.Value.Date)
                        {
                            _logger.LogWarning(
                                @"Film has been logged into Letterboxd previously ({Date})
                                User: {Username} ({UserId})
                                Movie: {Movie} ({TmdbId})",
                                ((DateTime)dateLastLog).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                                user.Username, user.Id.ToString("N"),
                                title, tmdbid);
                        }
                        else
                        {
                            await api.MarkAsWatched(filmResult.filmId, viewingDate, tags, favorite).ConfigureAwait(false);

                            await _activityManager.CreateAsync(new ActivityLog($"\"{title}\" log in Letterboxd", "LetterboxdSync", Guid.Empty) {
                                ShortOverview = $"Last played by {user.Username} at {viewingDate}",
                                Overview = $"Movie \"{title}\"({tmdbid}) played by Jellyfin user {user.Username} at {viewingDate} was log in Letterboxd diary of {account.UserLetterboxd} account",
                            }).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            @"{Message}
                            User: {Username} ({UserId})
                            Movie: {Movie} ({TmdbId})
                            StackTrace: {StackTrace}",
                            ex.Message,
                            user.Username, user.Id.ToString("N"),
                            title, tmdbid,
                            ex.StackTrace);
                    }
                }
                else
                {
                    _logger.LogWarning(
                        @"Film does not have TmdbId
                        User: {Username} ({UserId})
                        Movie: {Movie}",
                        user.Username, user.Id.ToString("N"),
                        title);
                }
            }
        }

        progress.Report(100);
        return;
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerInterval,
                    IntervalTicks = TimeSpan.FromDays(1).Ticks
                }
            };
}
