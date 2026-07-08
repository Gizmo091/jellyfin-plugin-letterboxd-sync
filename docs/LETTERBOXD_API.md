# Letterboxd API — Reverse-Engineering & Maintenance Notes

> Last verified working: **2026-07-06** — live `GET /films?filmId=tmdb:238` returned HTTP 200, and
> the plugin's own C# signing/lookup path is covered by live integration tests in
> `LetterboxdSync.Tests` (all green). The write path (`/auth/token`, `/log-entries`) uses the same
> signing and could not be exercised in CI without real member credentials.

## 1. Why this document exists

Historically this plugin talked to Letterboxd by **scraping the public website**
(`https://letterboxd.com/...`), logging in with a username/password form and parsing HTML.
That website sits behind **Cloudflare bot protection**, which now returns `403 Forbidden`
("Just a moment...") for automated requests. This is the root cause of:

- issue #5 — `TMDB lookup returned 403 (Forbidden) for https://letterboxd.com/tmdb/<id>`
- issue #4 — `MarkAsWatched` → "Sorry, your request could not be processed. Please try again later."

The Letterboxd **mobile app does not scrape the website** — it talks to a proper JSON REST
API at `api.letterboxd.com` which is **not** behind the same Cloudflare challenge. Moving the
plugin's network layer to this API removes the whole class of Cloudflare/anti-bot failures.

This file documents how that API works so the plugin can be maintained (and re-derived) if
Letterboxd changes anything.

## 2. Overview

- **Base URL:** `https://api.letterboxd.com/api/v0`
  - `https://st.letterboxd.com/api/v0` also answers (staging mirror) — do **not** rely on it.
- **Format:** JSON. For `POST`/`PATCH`/`DELETE`, required params go in the JSON body
  (except `/auth/token`, which is `application/x-www-form-urlencoded`).
- **Auth model:** OAuth2. Every request is *signed* with a first-party **API key + secret**
  (see §4). Member-scoped calls additionally carry a **Bearer access token** (see §5).
- **Official docs:** <https://api-docs.letterboxd.com/> (Swagger UI; the raw spec is not
  publicly downloadable — it 403s). A machine-generated TypeScript client mirroring the full
  spec is very useful as a reference: <https://github.com/erunion/letterboxd-client>.

## 3. Credentials (extracted from the Android app)

The official API is "by request only" (email `api@letterboxd.com`). The **first-party app key**
is hardcoded in the Letterboxd APK and can be extracted. As of 2026-07 these values are valid:

```
API key    : ebe3d27ec52a35fc8d1835c6531c37bd72b7a54337666d5bd759379b72ae16f0
API secret : c60ce045d25bc90cb56026a8dd621eebeef995cbecc51951192da75348c977cd
```

> The secret is stored obfuscated in the APK as base64 split across constants. In the
> `lumaaaaaa/letterboxdAPI` PoC it appears as
> `base64("YzYwY2UwNDVkMjV" + "iYzkwY2I1NjAyNm" + "E4ZGQ2MjFlZWJlZ" + "WY5OTVjYmVjYzUx" + "OTUxMTkyZGE3NTM" + "0OGM5NzdjZA==")`
> → the hex string above.

### How to re-extract if the key is rotated

If the API starts returning `401 { "type": "...", "message": "..." }` with reason
*"An invalid API key or computed signature was supplied"* on **public** endpoints, the key was
rotated. To recover it:

1. Install the Letterboxd Android app on an emulator/phone with a proxy CA installed.
2. Run **mitmproxy** (or Charles/HTTP Toolkit) as a MITM proxy; the app may need SSL-unpinning
   (Frida / `objection`).
3. Observe requests to `https://api.letterboxd.com/api/v0/...`. The `apikey` query param is the
   key. To get the secret, either pull it from the APK (base64 constants as above) or brute the
   signing input against a captured `signature` (the algorithm in §4 is fixed).

References that document this process:
- <https://blog.alexbeals.com/posts/extracting-letterboxd-tokens-with-mitmproxy>
- <https://github.com/lumaaaaaa/letterboxdAPI> (Go PoC — the signing function)

## 4. Request signing (required on EVERY request)

Confirmed against the reference client `erunion/letterboxd-client` (`src/lib/core.ts`) **and**
verified live. Algorithm:

1. Start from the query params for the call (may be empty). Add:
   - `apikey`   = the API key
   - `nonce`    = a fresh UUID v4 (string)
   - `timestamp`= current Unix time in **seconds**
2. Build the full URL including those params:
   `https://api.letterboxd.com/api/v0<path>?<encoded query>`
3. Compute the signing base string, **NUL-separated** (`\0`, a literal zero byte):

   ```
   sigBase = METHOD_UPPERCASE + "\0" + FULL_URL + "\0" + BODY
   ```

   - `BODY` is the exact request body **bytes** you will send:
     - GET / no body → empty string
     - `/auth/token` → the form string `grant_type=password&username=...&password=...`
     - JSON endpoints → the exact `JSON.stringify(body)` string
4. `signature = lowercase_hex( HMAC_SHA256(key = API_SECRET, message = sigBase) )`
5. Append `signature=<sig>` to the query string and send.

> **Critical invariant:** the URL you *sign* (minus the `signature` param) must be **byte-for-byte**
> the URL you *send* minus `signature`, and the `BODY` you sign must be byte-for-byte the body you
> send. Param **order does not matter** (the server strips `signature` and re-validates over what it
> received) — verified: both sorted (our test) and insertion-order (erunion) signatures are accepted.
> The safest implementation is: build the query string once, sign it, then append `&signature=...` to
> *that same string*; and serialize the body once and reuse the identical bytes for signing and sending.

Reference implementations of the signing function:
- Go: `lumaaaaaa/letterboxdAPI/functions.go` → `signRequest` / `signature`
- TS: `erunion/letterboxd-client/src/lib/core.ts` → `buildParams`
- Legacy Python (uses the same HMAC scheme): `letterboxd` pkg `services/auth.html`

## 5. Authentication (member access token)

Member-scoped actions (logging a film, reading `/me`, private watchlist) need a **Bearer token**
obtained via the OAuth2 **password grant** (first-party only — this app key is first-party, so it
works).

### 5.1 Get a token — `POST /auth/token`

- Headers: `Content-Type: application/x-www-form-urlencoded`, `Accept: application/json`
- Body (form-urlencoded, also fed into the signature as `BODY`):

  ```
  grant_type=password&username=<letterboxd-username>&password=<letterboxd-password>
  ```
- Still signed with apikey+nonce+timestamp+signature (see §4).
- Responses:
  - `200` → `AccessToken` (below)
  - `400` `OAuthError` → wrong credentials / account not found
  - `401` → invalid API key or signature

`AccessToken` shape:

```jsonc
{
  "access_token":  "…",        // use as: Authorization: Bearer <access_token>
  "token_type":    "bearer",
  "expires_in":    3600,        // seconds (always ~1h)
  "refresh_token": "…",         // long-lived; only dies if Letterboxd revokes it
  "issuer":        "…"
}
```

### 5.2 Refresh — `POST /auth/token`

Same endpoint, body:

```
grant_type=refresh_token&refresh_token=<refresh-token>
```

> **Recommended storage model:** ask the user for username+password **once**, exchange it for
> tokens, then persist only the **refresh_token** (and discard the password). On each sync, if the
> access token is expired, refresh. This avoids storing plaintext passwords and needs no re-login.

### 5.3 Authenticated request

Add header `Authorization: Bearer <access_token>` **in addition to** the apikey signature.

## 6. Endpoints used by this plugin

Full path list confirmed from `erunion/letterboxd-client` (`src/index.ts`). The ones we need:

### 6.1 Resolve a TMDB id → Letterboxd film — `GET /films`  (replaces scraping `/tmdb/<id>`)

- Query: `filmId=tmdb:<tmdbId>` (the `filmId` filter accepts
  *"up to 100 Letterboxd IDs or TMDB IDs prefixed with `tmdb:`, or IMDB IDs prefixed with `imdb:`"*),
  optionally `perPage=1`.
- Public (apikey signature is enough; no bearer token required).
- Response `FilmsResponse`: `{ items: FilmSummary[], next }`. Take `items[0]`:
  - `items[0].id`   → the **film LID** (e.g. `2aNK` for The Godfather) — this is what you log.
  - `items[0].name`, `items[0].releaseYear`, `items[0].links` (contains the letterboxd/tmdb URLs).

Verified live:

```
GET /films?filmId=tmdb:238&perPage=1  → 200
items[0] = { id:"2aNK", name:"The Godfather", releaseYear:1972 }
```

### 6.2 Mark a film as watched / log it — `POST /log-entries`  (replaces `save-diary-entry` scrape)

- Auth: **Bearer required**. JSON body = `LogEntryCreationRequest`:

  ```jsonc
  {
    "filmId": "<film LID from 6.1>",     // REQUIRED
    "diaryDetails": {                     // present ⇒ it's a diary entry (a "watch")
      "diaryDate": "YYYY-MM-DD",          // ISO 8601 date the film was watched
      "rewatch": false
    },
    "like":   false,                      // optional — the 'heart'
    "rating": 3.5,                        // optional — 0.5..5.0 in 0.5 steps
    "tags":   ["jellyfin"]                // optional
  }
  ```
- Responses:
  - `200`/`201` `LogEntry` → created. **In practice the live API returns `200 OK`** with the new
    `LogEntry` body, even though the reference spec documents `201`. Treat both as success and read
    `id` (the log entry LID) from the body.
  - `204` → **no action taken, the log entry already exists** (built-in idempotency — a natural
    replacement for the old `GetDateLastLog` "already logged this date" check)
  - `400` bad request · `401` no authenticated member · `404` film not found

### 6.3 Already-logged check (optional) — `GET /log-entries`

If we want to avoid re-logging beyond the `204` behaviour: `GET /log-entries?film=<LID>&member=<memberLID>`
and inspect each entry's `diaryDetails.diaryDate`. Get the authenticated member LID from `GET /me`.

### 6.4 Watchlist sync — `GET /member/{memberLID}/watchlist`

- Returns films (paginated via `cursor`/`next`, `perPage` up to 100). Each `FilmSummary` carries
  `links` including the TMDB id — match those against the Jellyfin library (same as today).
- Needs the **member LID** for a username. Resolve username → LID via
  `GET /search?input=<username>&searchMethod=Autocomplete&include=MemberSearchItem&perPage=1`
  → `items[0].member.id`. (For the signed-in user, `GET /me` → `member.id`.)

### 6.5 Diary import (reverse sync) — `GET /me` + `GET /log-entries`

Used by `LetterboxdDiaryImportTask` to mirror the member's own diary back into Jellyfin:

1. `GET /me` (Bearer required) → the authenticated member LID (`member.id`, falling back to a top-level `id`).
2. `GET /log-entries?member=<memberLID>&where=HasDiaryDate&perPage=100` (paginated via `cursor`/`next`).
   Each `LogEntry` item carries `film` (a `FilmSummary` whose `links` hold the TMDB id),
   `diaryDetails.diaryDate` (`YYYY-MM-DD`), and an optional `rating` (0.5-5.0). Match the TMDB id
   against the library and mark the film watched with the diary date.

## 7. Mapping — old scraping method → new API call

| `LetterboxdApi` method (old, scraping)            | New API call                                                            |
|---------------------------------------------------|-------------------------------------------------------------------------|
| `Authenticate(user, pass)` (sign-in form + CSRF)  | `POST /auth/token` grant_type=password → store refresh_token (§5)        |
| `SearchFilmByTmdbId(tmdbId)`                       | `GET /films?filmId=tmdb:<id>` → `items[0].id` (LID) (§6.1)               |
| `MarkAsWatched(slug, id, date, tags, liked)`      | `POST /log-entries` with `diaryDetails.diaryDate` (§6.2)                 |
| `GetDateLastLog(slug)`                            | rely on `204` from §6.2, or `GET /log-entries?film&member` (§6.3)        |
| `GetFilmsFromList` / watchlist scrape             | `GET /member/{lid}/watchlist` (§6.4)                                     |
| `SetRawCookies` / Cloudflare cookie workaround     | **removed** — no longer needed                                          |

Note: the film **slug** is no longer needed for logging — the API uses the **LID**. `FilmResult`
can carry the LID (and TMDB id for watchlist matching).

## 8. Risks & maintenance

- **Shared first-party key.** We reuse the Letterboxd app's key. Letterboxd could rotate/revoke it
  (§3 shows how to recover) or block this usage. This is a ToS grey area (interoperability for
  personal use). Keep the key in one constant so it is trivial to update.
- **Rate limiting.** Keep the existing small randomized delays between calls to stay well-mannered.
- **Password grant is first-party.** If Letterboxd disables it for this key, fall back to prompting
  the user for a refresh token captured from the app (see §3 mitmproxy method).
- **Spec drift.** Re-check field names against `erunion/letterboxd-client` (kept in sync with the
  official spec) if responses stop parsing.

## 9. References

- Official docs: <https://api-docs.letterboxd.com/>
- Reference TS client (full spec): <https://github.com/erunion/letterboxd-client>
- Go signing PoC + extracted key: <https://github.com/lumaaaaaa/letterboxdAPI>
- Token extraction write-up: <https://blog.alexbeals.com/posts/extracting-letterboxd-tokens-with-mitmproxy>
- Legacy Python client (same HMAC scheme): <https://pypi.org/project/letterboxd/>
