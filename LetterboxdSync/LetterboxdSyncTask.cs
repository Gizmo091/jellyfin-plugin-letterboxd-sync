using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;
using LetterboxdSync.Configuration;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using System.Linq;
using MediaBrowser.Model.Entities;
using System.Globalization;

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

    private static PluginConfiguration Configuration =>
            Plugin.Instance!.Configuration;

    public string Name => "Played media sync with letterboxd";

    public string Key => "LetterboxdSync";

    public string Description => "Sync movies with Letterboxd";

    public string Category => "LetterboxdSync";


    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var lstUsers = _userManager.GetUsers();
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
                    return userItemData.LastPlayedDate.HasValue && userItemData.LastPlayedDate.Value >= cutoffDate;
                }).ToList();
            }

            var api = new LetterboxdApi(_logger);
            try
            {
                await AuthenticateAccount(api, account).ConfigureAwait(false);
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
                var userItemData = _userDataManager.GetUserData(user, movie);
                bool favorite = movie.IsFavoriteOrLiked(user, userItemData) && account.SendFavorite;
                DateTime? viewingDate = userItemData.LastPlayedDate;
                string[] tags = new List<string>() { "" }.ToArray();

                if (int.TryParse(movie.GetProviderId(MetadataProvider.Tmdb), out tmdbid))
                {
                    try
                    {
                        var filmResult = await api.SearchFilmByTmdbId(tmdbid).ConfigureAwait(false);

                        if (filmResult == null)
                        {
                            _logger.LogWarning(
                                @"Film not found on Letterboxd
                                User: {Username} ({UserId})
                                Movie: {Movie} ({TmdbId})",
                                user.Username, user.Id.ToString("N"),
                                title, tmdbid);
                            continue;
                        }

                        // Letterboxd's diary needs a date; fall back to today if Jellyfin has none.
                        viewingDate = (viewingDate ?? DateTime.Now).Date;

                        // MarkAsWatched is idempotent server-side (204 = already logged), so no pre-check is needed.
                        await api.MarkAsWatched(filmResult.filmId, viewingDate, tags, favorite).ConfigureAwait(false);
                        _logger.LogInformation(
                            @"Film logged in Letterboxd
                            User: {Username} ({UserId})
                            Movie: {Movie} ({TmdbId})
                            Date: {ViewingDate}",
                            user.Username, user.Id.ToString("N"),
                            title, tmdbid, viewingDate);

                        // Small delay between films to stay well-mannered.
                        await Task.Delay(1000 + Random.Shared.Next(1000), cancellationToken).ConfigureAwait(false);
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
    }

    private static readonly object ConfigSaveLock = new();

    /// <summary>
    /// Authenticates <paramref name="api"/> for the given account, preferring a stored refresh token
    /// and falling back to username/password. A newly obtained or rotated refresh token is persisted
    /// and the plaintext password is dropped.
    /// </summary>
    private async Task AuthenticateAccount(LetterboxdApi api, Account account)
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
                _logger.LogWarning(ex, "Letterboxd refresh token rejected; falling back to password login if available.");
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

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfoType.IntervalTrigger,
                    IntervalTicks = TimeSpan.FromDays(1).Ticks
                }
            };
}
