using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using LetterboxdSync.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace LetterboxdSync;

/// <summary>
/// Reverse sync: reads the member's Letterboxd diary and marks the matching films as watched in
/// Jellyfin (using the diary date). Opt-in per account via <see cref="Account.ImportDiary"/>.
/// </summary>
public class LetterboxdDiaryImportTask : IScheduledTask
{
    private readonly ILogger _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IUserDataManager _userDataManager;

    public LetterboxdDiaryImportTask(
        IUserManager userManager,
        ILoggerFactory loggerFactory,
        ILibraryManager libraryManager,
        IUserDataManager userDataManager)
    {
        _logger = loggerFactory.CreateLogger<LetterboxdDiaryImportTask>();
        _userManager = userManager;
        _libraryManager = libraryManager;
        _userDataManager = userDataManager;
    }

    public string Name => "Import Letterboxd diary to Jellyfin";

    public string Key => "LetterboxdDiaryImport";

    public string Description => "Mark films as watched in Jellyfin from your Letterboxd diary";

    public string Category => "LetterboxdSync";

    private static PluginConfiguration Configuration => Plugin.Instance!.Configuration;

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var users = _userManager.GetUsers().ToList();
        var totalUsers = users.Count;
        var processedUsers = 0;

        foreach (var user in users)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var account = Configuration.Accounts.FirstOrDefault(a =>
                a.UserJellyfin == user.Id.ToString("N") && a.Enable && a.ImportDiary);

            if (account != null)
            {
                try
                {
                    await ImportForUserAsync(user, account, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error importing Letterboxd diary for user {Username} ({UserId})",
                        user.Username,
                        user.Id.ToString("N"));
                }
            }

            processedUsers++;
            progress.Report((double)processedUsers / totalUsers * 100);
        }

        progress.Report(100);
    }

    private async Task ImportForUserAsync(User user, Account account, CancellationToken cancellationToken)
    {
        var api = new LetterboxdApi(_logger);
        await LetterboxdAuthenticator.AuthenticateAsync(api, account, _logger).ConfigureAwait(false);

        var memberId = await api.GetAuthenticatedMemberId().ConfigureAwait(false);
        if (string.IsNullOrEmpty(memberId))
        {
            _logger.LogWarning("Could not resolve the Letterboxd member id for user {UserId}; skipping diary import.", user.Id.ToString("N"));
            return;
        }

        var entries = await api.GetDiaryEntries(memberId).ConfigureAwait(false);
        if (entries.Count == 0)
        {
            _logger.LogInformation("Letterboxd diary for user {UserId} is empty.", user.Id.ToString("N"));
            return;
        }

        // Keep the most recent diary date per film.
        var watchedDates = new Dictionary<string, DateTime>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            if (string.IsNullOrEmpty(entry.TmdbId))
            {
                continue;
            }

            var date = entry.DiaryDate ?? DateTime.Now;
            if (!watchedDates.TryGetValue(entry.TmdbId, out var existing) || date > existing)
            {
                watchedDates[entry.TmdbId] = date;
            }
        }

        var movies = _libraryManager.GetItemList(new InternalItemsQuery(user)
        {
            IncludeItemTypes = new[] { BaseItemKind.Movie },
            IsVirtualItem = false,
            Recursive = true,
            HasTmdbId = true,
        });

        var imported = 0;
        foreach (var movie in movies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tmdbId = movie.GetProviderId(MetadataProvider.Tmdb);
            if (string.IsNullOrEmpty(tmdbId) || !watchedDates.TryGetValue(tmdbId, out var watchedDate))
            {
                continue;
            }

            var userData = _userDataManager.GetUserData(user, movie);
            if (userData is null || userData.Played)
            {
                continue;
            }

            userData.Played = true;
            userData.LastPlayedDate = watchedDate;
            if (userData.PlayCount < 1)
            {
                userData.PlayCount = 1;
            }

            _userDataManager.SaveUserData(user, movie, userData, UserDataSaveReason.Import, cancellationToken);
            imported++;

            _logger.LogInformation(
                "Marked '{Movie}' watched in Jellyfin from Letterboxd diary (user {UserId}, date {Date}).",
                movie.Name,
                user.Id.ToString("N"),
                watchedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        _logger.LogInformation(
            "Letterboxd diary import complete for user {UserId}: {Imported} newly marked watched ({DiaryCount} diary films).",
            user.Id.ToString("N"),
            imported,
            watchedDates.Count);
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => new[]
    {
        new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.DailyTrigger,
            TimeOfDayTicks = TimeSpan.FromHours(4).Ticks
        }
    };
}
