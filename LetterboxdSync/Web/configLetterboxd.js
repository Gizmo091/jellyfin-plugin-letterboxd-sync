
export const pluginId = 'b1fb3d98-3336-4b87-a5c9-8a948bd87233';

export default function (view, params) {

    // Refresh token of the account currently loaded in the form (never shown in an input).
    var currentRefreshToken = null;

    function addWatchlistEntry(container, value) {
        var row = document.createElement('div');
        row.className = 'inputContainer';
        row.style.display = 'flex';
        row.style.alignItems = 'center';
        row.style.gap = '10px';

        var input = document.createElement('input');
        input.setAttribute('is', 'emby-input');
        input.type = 'text';
        input.className = 'watchlist-entry';
        input.setAttribute('label', 'Watchlist link or Letterboxd profile');
        input.setAttribute('autocomplete', 'off');
        input.value = value || '';
        input.style.flex = '1';

        var removeBtn = document.createElement('button');
        removeBtn.setAttribute('is', 'emby-button');
        removeBtn.type = 'button';
        removeBtn.className = 'raised';
        removeBtn.textContent = 'Remove';
        removeBtn.addEventListener('click', function () {
            row.remove();
        });

        row.appendChild(input);
        row.appendChild(removeBtn);
        container.appendChild(row);
    }

    function loadWatchlistForUser(accountData) {
        var container = view.querySelector('#watchlistContainer');
        container.innerHTML = '';
        var usernames = accountData.WatchlistUsernames || [];
        for (var i = 0; i < usernames.length; i++) {
            addWatchlistEntry(container, usernames[i]);
        }
    }

    function getWatchlistUsernames() {
        var watchlistInputs = view.querySelectorAll('.watchlist-entry');
        var usernames = [];
        for (var i = 0; i < watchlistInputs.length; i++) {
            var val = watchlistInputs[i].value.trim();
            if (val) {
                usernames.push(val);
            }
        }
        return usernames;
    }

    function setLinkStatus(account) {
        var status = view.querySelector('#lbxd-link-status');
        if (!status) {
            return;
        }
        if (account && account.RefreshToken) {
            status.textContent = 'Account linked ✓. Leave the password blank to keep the existing link, or enter it again to re-link.';
        } else {
            status.textContent = 'Not linked. Enter the account credentials to link. The password is exchanged for a token on save and is never stored.';
        }
    }

    function populate(account) {
        account = account || {};
        currentRefreshToken = account.RefreshToken || null;
        view.querySelector('#lbxd-account').value = account.UserLetterboxd || '';
        view.querySelector('#lbxd-key').value = '';
        view.querySelector('#enable').checked = account.Enable || false;
        view.querySelector('#sendfavorite').checked = account.SendFavorite || false;
        view.querySelector('#enabledatefilter').checked = account.EnableDateFilter || false;
        view.querySelector('#datefilterdays').value = account.DateFilterDays || 7;
        loadWatchlistForUser(account);
        setLinkStatus(account);
    }

    function findAccount(config, userId) {
        var matches = config.Accounts.filter(function (item) {
            return item.UserJellyfin == userId;
        });
        return matches.length > 0 ? matches[0] : null;
    }

    view.addEventListener('viewshow', function () {
        const selectUsers = view.querySelector('#usersJellyfin');
        selectUsers.innerHTML = '';

        ApiClient.getUsers().then(users => {
            for (let user of users) {
                const option = document.createElement('option');
                option.value = user.Id;
                option.textContent = user.Name;
                selectUsers.appendChild(option);
            }

            const userSelectedId = selectUsers.value;
            ApiClient.getPluginConfiguration(pluginId).then(config => {
                populate(findAccount(config, userSelectedId));
            });
        });
    });

    view.querySelector('#addWatchlistBtn').addEventListener('click', function () {
        addWatchlistEntry(view.querySelector('#watchlistContainer'), '');
    });

    view.querySelector('#usersJellyfin').addEventListener('change', function (e) {
        e.preventDefault();
        const userSelectedId = e.target.value;
        ApiClient.getPluginConfiguration(pluginId).then(config => {
            populate(findAccount(config, userSelectedId));
        });
    });

    view.querySelector('#LetterboxdSyncConfigForm').addEventListener('submit', function (e) {
        e.preventDefault();
        Dashboard.showLoadingMsg();

        const userSelectedId = view.querySelector('#usersJellyfin').value;
        const password = view.querySelector('#lbxd-key').value.trim();

        ApiClient.getPluginConfiguration(pluginId).then(config => {
            let accountsUpdate = config.Accounts.filter(function (account) {
                return account.UserJellyfin != userSelectedId;
            });

            let configUser = {};
            configUser.UserJellyfin = userSelectedId;
            configUser.UserLetterboxd = view.querySelector('#lbxd-account').value;
            configUser.Enable = view.querySelector('#enable').checked;
            configUser.SendFavorite = view.querySelector('#sendfavorite').checked;
            configUser.EnableDateFilter = view.querySelector('#enabledatefilter').checked;
            configUser.DateFilterDays = parseInt(view.querySelector('#datefilterdays').value) || 7;
            configUser.WatchlistUsernames = getWatchlistUsernames();

            // If a password was entered, use it to (re)link; otherwise keep the existing token.
            if (password) {
                configUser.PasswordLetterboxd = password;
                configUser.RefreshToken = null;
            } else {
                configUser.PasswordLetterboxd = null;
                configUser.RefreshToken = currentRefreshToken;
            }

            function persist() {
                configUser.PasswordLetterboxd = null;
                accountsUpdate.push(configUser);
                config.Accounts = accountsUpdate;
                ApiClient.updatePluginConfiguration(pluginId, config).then(function (result) {
                    currentRefreshToken = configUser.RefreshToken || null;
                    setLinkStatus(configUser);
                    Dashboard.processPluginConfigurationUpdateResult(result);
                });
            }

            // When disabled, or when there is nothing to (re)validate, just save.
            if (!configUser.Enable || (!password && !configUser.RefreshToken)) {
                Dashboard.hideLoadingMsg();
                persist();
                return;
            }

            const url = ApiClient.getUrl('Jellyfin.Plugin.LetterboxdSync/Authenticate');
            const data = JSON.stringify(configUser);

            ApiClient.ajax({ type: 'POST', url, data, contentType: 'application/json' }).then(function (response) {
                Dashboard.hideLoadingMsg();
                response.json().then(function (res) {
                    if (res && res.refreshToken) {
                        configUser.RefreshToken = res.refreshToken;
                    }
                    persist();
                });
            }).catch(function (response) {
                Dashboard.hideLoadingMsg();
                response.json().then(res => {
                    Dashboard.processErrorResponse({ statusText: `${response.statusText} - ${res.Message}` });
                });
            });
        });
    });
}
