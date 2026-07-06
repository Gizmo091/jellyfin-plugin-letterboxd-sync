# Releasing & CI

This repo has two GitHub Actions workflows:

| Workflow | File | Trigger | What it does |
|----------|------|---------|--------------|
| **CI** | `.github/workflows/ci.yml` | every push to `master`, every PR, manual | builds + runs the test suite |
| **Publish** | `.github/workflows/publish.yaml` | pushing a `v*` tag, or manual | builds a **draft** release → runs the full tests → publishes only if they pass |

## One-time setup

Add two repository secrets (Settings → Secrets and variables → Actions → *New repository secret*),
pointing at a **throwaway Letterboxd account used only for testing**:

- `LETTERBOXD_TEST_USERNAME`
- `LETTERBOXD_TEST_PASSWORD`

Optionally configure an **Environment** named `prd` (Settings → Environments) with a required
reviewer. The `publish` job targets that environment, so the release will wait for a manual approval
after the tests pass and before it goes public. Leave it unprotected for a fully automatic publish
once tests are green.

## The tests

There are two kinds of tests in `LetterboxdSync.Tests`:

- **Read tests** — hit the public Letterboxd API (film lookup by TMDB id, input parsing). They need
  no credentials and run on every CI build.
- **Write tests** (`LetterboxdApiWriteTests`) — exercise the authenticated path: password grant →
  refresh grant, and logging a film then **deleting it again** so the test account stays clean.
  They are **opt-in**: they only run when

  ```
  LETTERBOXD_TEST_USERNAME, LETTERBOXD_TEST_PASSWORD  and  LETTERBOXD_RUN_WRITE_TESTS=true
  ```

  are all set. This keeps the test account from being hit on every commit.

### When do the write tests run?

- **Ordinary push / PR:** ❌ skipped (`LETTERBOXD_RUN_WRITE_TESTS` is not set).
- **Manual CI run** (Actions → *CI* → *Run workflow*): ✅ they run — use this to validate the write
  path on demand.
- **During a release** (the `publish` workflow's `test` job): ✅ they run and **must pass** for the
  release to be published.

### Running the write tests locally

```bash
export LETTERBOXD_TEST_USERNAME='your-test-account'
export LETTERBOXD_TEST_PASSWORD='...'
export LETTERBOXD_RUN_WRITE_TESTS=true
dotnet test LetterboxdSync.Tests/LetterboxdSync.Tests.csproj -c Release
```

Without those variables the write tests are reported as *skipped*, not failed.

## Cutting a release

Releases are **tag-driven**: the version comes from the tag, nothing is auto-bumped.

1. Make sure `master` is green in CI and, ideally, run the **CI** workflow manually once so the
   write tests pass before you tag.
2. Pick the next [semver](https://semver.org/) version, e.g. `1.9.0`.
3. Tag and push:

   ```bash
   git checkout master && git pull
   git tag v1.9.0
   git push origin v1.9.0
   ```

That triggers the **Publish** workflow, which:

1. **build** — derives the version from the tag (`v1.9.0` → `1.9.0.0`), builds `LetterboxdSync.dll`,
   zips it, and creates a **draft** GitHub release with the asset attached.
2. **test** — runs the whole test suite *including* the authenticated write tests. If anything fails
   the workflow stops here and the release stays a **draft** (nothing is published).
3. **publish** — (waits for `prd` approval if configured, then) appends the version to `manifest.json`,
   commits it to `master` (`chore(release): vX.Y.Z [skip ci]`), and flips the release from draft to
   **published**.

The published release's zip URL is what `manifest.json` points Jellyfin at, so users get the update
through the plugin catalog automatically.

### Manual publish (no tag)

Actions → *🚀 Publish* → *Run workflow*, and enter the version (e.g. `1.9.0`). Same flow; the tag is
created for you.

## Versioning notes

- Tag `vX.Y.Z` → plugin version `X.Y.Z.0` in `manifest.json` and `build.yaml`.
- `targetAbi` is `10.11.9.0` — the plugin uses `IUserManager.GetUsers()`, introduced in Jellyfin
  **10.11.9**, so it will not install on older 10.11.x. Bump `TARGETABI` in `publish.yaml` (and
  `build.yaml`) if you retarget.

## If a release fails

- **Tests failed:** the release is left as an unpublished **draft**. Fix the issue on `master`, then
  delete the draft release and the tag and start over:

  ```bash
  gh release delete v1.9.0 --cleanup-tag   # or delete the draft in the UI, then:
  git push --delete origin v1.9.0
  ```

- **The app key was revoked** (write tests fail with 401 "invalid API key or computed signature"):
  see `docs/LETTERBOXD_API.md` §3 for how to re-extract the Letterboxd app key/secret, update the
  constants in `LetterboxdSync/LetterboxdApi.cs`, then re-tag.
