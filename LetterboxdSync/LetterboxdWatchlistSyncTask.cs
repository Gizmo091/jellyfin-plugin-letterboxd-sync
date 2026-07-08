using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using LetterboxdSync.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Playlists;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace LetterboxdSync;

public class LetterboxdWatchlistSyncTask : IScheduledTask
{
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILibraryManager _libraryManager;
    private readonly IPlaylistManager _playlistManager;
    private readonly IUserManager _userManager;

    public LetterboxdWatchlistSyncTask(
            IUserManager userManager,
            ILoggerFactory loggerFactory,
            ILibraryManager libraryManager,
            IPlaylistManager playlistManager)
    {
        _logger = loggerFactory.CreateLogger<LetterboxdWatchlistSyncTask>();
        _loggerFactory = loggerFactory;
        _userManager = userManager;
        _libraryManager = libraryManager;
        _playlistManager = playlistManager;
    }

    private static PluginConfiguration Configuration =>
            Plugin.Instance!.Configuration;

    public string Name => "Sync Letterboxd Watchlists";

    public string Key => "LetterboxdWatchlistSync";

    public string Description => "Sync Letterboxd watchlists to Jellyfin Playlists";

    public string Category => "LetterboxdSync";

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var users = _userManager.GetUsers().ToList();
        var totalUsers = users.Count;
        var processedUsers = 0;

        foreach (var user in users)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var account = Configuration.Accounts.FirstOrDefault(a =>
                string.Equals(a.UserJellyfin, user.Id.ToString("N"), StringComparison.OrdinalIgnoreCase));

            var watchlists = account?.GetEffectiveWatchlists();
            if (account == null || watchlists == null || watchlists.Count == 0)
            {
                processedUsers++;
                progress.Report((double)processedUsers / totalUsers * 100);
                continue;
            }

            // Set up Seerr once per user, only when at least one watchlist wants auto-requesting.
            SeerrClient? seerr = null;
            int? seerrUserId = null;
            if (watchlists.Any(w => w.AutoRequest)
                && !string.IsNullOrWhiteSpace(Configuration.SeerrUrl)
                && !string.IsNullOrWhiteSpace(Configuration.SeerrApiKey))
            {
                seerr = new SeerrClient(Configuration.SeerrUrl!, Configuration.SeerrApiKey!, _logger);
                try
                {
                    seerrUserId = await seerr.ResolveUserIdByJellyfin(user.Id, user.Username).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not resolve the Seerr user for {Username}; auto-requesting is skipped.", user.Username);
                }

                if (seerrUserId == null)
                {
                    _logger.LogWarning("No Seerr account maps to Jellyfin user {Username}; auto-requesting is skipped for them.", user.Username);
                    seerr = null;
                }
            }

            foreach (var entry in watchlists)
            {
                if (string.IsNullOrWhiteSpace(entry.Input))
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await SyncWatchlistForUser(user.Id, entry, seerr, seerrUserId, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error syncing watchlist for user {Username} ({UserId}), watchlist {Input}",
                        user.Username,
                        user.Id.ToString("N"),
                        entry.Input);
                }
            }

            processedUsers++;
            progress.Report((double)processedUsers / totalUsers * 100);
        }

        progress.Report(100);
    }

    private async Task SyncWatchlistForUser(Guid jellyfinUserId, WatchlistEntry entry, SeerrClient? seerr, int? seerrUserId, CancellationToken cancellationToken)
    {
        var watchlistInput = entry.Input!;
        var target = await LetterboxdApi.ResolveWatchlistInput(watchlistInput).ConfigureAwait(false);

        _logger.LogInformation(
            "Syncing '{PlaylistName}' (input: {Input}) to Jellyfin user {UserId}",
            target.DisplayName,
            watchlistInput,
            jellyfinUserId.ToString("N"));

        var api = new LetterboxdApi(_logger);

        List<FilmResult> watchlistFilms;
        if (string.IsNullOrEmpty(target.ListSlug))
        {
            if (string.IsNullOrEmpty(target.Username))
            {
                _logger.LogWarning("Could not determine a Letterboxd username from input '{Input}'", watchlistInput);
                return;
            }

            var memberId = await api.ResolveMemberId(target.Username).ConfigureAwait(false);
            if (memberId == null)
            {
                _logger.LogWarning("Letterboxd member '{Username}' not found", target.Username);
                return;
            }

            watchlistFilms = await api.GetWatchlist(memberId).ConfigureAwait(false);
        }
        else
        {
            var listId = await api.ResolveListId(target.Username!, target.ListSlug).ConfigureAwait(false);
            if (listId == null)
            {
                _logger.LogWarning("Letterboxd list '{User}/{Slug}' could not be resolved via the API", target.Username, target.ListSlug);
                return;
            }

            watchlistFilms = await api.GetListEntries(listId).ConfigureAwait(false);
        }

        if (watchlistFilms.Count == 0)
        {
            _logger.LogInformation("'{PlaylistName}' is empty or does not exist", target.DisplayName);
            return;
        }

        var watchlistTmdbIds = watchlistFilms
            .Select(f => f.TmdbId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Select(id => id!)
            .ToHashSet();

        // Find matching movies in the Jellyfin library
        var allMovies = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Movie },
            IsVirtualItem = false,
            Recursive = true,
            HasTmdbId = true
        });

        var libraryTmdbIds = allMovies
            .Select(m => m.GetProviderId(MetadataProvider.Tmdb))
            .Where(id => !string.IsNullOrEmpty(id))
            .Select(id => id!)
            .ToHashSet();

        // Auto-request films that are on the watchlist but missing from the library (runs regardless
        // of whether a playlist can be built, so it happens even when nothing is in the library yet).
        if (entry.AutoRequest && seerr != null && seerrUserId != null)
        {
            await RequestMissingFilms(watchlistTmdbIds, libraryTmdbIds, seerr, seerrUserId.Value, target.DisplayName, cancellationToken).ConfigureAwait(false);
        }

        var matchedItems = allMovies
            .Where(m => watchlistTmdbIds.Contains(m.GetProviderId(MetadataProvider.Tmdb) ?? string.Empty))
            .ToList();

        if (matchedItems.Count == 0)
        {
            _logger.LogInformation(
                "No matching movies found in library for '{PlaylistName}' ({WatchlistCount} films in list)",
                target.DisplayName,
                watchlistFilms.Count);
            return;
        }

        var matchedItemIds = matchedItems.Select(m => m.Id).ToHashSet();

        // Find or create the playlist
        string playlistName = target.DisplayName;

        var existingPlaylists = _playlistManager.GetPlaylists(jellyfinUserId);
        var playlist = existingPlaylists.FirstOrDefault(p =>
            string.Equals(p.Name, playlistName, StringComparison.OrdinalIgnoreCase));

        if (playlist == null)
        {
            // Create new playlist with all matched items
            await _playlistManager.CreatePlaylist(new PlaylistCreationRequest
            {
                Name = playlistName,
                UserId = jellyfinUserId,
                MediaType = MediaType.Video,
                ItemIdList = matchedItemIds.ToArray(),
                Public = false
            }).ConfigureAwait(false);

            _logger.LogInformation(
                "Created playlist '{PlaylistName}' with {Count} items for user {UserId}",
                playlistName,
                matchedItems.Count,
                jellyfinUserId.ToString("N"));
            return;
        }

        // Playlist exists: add only items not already present
        var currentItemIds = playlist.GetLinkedChildren()
            .Select(c => c.Id)
            .ToHashSet();

        var itemsToAdd = matchedItemIds.Except(currentItemIds).ToList();

        if (itemsToAdd.Count == 0)
        {
            _logger.LogInformation(
                "Playlist '{PlaylistName}' is already up to date ({Count} items)",
                playlistName,
                currentItemIds.Count);
            return;
        }

        await _playlistManager.AddItemToPlaylistAsync(
            playlist.Id,
            itemsToAdd.AsReadOnly(),
            jellyfinUserId).ConfigureAwait(false);

        _logger.LogInformation(
            "Added {AddCount} items to playlist '{PlaylistName}' (now {TotalCount} items)",
            itemsToAdd.Count,
            playlistName,
            currentItemIds.Count + itemsToAdd.Count);
    }

    private async Task RequestMissingFilms(
        IReadOnlyCollection<string> watchlistTmdbIds,
        HashSet<string> libraryTmdbIds,
        SeerrClient seerr,
        int seerrUserId,
        string displayName,
        CancellationToken cancellationToken)
    {
        var missing = watchlistTmdbIds.Where(id => !libraryTmdbIds.Contains(id)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        int created = 0, alreadyThere = 0, failed = 0;
        foreach (var tmdb in missing)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!int.TryParse(tmdb, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tmdbId))
            {
                continue;
            }

            var outcome = await seerr.RequestMovieAsync(tmdbId, seerrUserId).ConfigureAwait(false);
            switch (outcome)
            {
                case SeerrRequestOutcome.Created:
                    created++;
                    break;
                case SeerrRequestOutcome.AlreadyExists:
                    alreadyThere++;
                    break;
                default:
                    failed++;
                    break;
            }

            // Be gentle with the Seerr instance.
            await Task.Delay(500 + Random.Shared.Next(500), cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation(
            "Seerr auto-request for '{PlaylistName}': {Created} requested, {Existing} already present, {Failed} failed ({Missing} missing from library).",
            displayName,
            created,
            alreadyThere,
            failed,
            missing.Count);
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => new[]
    {
        new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.DailyTrigger,
            TimeOfDayTicks = TimeSpan.FromHours(3).Ticks
        }
    };
}
