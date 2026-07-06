(function () {
    'use strict';

    var MENU_ID = 'letterboxdSyncMenuSection';
    var DIALOG_ID = 'letterboxdSyncDialog';

    // ===== i18n =====
    var STRINGS = {
        en: {
            login: 'Letterboxd Login',
            username: 'Letterboxd Username',
            password: 'Letterboxd Password',
            options: 'Options',
            enableSync: 'Enable sync',
            enableSyncDesc: 'Synchronizes the films watched by this user',
            sendFavorites: 'Send Favorites',
            dateFiltering: 'Date Filtering',
            enableDateFilter: 'Enable Date Filtering',
            enableDateFilterDesc: 'Only sync movies played within the specified number of days',
            daysToLookBack: 'Days to look back',
            watchlistSync: 'Watchlist Sync',
            watchlistDesc: 'Paste a watchlist link (letterboxd.com or boxd.it short link) or a Letterboxd profile name to sync their public watchlist as a playlist.',
            watchlistPlaceholder: 'watchlist link or Letterboxd profile',
            addWatchlist: 'Add Watchlist',
            remove: 'Remove',
            save: 'Save',
            saving: 'Saving...',
            settingsSaved: 'Settings saved.',
            errorSaving: 'Error saving settings.',
            authFailed: 'Authentication failed.'
        },
        fr: {
            login: 'Connexion Letterboxd',
            username: 'Nom d\'utilisateur Letterboxd',
            password: 'Mot de passe Letterboxd',
            options: 'Options',
            enableSync: 'Activer la synchronisation',
            enableSyncDesc: 'Synchronise les films vus par cet utilisateur',
            sendFavorites: 'Envoyer les favoris',
            dateFiltering: 'Filtrage par date',
            enableDateFilter: 'Activer le filtrage par date',
            enableDateFilterDesc: 'Ne synchroniser que les films vus dans le nombre de jours sp\u00e9cifi\u00e9',
            daysToLookBack: 'Nombre de jours',
            watchlistSync: 'Synchronisation des watchlists',
            watchlistDesc: 'Collez un lien de watchlist (letterboxd.com ou lien court boxd.it) ou un nom d\'utilisateur Letterboxd pour synchroniser sa watchlist publique en playlist.',
            watchlistPlaceholder: 'lien de watchlist ou nom d\'utilisateur',
            addWatchlist: 'Ajouter une watchlist',
            remove: 'Supprimer',
            save: 'Enregistrer',
            saving: 'Enregistrement...',
            settingsSaved: 'Param\u00e8tres enregistr\u00e9s.',
            errorSaving: 'Erreur lors de l\'enregistrement.',
            authFailed: '\u00c9chec de l\'authentification.'
        },
        de: {
            login: 'Letterboxd-Anmeldung',
            username: 'Letterboxd-Benutzername',
            password: 'Letterboxd-Passwort',
            options: 'Optionen',
            enableSync: 'Synchronisierung aktivieren',
            enableSyncDesc: 'Synchronisiert die von diesem Benutzer gesehenen Filme',
            sendFavorites: 'Favoriten senden',
            dateFiltering: 'Datumsfilter',
            enableDateFilter: 'Datumsfilter aktivieren',
            enableDateFilterDesc: 'Nur Filme synchronisieren, die innerhalb der angegebenen Tage gesehen wurden',
            daysToLookBack: 'Anzahl der Tage',
            watchlistSync: 'Watchlist-Synchronisierung',
            watchlistDesc: 'F\u00fcgen Sie einen Watchlist-Link (letterboxd.com oder boxd.it-Kurzlink) oder einen Letterboxd-Benutzernamen ein, um die \u00f6ffentliche Watchlist als Playlist zu synchronisieren.',
            watchlistPlaceholder: 'Watchlist-Link oder Benutzername',
            addWatchlist: 'Watchlist hinzuf\u00fcgen',
            remove: 'Entfernen',
            save: 'Speichern',
            saving: 'Speichern...',
            settingsSaved: 'Einstellungen gespeichert.',
            errorSaving: 'Fehler beim Speichern.',
            authFailed: 'Authentifizierung fehlgeschlagen.'
        },
        es: {
            login: 'Inicio de sesi\u00f3n Letterboxd',
            username: 'Usuario Letterboxd',
            password: 'Contrase\u00f1a Letterboxd',
            options: 'Opciones',
            enableSync: 'Activar sincronizaci\u00f3n',
            enableSyncDesc: 'Sincroniza las pel\u00edculas vistas por este usuario',
            sendFavorites: 'Enviar favoritos',
            dateFiltering: 'Filtrado por fecha',
            enableDateFilter: 'Activar filtrado por fecha',
            enableDateFilterDesc: 'Solo sincronizar pel\u00edculas vistas en el n\u00famero de d\u00edas especificado',
            daysToLookBack: 'D\u00edas a retroceder',
            watchlistSync: 'Sincronizaci\u00f3n de watchlists',
            watchlistDesc: 'Pegue un enlace de watchlist (letterboxd.com o enlace corto boxd.it) o un nombre de usuario de Letterboxd para sincronizar su watchlist p\u00fablica como lista de reproducci\u00f3n.',
            watchlistPlaceholder: 'enlace de watchlist o nombre de usuario',
            addWatchlist: 'A\u00f1adir watchlist',
            remove: 'Eliminar',
            save: 'Guardar',
            saving: 'Guardando...',
            settingsSaved: 'Ajustes guardados.',
            errorSaving: 'Error al guardar.',
            authFailed: 'Error de autenticaci\u00f3n.'
        },
        it: {
            login: 'Accesso Letterboxd',
            username: 'Nome utente Letterboxd',
            password: 'Password Letterboxd',
            options: 'Opzioni',
            enableSync: 'Attiva sincronizzazione',
            enableSyncDesc: 'Sincronizza i film visti da questo utente',
            sendFavorites: 'Invia preferiti',
            dateFiltering: 'Filtro per data',
            enableDateFilter: 'Attiva filtro per data',
            enableDateFilterDesc: 'Sincronizza solo i film visti nel numero di giorni specificato',
            daysToLookBack: 'Giorni da considerare',
            watchlistSync: 'Sincronizzazione watchlist',
            watchlistDesc: 'Aggiungi nomi utente Letterboxd le cui watchlist pubbliche vuoi sincronizzare come playlist.',
            addWatchlist: 'Aggiungi watchlist',
            remove: 'Rimuovi',
            save: 'Salva',
            saving: 'Salvataggio...',
            settingsSaved: 'Impostazioni salvate.',
            errorSaving: 'Errore durante il salvataggio.',
            authFailed: 'Autenticazione fallita.'
        },
        pt: {
            login: 'Login Letterboxd',
            username: 'Utilizador Letterboxd',
            password: 'Palavra-passe Letterboxd',
            options: 'Op\u00e7\u00f5es',
            enableSync: 'Ativar sincroniza\u00e7\u00e3o',
            enableSyncDesc: 'Sincroniza os filmes vistos por este utilizador',
            sendFavorites: 'Enviar favoritos',
            dateFiltering: 'Filtro por data',
            enableDateFilter: 'Ativar filtro por data',
            enableDateFilterDesc: 'Sincronizar apenas filmes vistos no n\u00famero de dias especificado',
            daysToLookBack: 'Dias a retroceder',
            watchlistSync: 'Sincroniza\u00e7\u00e3o de watchlists',
            watchlistDesc: 'Adicione utilizadores Letterboxd cujas watchlists p\u00fablicas pretende sincronizar como playlists.',
            addWatchlist: 'Adicionar watchlist',
            remove: 'Remover',
            save: 'Guardar',
            saving: 'A guardar...',
            settingsSaved: 'Defini\u00e7\u00f5es guardadas.',
            errorSaving: 'Erro ao guardar.',
            authFailed: 'Falha na autentica\u00e7\u00e3o.'
        },
        nl: {
            login: 'Letterboxd-inloggen',
            username: 'Letterboxd-gebruikersnaam',
            password: 'Letterboxd-wachtwoord',
            options: 'Opties',
            enableSync: 'Synchronisatie inschakelen',
            enableSyncDesc: 'Synchroniseert de films bekeken door deze gebruiker',
            sendFavorites: 'Favorieten verzenden',
            dateFiltering: 'Datumfilter',
            enableDateFilter: 'Datumfilter inschakelen',
            enableDateFilterDesc: 'Alleen films synchroniseren die binnen het opgegeven aantal dagen zijn bekeken',
            daysToLookBack: 'Aantal dagen',
            watchlistSync: 'Watchlist-synchronisatie',
            watchlistDesc: 'Voeg Letterboxd-gebruikersnamen toe waarvan u de openbare watchlists als afspeellijsten wilt synchroniseren.',
            addWatchlist: 'Watchlist toevoegen',
            remove: 'Verwijderen',
            save: 'Opslaan',
            saving: 'Opslaan...',
            settingsSaved: 'Instellingen opgeslagen.',
            errorSaving: 'Fout bij opslaan.',
            authFailed: 'Authenticatie mislukt.'
        },
        pl: {
            login: 'Logowanie Letterboxd',
            username: 'Nazwa u\u017cytkownika Letterboxd',
            password: 'Has\u0142o Letterboxd',
            options: 'Opcje',
            enableSync: 'W\u0142\u0105cz synchronizacj\u0119',
            enableSyncDesc: 'Synchronizuje filmy obejrzane przez tego u\u017cytkownika',
            sendFavorites: 'Wysy\u0142aj ulubione',
            dateFiltering: 'Filtrowanie wg daty',
            enableDateFilter: 'W\u0142\u0105cz filtrowanie wg daty',
            enableDateFilterDesc: 'Synchronizuj tylko filmy obejrzane w podanej liczbie dni',
            daysToLookBack: 'Liczba dni',
            watchlistSync: 'Synchronizacja watchlist',
            watchlistDesc: 'Dodaj nazwy u\u017cytkownik\u00f3w Letterboxd, kt\u00f3rych publiczne watchlisty chcesz synchronizowa\u0107 jako playlisty.',
            addWatchlist: 'Dodaj watchlist\u0119',
            remove: 'Usu\u0144',
            save: 'Zapisz',
            saving: 'Zapisywanie...',
            settingsSaved: 'Ustawienia zapisane.',
            errorSaving: 'B\u0142\u0105d podczas zapisywania.',
            authFailed: 'Uwierzytelnianie nie powiod\u0142o si\u0119.'
        },
        cs: {
            login: 'P\u0159ihl\u00e1\u0161en\u00ed Letterboxd',
            username: 'U\u017eivatelsk\u00e9 jm\u00e9no Letterboxd',
            password: 'Heslo Letterboxd',
            options: 'Mo\u017enosti',
            enableSync: 'Povolit synchronizaci',
            enableSyncDesc: 'Synchronizuje filmy zhl\u00e9dnut\u00e9 t\u00edmto u\u017eivatelem',
            sendFavorites: 'Odes\u00edlat obl\u00edben\u00e9',
            dateFiltering: 'Filtrov\u00e1n\u00ed dle data',
            enableDateFilter: 'Povolit filtrov\u00e1n\u00ed dle data',
            enableDateFilterDesc: 'Synchronizovat pouze filmy zhl\u00e9dnut\u00e9 v uveden\u00e9m po\u010dtu dn\u00ed',
            daysToLookBack: 'Po\u010det dn\u00ed',
            watchlistSync: 'Synchronizace watchlist\u016f',
            watchlistDesc: 'P\u0159idejte u\u017eivatelsk\u00e1 jm\u00e9na Letterboxd, jejich\u017e ve\u0159ejn\u00e9 watchlisty chcete synchronizovat jako playlisty.',
            addWatchlist: 'P\u0159idat watchlist',
            remove: 'Odebrat',
            save: 'Ulo\u017eit',
            saving: 'Ukl\u00e1d\u00e1n\u00ed...',
            settingsSaved: 'Nastaven\u00ed ulo\u017eeno.',
            errorSaving: 'Chyba p\u0159i ukl\u00e1d\u00e1n\u00ed.',
            authFailed: 'Ov\u011b\u0159en\u00ed selhalo.'
        },
        ro: {
            login: 'Autentificare Letterboxd', username: 'Utilizator Letterboxd', password: 'Parol\u0103 Letterboxd',
            options: 'Op\u021biuni', enableSync: 'Activeaz\u0103 sincronizarea', enableSyncDesc: 'Sincronizeaz\u0103 filmele vizionate de acest utilizator',
            sendFavorites: 'Trimite favorite', dateFiltering: 'Filtrare dup\u0103 dat\u0103', enableDateFilter: 'Activeaz\u0103 filtrarea dup\u0103 dat\u0103',
            enableDateFilterDesc: 'Sincronizeaz\u0103 doar filmele vizionate \u00een num\u0103rul de zile specificat', daysToLookBack: 'Num\u0103r de zile',
            watchlistSync: 'Sincronizare watchlist', watchlistDesc: 'Ad\u0103uga\u021bi utilizatori Letterboxd ale c\u0103ror watchlist-uri publice dori\u021bi s\u0103 le sincroniza\u021bi ca playlisturi.',
            addWatchlist: 'Adaug\u0103 watchlist', remove: '\u0218terge', save: 'Salveaz\u0103', saving: 'Se salveaz\u0103...',
            settingsSaved: 'Set\u0103ri salvate.', errorSaving: 'Eroare la salvare.', authFailed: 'Autentificare e\u0219uat\u0103.'
        },
        hu: {
            login: 'Letterboxd bejelentkez\u00e9s', username: 'Letterboxd felhaszn\u00e1l\u00f3n\u00e9v', password: 'Letterboxd jelsz\u00f3',
            options: 'Be\u00e1ll\u00edt\u00e1sok', enableSync: 'Szinkroniz\u00e1l\u00e1s enged\u00e9lyez\u00e9se', enableSyncDesc: 'Szinkroniz\u00e1lja a felhaszn\u00e1l\u00f3 \u00e1ltal megtekintett filmeket',
            sendFavorites: 'Kedvencek k\u00fcld\u00e9se', dateFiltering: 'D\u00e1tumsz\u0171r\u00e9s', enableDateFilter: 'D\u00e1tumsz\u0171r\u00e9s enged\u00e9lyez\u00e9se',
            enableDateFilterDesc: 'Csak a megadott napon bel\u00fcl megtekintett filmek szinkroniz\u00e1l\u00e1sa', daysToLookBack: 'Napok sz\u00e1ma',
            watchlistSync: 'Watchlist szinkroniz\u00e1l\u00e1s', watchlistDesc: 'Adjon hozz\u00e1 Letterboxd felhaszn\u00e1l\u00f3neveket, akiknek a nyilv\u00e1nos watchlistjeit lej\u00e1tsz\u00e1si listakk\u00e9nt szinkroniz\u00e1lni szeretn\u00e9.',
            addWatchlist: 'Watchlist hozz\u00e1ad\u00e1sa', remove: 'Elt\u00e1vol\u00edt\u00e1s', save: 'Ment\u00e9s', saving: 'Ment\u00e9s...',
            settingsSaved: 'Be\u00e1ll\u00edt\u00e1sok mentve.', errorSaving: 'Hiba a ment\u00e9s sor\u00e1n.', authFailed: 'Hiteles\u00edt\u00e9s sikertelen.'
        },
        sv: {
            login: 'Letterboxd-inloggning', username: 'Letterboxd-anv\u00e4ndarnamn', password: 'Letterboxd-l\u00f6senord',
            options: 'Alternativ', enableSync: 'Aktivera synkronisering', enableSyncDesc: 'Synkroniserar filmer sedda av denna anv\u00e4ndare',
            sendFavorites: 'Skicka favoriter', dateFiltering: 'Datumfiltrering', enableDateFilter: 'Aktivera datumfiltrering',
            enableDateFilterDesc: 'Synkronisera bara filmer sedda inom angivet antal dagar', daysToLookBack: 'Antal dagar',
            watchlistSync: 'Watchlist-synkronisering', watchlistDesc: 'L\u00e4gg till Letterboxd-anv\u00e4ndarnamn vars offentliga watchlists du vill synkronisera som spellistor.',
            addWatchlist: 'L\u00e4gg till watchlist', remove: 'Ta bort', save: 'Spara', saving: 'Sparar...',
            settingsSaved: 'Inst\u00e4llningar sparade.', errorSaving: 'Fel vid sparande.', authFailed: 'Autentisering misslyckades.'
        },
        da: {
            login: 'Letterboxd-login', username: 'Letterboxd-brugernavn', password: 'Letterboxd-adgangskode',
            options: 'Indstillinger', enableSync: 'Aktiv\u00e9r synkronisering', enableSyncDesc: 'Synkroniserer film set af denne bruger',
            sendFavorites: 'Send favoritter', dateFiltering: 'Datofiltrering', enableDateFilter: 'Aktiv\u00e9r datofiltrering',
            enableDateFilterDesc: 'Synkroniser kun film set inden for det angivne antal dage', daysToLookBack: 'Antal dage',
            watchlistSync: 'Watchlist-synkronisering', watchlistDesc: 'Tilf\u00f8j Letterboxd-brugernavne, hvis offentlige watchlists du vil synkronisere som afspilningslister.',
            addWatchlist: 'Tilf\u00f8j watchlist', remove: 'Fjern', save: 'Gem', saving: 'Gemmer...',
            settingsSaved: 'Indstillinger gemt.', errorSaving: 'Fejl ved lagring.', authFailed: 'Godkendelse mislykkedes.'
        },
        no: {
            login: 'Letterboxd-innlogging', username: 'Letterboxd-brukernavn', password: 'Letterboxd-passord',
            options: 'Alternativer', enableSync: 'Aktiver synkronisering', enableSyncDesc: 'Synkroniserer filmer sett av denne brukeren',
            sendFavorites: 'Send favoritter', dateFiltering: 'Datofiltrering', enableDateFilter: 'Aktiver datofiltrering',
            enableDateFilterDesc: 'Synkroniser kun filmer sett innen angitt antall dager', daysToLookBack: 'Antall dager',
            watchlistSync: 'Watchlist-synkronisering', watchlistDesc: 'Legg til Letterboxd-brukernavn hvis offentlige watchlister du vil synkronisere som spillelister.',
            addWatchlist: 'Legg til watchlist', remove: 'Fjern', save: 'Lagre', saving: 'Lagrer...',
            settingsSaved: 'Innstillinger lagret.', errorSaving: 'Feil ved lagring.', authFailed: 'Autentisering mislyktes.'
        },
        fi: {
            login: 'Letterboxd-kirjautuminen', username: 'Letterboxd-k\u00e4ytt\u00e4j\u00e4nimi', password: 'Letterboxd-salasana',
            options: 'Asetukset', enableSync: 'Ota synkronointi k\u00e4ytt\u00f6\u00f6n', enableSyncDesc: 'Synkronoi t\u00e4m\u00e4n k\u00e4ytt\u00e4j\u00e4n katselemat elokuvat',
            sendFavorites: 'L\u00e4het\u00e4 suosikit', dateFiltering: 'P\u00e4iv\u00e4m\u00e4\u00e4r\u00e4suodatus', enableDateFilter: 'Ota p\u00e4iv\u00e4m\u00e4\u00e4r\u00e4suodatus k\u00e4ytt\u00f6\u00f6n',
            enableDateFilterDesc: 'Synkronoi vain m\u00e4\u00e4ritetyn p\u00e4iv\u00e4m\u00e4\u00e4r\u00e4n sis\u00e4ll\u00e4 katsotut elokuvat', daysToLookBack: 'P\u00e4ivien m\u00e4\u00e4r\u00e4',
            watchlistSync: 'Watchlist-synkronointi', watchlistDesc: 'Lis\u00e4\u00e4 Letterboxd-k\u00e4ytt\u00e4j\u00e4nimi\u00e4, joiden julkiset watchlistit haluat synkronoida soittolistoiksi.',
            addWatchlist: 'Lis\u00e4\u00e4 watchlist', remove: 'Poista', save: 'Tallenna', saving: 'Tallennetaan...',
            settingsSaved: 'Asetukset tallennettu.', errorSaving: 'Virhe tallennuksessa.', authFailed: 'Todennus ep\u00e4onnistui.'
        },
        el: {
            login: '\u03a3\u03cd\u03bd\u03b4\u03b5\u03c3\u03b7 Letterboxd', username: '\u038c\u03bd\u03bf\u03bc\u03b1 \u03c7\u03c1\u03ae\u03c3\u03c4\u03b7 Letterboxd', password: '\u039a\u03c9\u03b4\u03b9\u03ba\u03cc\u03c2 Letterboxd',
            options: '\u0395\u03c0\u03b9\u03bb\u03bf\u03b3\u03ad\u03c2', enableSync: '\u0395\u03bd\u03b5\u03c1\u03b3\u03bf\u03c0\u03bf\u03af\u03b7\u03c3\u03b7 \u03c3\u03c5\u03b3\u03c7\u03c1\u03bf\u03bd\u03b9\u03c3\u03bc\u03bf\u03cd', enableSyncDesc: '\u03a3\u03c5\u03b3\u03c7\u03c1\u03bf\u03bd\u03af\u03b6\u03b5\u03b9 \u03c4\u03b9\u03c2 \u03c4\u03b1\u03b9\u03bd\u03af\u03b5\u03c2 \u03c0\u03bf\u03c5 \u03b5\u03af\u03b4\u03b5 \u03b1\u03c5\u03c4\u03cc\u03c2 \u03bf \u03c7\u03c1\u03ae\u03c3\u03c4\u03b7\u03c2',
            sendFavorites: '\u0391\u03c0\u03bf\u03c3\u03c4\u03bf\u03bb\u03ae \u03b1\u03b3\u03b1\u03c0\u03b7\u03bc\u03ad\u03bd\u03c9\u03bd', dateFiltering: '\u03a6\u03b9\u03bb\u03c4\u03c1\u03ac\u03c1\u03b9\u03c3\u03bc\u03b1 \u03b7\u03bc\u03b5\u03c1\u03bf\u03bc\u03b7\u03bd\u03af\u03b1\u03c2', enableDateFilter: '\u0395\u03bd\u03b5\u03c1\u03b3\u03bf\u03c0\u03bf\u03af\u03b7\u03c3\u03b7 \u03c6\u03b9\u03bb\u03c4\u03c1\u03b1\u03c1\u03af\u03c3\u03bc\u03b1\u03c4\u03bf\u03c2',
            enableDateFilterDesc: '\u03a3\u03c5\u03b3\u03c7\u03c1\u03bf\u03bd\u03b9\u03c3\u03bc\u03cc\u03c2 \u03bc\u03cc\u03bd\u03bf \u03c4\u03b1\u03b9\u03bd\u03b9\u03ce\u03bd \u03c0\u03bf\u03c5 \u03c0\u03c1\u03bf\u03b2\u03bb\u03ae\u03b8\u03b7\u03ba\u03b1\u03bd \u03b5\u03bd\u03c4\u03cc\u03c2 \u03c4\u03c9\u03bd \u03ba\u03b1\u03b8\u03bf\u03c1\u03b9\u03c3\u03bc\u03ad\u03bd\u03c9\u03bd \u03b7\u03bc\u03b5\u03c1\u03ce\u03bd', daysToLookBack: '\u0391\u03c1\u03b9\u03b8\u03bc\u03cc\u03c2 \u03b7\u03bc\u03b5\u03c1\u03ce\u03bd',
            watchlistSync: '\u03a3\u03c5\u03b3\u03c7\u03c1\u03bf\u03bd\u03b9\u03c3\u03bc\u03cc\u03c2 watchlist', watchlistDesc: '\u03a0\u03c1\u03bf\u03c3\u03b8\u03ad\u03c3\u03c4\u03b5 \u03c7\u03c1\u03ae\u03c3\u03c4\u03b5\u03c2 Letterboxd \u03c4\u03c9\u03bd \u03bf\u03c0\u03bf\u03af\u03c9\u03bd \u03c4\u03b9\u03c2 \u03b4\u03b7\u03bc\u03cc\u03c3\u03b9\u03b5\u03c2 watchlists \u03b8\u03ad\u03bb\u03b5\u03c4\u03b5 \u03bd\u03b1 \u03c3\u03c5\u03b3\u03c7\u03c1\u03bf\u03bd\u03af\u03c3\u03b5\u03c4\u03b5.',
            addWatchlist: '\u03a0\u03c1\u03bf\u03c3\u03b8\u03ae\u03ba\u03b7 watchlist', remove: '\u0391\u03c6\u03b1\u03af\u03c1\u03b5\u03c3\u03b7', save: '\u0391\u03c0\u03bf\u03b8\u03ae\u03ba\u03b5\u03c5\u03c3\u03b7', saving: '\u0391\u03c0\u03bf\u03b8\u03ae\u03ba\u03b5\u03c5\u03c3\u03b7...',
            settingsSaved: '\u03a1\u03c5\u03b8\u03bc\u03af\u03c3\u03b5\u03b9\u03c2 \u03b1\u03c0\u03bf\u03b8\u03b7\u03ba\u03b5\u03cd\u03c4\u03b7\u03ba\u03b1\u03bd.', errorSaving: '\u03a3\u03c6\u03ac\u03bb\u03bc\u03b1 \u03b1\u03c0\u03bf\u03b8\u03ae\u03ba\u03b5\u03c5\u03c3\u03b7\u03c2.', authFailed: '\u0391\u03c0\u03bf\u03c4\u03c5\u03c7\u03af\u03b1 \u03b5\u03bb\u03ad\u03b3\u03c7\u03bf\u03c5.'
        },
        ru: {
            login: '\u0412\u0445\u043e\u0434 Letterboxd', username: '\u0418\u043c\u044f \u043f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u0442\u0435\u043b\u044f Letterboxd', password: '\u041f\u0430\u0440\u043e\u043b\u044c Letterboxd',
            options: '\u041d\u0430\u0441\u0442\u0440\u043e\u0439\u043a\u0438', enableSync: '\u0412\u043a\u043b\u044e\u0447\u0438\u0442\u044c \u0441\u0438\u043d\u0445\u0440\u043e\u043d\u0438\u0437\u0430\u0446\u0438\u044e', enableSyncDesc: '\u0421\u0438\u043d\u0445\u0440\u043e\u043d\u0438\u0437\u0438\u0440\u0443\u0435\u0442 \u043f\u0440\u043e\u0441\u043c\u043e\u0442\u0440\u0435\u043d\u043d\u044b\u0435 \u044d\u0442\u0438\u043c \u043f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u0442\u0435\u043b\u0435\u043c \u0444\u0438\u043b\u044c\u043c\u044b',
            sendFavorites: '\u041e\u0442\u043f\u0440\u0430\u0432\u043b\u044f\u0442\u044c \u0438\u0437\u0431\u0440\u0430\u043d\u043d\u043e\u0435', dateFiltering: '\u0424\u0438\u043b\u044c\u0442\u0440 \u043f\u043e \u0434\u0430\u0442\u0435', enableDateFilter: '\u0412\u043a\u043b\u044e\u0447\u0438\u0442\u044c \u0444\u0438\u043b\u044c\u0442\u0440 \u043f\u043e \u0434\u0430\u0442\u0435',
            enableDateFilterDesc: '\u0421\u0438\u043d\u0445\u0440\u043e\u043d\u0438\u0437\u0438\u0440\u043e\u0432\u0430\u0442\u044c \u0442\u043e\u043b\u044c\u043a\u043e \u0444\u0438\u043b\u044c\u043c\u044b \u0437\u0430 \u0443\u043a\u0430\u0437\u0430\u043d\u043d\u043e\u0435 \u043a\u043e\u043b\u0438\u0447\u0435\u0441\u0442\u0432\u043e \u0434\u043d\u0435\u0439', daysToLookBack: '\u041a\u043e\u043b\u0438\u0447\u0435\u0441\u0442\u0432\u043e \u0434\u043d\u0435\u0439',
            watchlistSync: '\u0421\u0438\u043d\u0445\u0440\u043e\u043d\u0438\u0437\u0430\u0446\u0438\u044f \u0441\u043f\u0438\u0441\u043a\u043e\u0432', watchlistDesc: '\u0414\u043e\u0431\u0430\u0432\u044c\u0442\u0435 \u043f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u0442\u0435\u043b\u0435\u0439 Letterboxd, \u0447\u044c\u0438 \u043f\u0443\u0431\u043b\u0438\u0447\u043d\u044b\u0435 \u0441\u043f\u0438\u0441\u043a\u0438 \u0432\u044b \u0445\u043e\u0442\u0438\u0442\u0435 \u0441\u0438\u043d\u0445\u0440\u043e\u043d\u0438\u0437\u0438\u0440\u043e\u0432\u0430\u0442\u044c.',
            addWatchlist: '\u0414\u043e\u0431\u0430\u0432\u0438\u0442\u044c \u0441\u043f\u0438\u0441\u043e\u043a', remove: '\u0423\u0434\u0430\u043b\u0438\u0442\u044c', save: '\u0421\u043e\u0445\u0440\u0430\u043d\u0438\u0442\u044c', saving: '\u0421\u043e\u0445\u0440\u0430\u043d\u0435\u043d\u0438\u0435...',
            settingsSaved: '\u041d\u0430\u0441\u0442\u0440\u043e\u0439\u043a\u0438 \u0441\u043e\u0445\u0440\u0430\u043d\u0435\u043d\u044b.', errorSaving: '\u041e\u0448\u0438\u0431\u043a\u0430 \u0441\u043e\u0445\u0440\u0430\u043d\u0435\u043d\u0438\u044f.', authFailed: '\u041e\u0448\u0438\u0431\u043a\u0430 \u0430\u0443\u0442\u0435\u043d\u0442\u0438\u0444\u0438\u043a\u0430\u0446\u0438\u0438.'
        },
        hi: {
            login: 'Letterboxd \u0932\u0949\u0917\u0907\u0928', username: 'Letterboxd \u0909\u092a\u092f\u094b\u0917\u0915\u0930\u094d\u0924\u093e \u0928\u093e\u092e', password: 'Letterboxd \u092a\u093e\u0938\u0935\u0930\u094d\u0921',
            options: '\u0935\u093f\u0915\u0932\u094d\u092a', enableSync: '\u0938\u093f\u0902\u0915 \u0938\u0915\u094d\u0937\u092e \u0915\u0930\u0947\u0902', enableSyncDesc: '\u0907\u0938 \u0909\u092a\u092f\u094b\u0917\u0915\u0930\u094d\u0924\u093e \u0926\u094d\u0935\u093e\u0930\u093e \u0926\u0947\u0916\u0940 \u0917\u0908 \u0938\u092d\u0940 \u092b\u093f\u0932\u094d\u092e\u094b\u0902 \u0915\u094b \u0938\u093f\u0902\u0915 \u0915\u0930\u0924\u093e \u0939\u0948',
            sendFavorites: '\u092a\u0938\u0902\u0926\u0940\u0926\u093e \u092d\u0947\u091c\u0947\u0902', dateFiltering: '\u0924\u093f\u0925\u093f \u092b\u093c\u093f\u0932\u094d\u091f\u0930', enableDateFilter: '\u0924\u093f\u0925\u093f \u092b\u093c\u093f\u0932\u094d\u091f\u0930 \u0938\u0915\u094d\u0937\u092e \u0915\u0930\u0947\u0902',
            enableDateFilterDesc: '\u0915\u0947\u0935\u0932 \u0928\u093f\u0930\u094d\u0927\u093e\u0930\u093f\u0924 \u0926\u093f\u0928\u094b\u0902 \u092e\u0947\u0902 \u0926\u0947\u0916\u0940 \u0917\u0908 \u092b\u093f\u0932\u094d\u092e\u0947\u0902 \u0938\u093f\u0902\u0915 \u0915\u0930\u0947\u0902', daysToLookBack: '\u0926\u093f\u0928\u094b\u0902 \u0915\u0940 \u0938\u0902\u0916\u094d\u092f\u093e',
            watchlistSync: '\u0935\u0949\u091a\u0932\u093f\u0938\u094d\u091f \u0938\u093f\u0902\u0915', watchlistDesc: 'Letterboxd \u0909\u092a\u092f\u094b\u0917\u0915\u0930\u094d\u0924\u093e \u0928\u093e\u092e \u091c\u094b\u0921\u093c\u0947\u0902 \u091c\u093f\u0928\u0915\u0940 \u0938\u093e\u0930\u094d\u0935\u091c\u0928\u093f\u0915 \u0935\u0949\u091a\u0932\u093f\u0938\u094d\u091f \u0906\u092a \u092a\u094d\u0932\u0947\u0932\u093f\u0938\u094d\u091f \u0915\u0947 \u0930\u0942\u092a \u092e\u0947\u0902 \u0938\u093f\u0902\u0915 \u0915\u0930\u0928\u093e \u091a\u093e\u0939\u0924\u0947 \u0939\u0948\u0902\u0964',
            addWatchlist: '\u0935\u0949\u091a\u0932\u093f\u0938\u094d\u091f \u091c\u094b\u0921\u093c\u0947\u0902', remove: '\u0939\u091f\u093e\u090f\u0902', save: '\u0938\u0939\u0947\u091c\u0947\u0902', saving: '\u0938\u0939\u0947\u091c\u093e \u091c\u093e \u0930\u0939\u093e \u0939\u0948...',
            settingsSaved: '\u0938\u0947\u091f\u093f\u0902\u0917\u094d\u0938 \u0938\u0939\u0947\u091c\u0940 \u0917\u0908\u0902\u0964', errorSaving: '\u0938\u0939\u0947\u091c\u0928\u0947 \u092e\u0947\u0902 \u0924\u094d\u0930\u0941\u091f\u093f\u0964', authFailed: '\u092a\u094d\u0930\u092e\u093e\u0923\u0940\u0915\u0930\u0923 \u0935\u093f\u092b\u0932\u0964'
        },
        zh: {
            login: 'Letterboxd \u767b\u5f55', username: 'Letterboxd \u7528\u6237\u540d', password: 'Letterboxd \u5bc6\u7801',
            options: '\u9009\u9879', enableSync: '\u542f\u7528\u540c\u6b65', enableSyncDesc: '\u540c\u6b65\u6b64\u7528\u6237\u89c2\u770b\u8fc7\u7684\u7535\u5f71',
            sendFavorites: '\u53d1\u9001\u6536\u85cf', dateFiltering: '\u65e5\u671f\u8fc7\u6ee4', enableDateFilter: '\u542f\u7528\u65e5\u671f\u8fc7\u6ee4',
            enableDateFilterDesc: '\u4ec5\u540c\u6b65\u6307\u5b9a\u5929\u6570\u5185\u89c2\u770b\u7684\u7535\u5f71', daysToLookBack: '\u56de\u770b\u5929\u6570',
            watchlistSync: '\u7247\u5355\u540c\u6b65', watchlistDesc: '\u6dfb\u52a0 Letterboxd \u7528\u6237\u540d\uff0c\u5c06\u5176\u516c\u5f00\u7247\u5355\u540c\u6b65\u4e3a\u64ad\u653e\u5217\u8868\u3002',
            addWatchlist: '\u6dfb\u52a0\u7247\u5355', remove: '\u5220\u9664', save: '\u4fdd\u5b58', saving: '\u4fdd\u5b58\u4e2d...',
            settingsSaved: '\u8bbe\u7f6e\u5df2\u4fdd\u5b58\u3002', errorSaving: '\u4fdd\u5b58\u5931\u8d25\u3002', authFailed: '\u8ba4\u8bc1\u5931\u8d25\u3002'
        },
        ja: {
            login: 'Letterboxd \u30ed\u30b0\u30a4\u30f3', username: 'Letterboxd \u30e6\u30fc\u30b6\u30fc\u540d', password: 'Letterboxd \u30d1\u30b9\u30ef\u30fc\u30c9',
            options: '\u30aa\u30d7\u30b7\u30e7\u30f3', enableSync: '\u540c\u671f\u3092\u6709\u52b9\u306b\u3059\u308b', enableSyncDesc: '\u3053\u306e\u30e6\u30fc\u30b6\u30fc\u304c\u8996\u8074\u3057\u305f\u6620\u753b\u3092\u540c\u671f\u3057\u307e\u3059',
            sendFavorites: '\u304a\u6c17\u306b\u5165\u308a\u3092\u9001\u4fe1', dateFiltering: '\u65e5\u4ed8\u30d5\u30a3\u30eb\u30bf\u30fc', enableDateFilter: '\u65e5\u4ed8\u30d5\u30a3\u30eb\u30bf\u30fc\u3092\u6709\u52b9\u306b\u3059\u308b',
            enableDateFilterDesc: '\u6307\u5b9a\u3057\u305f\u65e5\u6570\u4ee5\u5185\u306b\u8996\u8074\u3057\u305f\u6620\u753b\u306e\u307f\u540c\u671f', daysToLookBack: '\u65e5\u6570',
            watchlistSync: '\u30a6\u30a9\u30c3\u30c1\u30ea\u30b9\u30c8\u540c\u671f', watchlistDesc: '\u516c\u958b\u30a6\u30a9\u30c3\u30c1\u30ea\u30b9\u30c8\u3092\u30d7\u30ec\u30a4\u30ea\u30b9\u30c8\u3068\u3057\u3066\u540c\u671f\u3059\u308bLetterboxd\u30e6\u30fc\u30b6\u30fc\u540d\u3092\u8ffd\u52a0\u3057\u3066\u304f\u3060\u3055\u3044\u3002',
            addWatchlist: '\u30a6\u30a9\u30c3\u30c1\u30ea\u30b9\u30c8\u3092\u8ffd\u52a0', remove: '\u524a\u9664', save: '\u4fdd\u5b58', saving: '\u4fdd\u5b58\u4e2d...',
            settingsSaved: '\u8a2d\u5b9a\u304c\u4fdd\u5b58\u3055\u308c\u307e\u3057\u305f\u3002', errorSaving: '\u4fdd\u5b58\u30a8\u30e9\u30fc\u3002', authFailed: '\u8a8d\u8a3c\u306b\u5931\u6557\u3057\u307e\u3057\u305f\u3002'
        }
    };

    var lang = (document.documentElement.lang || navigator.language || 'en').substring(0, 2).toLowerCase();
    var t = STRINGS[lang] || STRINGS.en;

    // Store initial credentials to detect changes

    // ===== Sidebar injection =====
    function injectSidebarEntry() {
        if (document.getElementById(MENU_ID)) {
            return;
        }

        var scrollContainer = document.querySelector('.mainDrawer-scrollContainer');
        if (!scrollContainer) {
            return;
        }

        var section = document.createElement('div');
        section.id = MENU_ID;
        section.className = 'navMenuSection';

        var header = document.createElement('h3');
        header.className = 'sidebarHeader';
        header.textContent = 'Letterboxd';
        section.appendChild(header);

        var link = document.createElement('a');
        link.setAttribute('is', 'emby-linkbutton');
        link.className = 'navMenuOption lnkMediaFolder emby-button';
        link.href = '#';
        link.addEventListener('click', function (e) {
            e.preventDefault();
            closeSidebar();
            setTimeout(openDialog, 400);
        });
        link.innerHTML = '<span class="material-icons navMenuOptionIcon" aria-hidden="true">movie_filter</span>' +
            '<span class="navMenuOptionText">Letterboxd Sync</span>';
        section.appendChild(link);

        var adminSection = scrollContainer.querySelector('.adminMenuOptions');
        if (adminSection) {
            adminSection.parentNode.insertBefore(section, adminSection);
        } else {
            scrollContainer.appendChild(section);
        }
    }

    function closeSidebar() {
        var drawer = document.querySelector('.mainDrawer');
        if (drawer) {
            drawer.classList.remove('opened');
            drawer.style.transform = '';
        }
        var backdrop = document.querySelector('.mainDrawer-backdrop');
        if (backdrop) {
            backdrop.classList.remove('opened');
            backdrop.style.display = 'none';
        }
        document.body.classList.remove('bodyWithPopupOpen');
    }

    // ===== Dialog =====
    function openDialog() {
        var existing = document.getElementById(DIALOG_ID);
        if (existing) {
            existing.remove();
        }

        var overlay = document.createElement('div');
        overlay.id = DIALOG_ID;
        overlay.style.cssText = 'position:fixed;top:0;left:0;right:0;bottom:0;z-index:999;display:flex;align-items:center;justify-content:center;';

        // Backdrop
        var backdrop = document.createElement('div');
        backdrop.style.cssText = 'position:absolute;top:0;left:0;right:0;bottom:0;background:rgba(0,0,0,0.7);';
        backdrop.addEventListener('click', function () { closeDialog(); });
        overlay.appendChild(backdrop);

        // Dialog panel
        var panel = document.createElement('div');
        panel.style.cssText = [
            'position:relative;',
            'background:var(--theme-card-background, #1c1c1e);',
            'color:var(--theme-text-color, #fff);',
            'border-radius:8px;',
            'width:90%;',
            'max-width:600px;',
            'max-height:85vh;',
            'overflow-y:auto;',
            'padding:1.5em 2em;',
            'box-shadow:0 8px 32px rgba(0,0,0,0.5);'
        ].join('');

        // Header bar
        var headerBar = document.createElement('div');
        headerBar.style.cssText = 'display:flex;align-items:center;justify-content:space-between;margin-bottom:1em;';

        var title = document.createElement('h2');
        title.style.cssText = 'margin:0;font-size:1.4em;';
        title.textContent = 'Letterboxd Sync';
        headerBar.appendChild(title);

        var closeBtn = document.createElement('button');
        closeBtn.type = 'button';
        closeBtn.className = 'paper-icon-button-light';
        closeBtn.style.cssText = 'background:none;border:none;color:inherit;cursor:pointer;font-size:1.4em;padding:4px;';
        closeBtn.innerHTML = '<span class="material-icons">close</span>';
        closeBtn.addEventListener('click', function () { closeDialog(); });
        headerBar.appendChild(closeBtn);

        panel.appendChild(headerBar);

        // Form content
        var chk = '<span class="checkboxOutline"><span class="material-icons checkboxIcon checkboxIcon-checked check" aria-hidden="true"></span><span class="material-icons checkboxIcon checkboxIcon-unchecked" aria-hidden="true"></span></span>';
        var formHtml = [
            '<form id="LetterboxdSyncUserConfigForm">',
            '  <h3 style="margin-top:0;">' + t.login + '</h3>',
            '  <div class="inputContainer">',
            '    <label class="inputLabel" for="lbxd-account">' + t.username + '</label>',
            '    <input type="text" id="lbxd-account" class="emby-input" autocomplete="off" />',
            '    <div class="fieldDescription">' + (t.usernameHint || 'Your Letterboxd username (from letterboxd.com/&lt;username&gt;/), not your email address.') + '</div>',
            '  </div>',
            '  <div class="inputContainer">',
            '    <label class="inputLabel" for="lbxd-key">' + t.password + '</label>',
            '    <input type="password" id="lbxd-key" class="emby-input" autocomplete="new-password" />',
            '    <div class="fieldDescription">' + (t.passwordHint || 'Your Letterboxd password. Used once to obtain a token; it is never stored.') + '</div>',
            '    <div class="fieldDescription" id="lbxd-link-status"></div>',
            '  </div>',
            '  <h3>' + t.options + '</h3>',
            '  <div class="checkboxContainer checkboxContainer-withDescription">',
            '    <label class="emby-checkbox-label">',
            '      <input is="emby-checkbox" type="checkbox" id="lbxd-enable" data-embycheckbox="true" class="emby-checkbox" />',
            '      <span class="checkboxLabel">' + t.enableSync + '</span>' + chk,
            '    </label>',
            '    <div class="fieldDescription checkboxFieldDescription">' + t.enableSyncDesc + '</div>',
            '  </div>',
            '  <div class="checkboxContainer checkboxContainer-withDescription">',
            '    <label class="emby-checkbox-label">',
            '      <input is="emby-checkbox" type="checkbox" id="lbxd-sendfavorite" data-embycheckbox="true" class="emby-checkbox" />',
            '      <span class="checkboxLabel">' + t.sendFavorites + '</span>' + chk,
            '    </label>',
            '  </div>',
            '  <h3>' + t.dateFiltering + '</h3>',
            '  <div class="checkboxContainer checkboxContainer-withDescription">',
            '    <label class="emby-checkbox-label">',
            '      <input is="emby-checkbox" type="checkbox" id="lbxd-datefilter" data-embycheckbox="true" class="emby-checkbox" />',
            '      <span class="checkboxLabel">' + t.enableDateFilter + '</span>' + chk,
            '    </label>',
            '    <div class="fieldDescription checkboxFieldDescription">' + t.enableDateFilterDesc + '</div>',
            '  </div>',
            '  <div class="inputContainer">',
            '    <label class="inputLabel" for="lbxd-datedays">' + t.daysToLookBack + '</label>',
            '    <input type="number" id="lbxd-datedays" class="emby-input" min="1" max="5000" />',
            '  </div>',
            '  <h3>' + t.watchlistSync + '</h3>',
            '  <div class="fieldDescription" style="margin-bottom:0.5em;">' + t.watchlistDesc + '</div>',
            '  <div id="lbxd-watchlist-container"></div>',
            '  <button type="button" id="lbxd-add-watchlist" class="raised emby-button" style="margin:0.5em 0;">',
            '    <span>' + t.addWatchlist + '</span>',
            '  </button>',
            '  <br/><br/>',
            '  <button type="submit" class="raised button-submit block emby-button">',
            '    <span>' + t.save + '</span>',
            '  </button>',
            '</form>'
        ].join('\n');

        var formContainer = document.createElement('div');
        formContainer.innerHTML = formHtml;
        panel.appendChild(formContainer);

        overlay.appendChild(panel);
        document.body.appendChild(overlay);

        // Close on Escape
        overlay._onKeyDown = function (e) {
            if (e.key === 'Escape') {
                closeDialog();
            }
        };
        document.addEventListener('keydown', overlay._onKeyDown);

        loadConfig(panel);
        setupForm(panel);
    }

    function closeDialog() {
        var dialog = document.getElementById(DIALOG_ID);
        if (dialog) {
            if (dialog._onKeyDown) {
                document.removeEventListener('keydown', dialog._onKeyDown);
            }
            dialog.remove();
        }
    }

    // ===== Config loading =====
    function loadConfig(view) {
        var url = ApiClient.getUrl('Jellyfin.Plugin.LetterboxdSync/UserConfig');
        ApiClient.ajax({ type: 'GET', url: url, dataType: 'json' }).then(function (account) {
            view.querySelector('#lbxd-account').value = account.userLetterboxd || '';
            view.querySelector('#lbxd-key').value = '';
            view.querySelector('#lbxd-enable').checked = account.enable || false;
            view.querySelector('#lbxd-sendfavorite').checked = account.sendFavorite || false;
            view.querySelector('#lbxd-datefilter').checked = account.enableDateFilter || false;
            view.querySelector('#lbxd-datedays').value = account.dateFilterDays || 7;

            var status = view.querySelector('#lbxd-link-status');
            if (status) {
                status.textContent = account.isLinked
                    ? (t.linked || 'Account linked. Leave the password blank to keep it, or re-enter it to re-link.')
                    : (t.notLinked || 'Not linked. Enter your Letterboxd credentials. Your password is exchanged for a token on save and is never stored.');
            }

            var container = view.querySelector('#lbxd-watchlist-container');
            container.innerHTML = '';
            var usernames = account.watchlistUsernames || [];
            for (var i = 0; i < usernames.length; i++) {
                addWatchlistEntry(container, usernames[i]);
            }
        });
    }

    function addWatchlistEntry(container, value) {
        var row = document.createElement('div');
        row.style.cssText = 'display:flex;align-items:center;gap:10px;margin-bottom:0.5em;';

        var input = document.createElement('input');
        input.type = 'text';
        input.className = 'emby-input watchlist-entry';
        input.placeholder = t.watchlistPlaceholder || 'watchlist link or Letterboxd profile';
        input.setAttribute('autocomplete', 'off');
        input.value = value || '';
        input.style.flex = '1';

        var removeBtn = document.createElement('button');
        removeBtn.type = 'button';
        removeBtn.className = 'raised emby-button';
        removeBtn.textContent = t.remove;
        removeBtn.addEventListener('click', function () {
            row.remove();
        });

        row.appendChild(input);
        row.appendChild(removeBtn);
        container.appendChild(row);
    }

    // ===== Form logic =====
    function setupForm(view) {
        view.querySelector('#lbxd-add-watchlist').addEventListener('click', function () {
            addWatchlistEntry(view.querySelector('#lbxd-watchlist-container'), '');
        });

        view.querySelector('#LetterboxdSyncUserConfigForm').addEventListener('submit', function (e) {
            e.preventDefault();

            var configUser = {
                UserLetterboxd: view.querySelector('#lbxd-account').value,
                PasswordLetterboxd: view.querySelector('#lbxd-key').value,
                Enable: view.querySelector('#lbxd-enable').checked,
                SendFavorite: view.querySelector('#lbxd-sendfavorite').checked,
                EnableDateFilter: view.querySelector('#lbxd-datefilter').checked,
                DateFilterDays: parseInt(view.querySelector('#lbxd-datedays').value) || 7,
                WatchlistUsernames: []
            };

            var inputs = view.querySelectorAll('.watchlist-entry');
            for (var i = 0; i < inputs.length; i++) {
                var val = inputs[i].value.trim();
                if (val) {
                    configUser.WatchlistUsernames.push(val);
                }
            }

            var data = JSON.stringify(configUser);
            var submitBtn = view.querySelector('button[type="submit"]');
            submitBtn.disabled = true;
            submitBtn.querySelector('span').textContent = t.saving;

            function onDone() {
                submitBtn.disabled = false;
                submitBtn.querySelector('span').textContent = t.save;
            }

            // SaveUserConfig performs the OAuth exchange server-side (password -> token) and returns
            // an error if the credentials are invalid, so a separate auth step is no longer needed.
            var saveUrl = ApiClient.getUrl('Jellyfin.Plugin.LetterboxdSync/UserConfig');
            ApiClient.ajax({ type: 'POST', url: saveUrl, data: data, contentType: 'application/json' }).then(function () {
                onDone();
                Dashboard.alert(t.settingsSaved);
                closeDialog();
            }).catch(function (response) {
                onDone();
                if (response && response.json) {
                    response.json().then(function (res) {
                        Dashboard.alert((t.authFailed || 'Error') + ' ' + (res.Message || ''));
                    }).catch(function () {
                        Dashboard.alert(t.errorSaving);
                    });
                } else {
                    Dashboard.alert(t.errorSaving);
                }
            });
        });
    }

    // ===== Init =====
    var observer = new MutationObserver(function () {
        injectSidebarEntry();
    });

    observer.observe(document.body, {
        childList: true,
        subtree: true
    });

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', injectSidebarEntry);
    } else {
        injectSidebarEntry();
    }
})();
