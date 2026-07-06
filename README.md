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

This plugin sends daily updates to your Letterboxd diary with the films you watched on Jellyfin. Since **v2.0.0** it talks to Letterboxd's **official REST API** (`api.letterboxd.com`) instead of scraping the website, which removes the Cloudflare `403` errors that used to require copying cookies.

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

- The synchronization task runs every 24 hours, only for accounts marked as `Enable`.

- Check `Send Favorite` if you want films marked as favorites on Jellyfin to also be marked as favorites on Letterboxd.

- By default the plugin does a full sync to Letterboxd. Once the initial sync is done, it's advised to `Enable Date Filtering` with a short lookback to reduce load.

<p align="center">
    <img src="/images/config-page.png" width="70%">
</p>

## Upgrading from 1.x

- **No more cookies.** The `Raw Cookies` / Cloudflare workaround is gone — you can clear that field. Just make sure you sign in with your **username** (not email).
- Existing configurations keep working: on the next sync the plugin swaps your stored password for a refresh token automatically.
