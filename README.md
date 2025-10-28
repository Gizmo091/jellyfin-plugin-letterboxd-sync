<p align="center">
    <img src="/images/letterboxd-sync.png" width="70%">
</p>

<div align="center">
    <img alt="GitHub Release" src="https://img.shields.io/github/v/release/Gizmo091/jellyfin-plugin-letterboxd-sync">
    <img alt="GitHub Downloads (all assets, latest release)" src="https://img.shields.io/github/downloads/Gizmo091/jellyfin-plugin-letterboxd-sync/latest/total">
</div>

<p/>
    
<p align="center">
    A unofficial plugin to keep your watched movie history from Jellyfin automatically updated to your Letterboxd diary.
</p>

## About

This plugin sends daily updates to the Letterboxd diary informing the films watched on Jellyfin. Since the Letterboxd API is not publicly available, this project uses the HtmlAgilityPack package to interact directly with the website's interface.

## Installation

1. Open the dashboard in Jellyfin, then select `Catalog` and open `Settings` at the top with the `⚙️` button.

2. Click the `+` button and add the repository URL below, naming it whatever you like and save.

```
https://raw.githubusercontent.com/Gizmo091/jellyfin-plugin-letterboxd-sync/master/manifest.json
```

3. Go back to `Catalog`, click on 'LetterboxdSync' at 'General' group and install the most recent version.

4. Restart Jellyfin and go back to the plugin settings. Go to `My Plugins` and click on 'LetterboxdSync' to configure.
   
## Configure

 - You can associate one Letterboxd account for each Jellyfin user. You need click `Save` for each one.

 - The synchronization task runs every 24 hours and only for uses accounts marked as `Enable`.

 - Check `Send Favorite` if you want films marked as favorites on Jellyfin to be marked as favorites on Letterboxd.

 - By default the plugin will do a full sync to letterboxd. Once done initially its advised to `Enable Date Filtering` with a short lookback to avoid load on letterboxd.

<p align="center">
    <img src="/images/config-page.png" width="70%">
</p>
