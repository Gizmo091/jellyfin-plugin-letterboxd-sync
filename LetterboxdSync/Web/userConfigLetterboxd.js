
export default function (view, params) {

    function addWatchlistEntry(container, value, autoRequest) {
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

        var autoLabel = document.createElement('label');
        autoLabel.style.display = 'flex';
        autoLabel.style.alignItems = 'center';
        autoLabel.style.gap = '4px';
        autoLabel.style.whiteSpace = 'nowrap';
        var autoCheck = document.createElement('input');
        autoCheck.type = 'checkbox';
        autoCheck.className = 'watchlist-autorequest';
        autoCheck.checked = !!autoRequest;
        autoLabel.appendChild(autoCheck);
        autoLabel.appendChild(document.createTextNode('Auto-request'));

        var removeBtn = document.createElement('button');
        removeBtn.setAttribute('is', 'emby-button');
        removeBtn.type = 'button';
        removeBtn.className = 'raised';
        removeBtn.textContent = 'Remove';
        removeBtn.addEventListener('click', function () {
            row.remove();
        });

        row.appendChild(input);
        row.appendChild(autoLabel);
        row.appendChild(removeBtn);
        container.appendChild(row);
    }

    function getWatchlists() {
        var container = view.querySelector('#watchlistContainer');
        var inputs = container.querySelectorAll('.watchlist-entry');
        var result = [];
        for (var i = 0; i < inputs.length; i++) {
            var val = inputs[i].value.trim();
            if (!val) {
                continue;
            }
            var row = inputs[i].closest('.inputContainer');
            var check = row ? row.querySelector('.watchlist-autorequest') : null;
            result.push({ Input: val, AutoRequest: !!(check && check.checked) });
        }
        return result;
    }

    view.addEventListener('viewshow', function (e) {

        var container = view.querySelector('#watchlistContainer');

        var url = ApiClient.getUrl('Jellyfin.Plugin.LetterboxdSync/UserConfig');
        ApiClient.ajax({ type: 'GET', url: url, dataType: 'json' }).then(function (account) {
            view.querySelector('#lbxd-account').value = account.userLetterboxd || '';
            view.querySelector('#lbxd-key').value = '';
            view.querySelector('#enable').checked = account.enable || false;
            view.querySelector('#sendfavorite').checked = account.sendFavorite || false;
            view.querySelector('#sendrating').checked = account.sendRating;
            view.querySelector('#realtimesync').checked = account.enableRealtimeSync;
            view.querySelector('#importdiary').checked = account.importDiary || false;
            view.querySelector('#enabledatefilter').checked = account.enableDateFilter || false;
            view.querySelector('#datefilterdays').value = account.dateFilterDays || 7;

            var status = view.querySelector('#lbxd-link-status');
            if (status) {
                status.textContent = account.isLinked
                    ? 'Account linked. Leave the password blank to keep it, or re-enter it to re-link.'
                    : 'Not linked. Enter your Letterboxd credentials. Your password is exchanged for a token on save and is never stored.';
            }

            container.innerHTML = '';
            var entries = account.watchlists || [];
            for (var i = 0; i < entries.length; i++) {
                addWatchlistEntry(container, entries[i].input, entries[i].autoRequest);
            }

            var hint = view.querySelector('#seerr-hint');
            if (hint) {
                hint.textContent = account.seerrConfigured
                    ? ''
                    : 'Auto-request has no effect until an administrator configures Seerr in the plugin settings.';
            }
        });
    });

    view.querySelector('#addWatchlistBtn').addEventListener('click', function () {
        var container = view.querySelector('#watchlistContainer');
        addWatchlistEntry(container, '');
    });

    view.querySelector('#LetterboxdSyncUserConfigForm').addEventListener('submit', function (e) {

        e.preventDefault();

        Dashboard.showLoadingMsg();

        var configUser = {};
        configUser.UserLetterboxd = view.querySelector('#lbxd-account').value;
        configUser.PasswordLetterboxd = view.querySelector('#lbxd-key').value;
        configUser.Enable = view.querySelector('#enable').checked;
        configUser.SendFavorite = view.querySelector('#sendfavorite').checked;
        configUser.SendRating = view.querySelector('#sendrating').checked;
        configUser.EnableRealtimeSync = view.querySelector('#realtimesync').checked;
        configUser.ImportDiary = view.querySelector('#importdiary').checked;
        configUser.EnableDateFilter = view.querySelector('#enabledatefilter').checked;
        configUser.DateFilterDays = parseInt(view.querySelector('#datefilterdays').value) || 7;

        configUser.Watchlists = getWatchlists();

        var data = JSON.stringify(configUser);

        // SaveUserConfig performs the OAuth exchange server-side (password -> token) and returns an
        // error if the credentials are invalid, so a separate auth step is no longer needed.
        var saveUrl = ApiClient.getUrl('Jellyfin.Plugin.LetterboxdSync/UserConfig');
        ApiClient.ajax({ type: 'POST', url: saveUrl, data: data, contentType: 'application/json' }).then(function () {
            Dashboard.hideLoadingMsg();
            Dashboard.alert('Settings saved.');
        }).catch(function (response) {
            Dashboard.hideLoadingMsg();
            if (response && response.json) {
                response.json().then(function (res) {
                    Dashboard.processErrorResponse({ statusText: response.statusText + ' - ' + (res.Message || '') });
                }).catch(function () {
                    Dashboard.alert('Error saving settings.');
                });
            } else {
                Dashboard.alert('Error saving settings.');
            }
        });
    });
}
