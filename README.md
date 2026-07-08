<p align="center">
    <img src="/images/letterboxd-sync.png" width="70%">
</p>

<div align="center">
    <img alt="GitHub Release" src="https://img.shields.io/github/v/release/Gizmo091/jellyfin-plugin-letterboxd-sync">
    <img alt="GitHub Downloads (all assets, latest release)" src="https://img.shields.io/github/downloads/Gizmo091/jellyfin-plugin-letterboxd-sync/latest/total">
</div>

<p/>

<p align="center">
    An unofficial plugin to keep your watched movie history from Jellyfin automatically updated to your Letterboxd diary.
</p>

## About

This plugin keeps your Jellyfin watch history in sync with your Letterboxd diary. Since **v2.0.0** it talks to Letterboxd's **official REST API** (`api.letterboxd.com`) instead of scraping the website, which removes the Cloudflare `403` errors that used to require copying cookies.

### Features

- **Diary sync** — films you finish on Jellyfin are logged to your Letterboxd diary, either **in real time** (as soon as playback finishes) or via the daily catch-up task.
- **Ratings** — your Jellyfin personal rating (0-10) is sent as a Letterboxd star rating (0.5-5.0).
- **Favorites** — films favorited on Jellyfin are marked as "liked" on Letterboxd.
- **Rewatches** — a film played more than once is logged as a rewatch.
- **Watchlist import** — any public Letterboxd watchlist or list is mirrored into a Jellyfin playlist.
- **Seerr auto-request** *(opt-in)* — films on a watchlist that are missing from your library can be automatically requested in [Seerr](https://github.com/seerr-team/seerr) / Jellyseerr / Overseerr, on behalf of the matching Jellyfin user.
- **Diary import** *(opt-in)* — the reverse direction: films in your Letterboxd diary are marked as watched in Jellyfin.
- **Resilient** — API calls automatically retry rate limits (`429`) and server errors with exponential backoff.

## Requirements

- **Jellyfin ≥ 10.11.9** — earlier versions are not supported (the plugin uses an API that changed in 10.11.9).
- **[File Transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation)** plugin *(optional)* — needed only to inject the Letterboxd entry in the sidebar for non-admin users. Without it, admin configuration still works fine.

## Installation

1. Open the dashboard in Jellyfin, then select `Catalog` and open `Settings` at the top with the `⚙️` button.

2. Click the `+` button and add the repository URL below, naming it whatever you like, and save.

```
https://raw.githubusercontent.com/Gizmo091/jellyfin-plugin-letterboxd-sync/master/manifest.json
```

3. Go back to `Catalog`, click on 'LetterboxdSync' in the 'General' group and install the most recent version.

4. Restart Jellyfin, then go to the plugin settings (`My Plugins` → 'LetterboxdSync') to configure.

## Configure

- You can associate one Letterboxd account with each Jellyfin user. Click `Save` for each one.

- **Sign in with your Letterboxd _username_ (the one in `letterboxd.com/<username>/`), not your email address** — Letterboxd no longer allows sign-in by email. Enter your username and password once; the plugin exchanges them for a token and **never stores your password**.

- The daily catch-up task runs every 24 hours, only for accounts marked as `Enable`.

- **`Real-time Sync`** (on by default) logs a film to Letterboxd the moment you finish watching it, so you don't have to wait for the daily task. The daily task still runs as a safety net — logging is idempotent, so nothing is duplicated.

- Check `Send Favorite` if you want films marked as favorites on Jellyfin to also be marked as favorites on Letterboxd.

- **`Send Rating`** (on by default) sends your Jellyfin personal rating as a Letterboxd star rating. Because Letterboxd only records a rating when a film is first logged, this never overwrites a rating you set on Letterboxd afterwards.

- **`Import Diary`** (off by default) does the reverse: it reads your Letterboxd diary once a day and marks the matching films as watched in Jellyfin (using the diary date). It only ever marks films you haven't already watched in Jellyfin.

- **Seerr auto-request** (off by default): set a **Seerr URL** and **API key** in the admin plugin settings, then tick **`Auto-request`** on any watchlist. During the watchlist sync, films on that list that are missing from your library are requested in Seerr **as the matching Jellyfin user** (mapped via `jellyfinUserId`), so Seerr applies that user's own approval and quota rules. If no Seerr account maps to the user, auto-requesting is skipped for them.

- By default the plugin does a full sync to Letterboxd. Once the initial sync is done, it's advised to `Enable Date Filtering` with a short lookback to reduce load.

<p align="center">
    <img src="/images/config-page.png" width="70%">
</p>

## Upgrading from 1.x

- **No more cookies.** The `Raw Cookies` / Cloudflare workaround is gone — you can clear that field. Just make sure you sign in with your **username** (not email).
- Existing configurations keep working: on the next sync the plugin swaps your stored password for a refresh token automatically.
