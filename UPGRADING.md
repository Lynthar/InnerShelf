# Upgrading to a new Jellyfin release

This plugin pins to a minimum Jellyfin version (`targetAbi` in `meta.json`).
Three things determine compatibility, and they don't all move together —
this doc is the checklist for keeping them in sync when Jellyfin releases.

## Where versions live

| File | Field | Purpose |
|---|---|---|
| `Jellyfin.Plugin.InnerShelf/meta.json` | `targetAbi` | What the Jellyfin server checks at install time. Server version must be `>=` this. |
| `build.yaml` | `targetAbi` | What JPRM bakes into the published manifest. Must match `meta.json`. |
| `Jellyfin.Plugin.InnerShelf.csproj` | `Jellyfin.Controller` / `Jellyfin.Model` | The API surface the code is compiled against. |
| `Jellyfin.Plugin.InnerShelf.Tests.csproj` | `Jellyfin.Controller` / `Jellyfin.Model` | Same as main project, on purpose — so tests catch any "we used a post-floor API" mistake. |

The csproj `Version` is the **floor** (NuGet allows higher at restore time).
The `targetAbi` in the manifest is the **install gate**. Keep them in sync
or you'll either ship a plugin that won't install on the version you tested,
or one that installs but uses APIs that don't exist on the user's server.

## What the automation does

- **Dependabot** (`.github/dependabot.yml`) — every Monday, opens a PR if
  Jellyfin SDK packages have new versions. Both `Jellyfin.Controller` and
  `Jellyfin.Model` are grouped into one PR.
- **CI** (`.github/workflows/build-test.yml`) — runs `dotnet build` +
  `dotnet test` on every push and PR. `TreatWarningsAsErrors=true` in the
  csproj means any deprecated-API warning from the new SDK fails the build.

So: Dependabot PR appears → CI runs → green = no API surface broke → red =
look at the warnings/errors before merging.

## Manual checklist per Jellyfin release

CI green only proves the code still compiles against the new SDK. It does
**not** prove the plugin works on a real Jellyfin server. Do these manually:

### Patch release (e.g. 10.11.8 → 10.11.9)

1. Merge the Dependabot PR (CI is enough for patch releases — Jellyfin
   promises ABI stability across patches).
2. **Don't** bump `targetAbi` — there's no need to exclude users still on
   the older patch.
3. No new release tag needed unless you want to advertise compat.

### Minor release (e.g. 10.11.x → 10.12.0)

1. Wait for Dependabot to open the PR (or bump manually).
2. Build the plugin DLL: `dotnet build -c Release`
3. Spin up the new Jellyfin in Docker for testing:
   ```
   docker run -d --rm --name jf-test \
     -p 8096:8096 \
     -v /tmp/jf-test/config:/config \
     -v /tmp/jf-test/cache:/cache \
     jellyfin/jellyfin:10.12.0
   ```
4. Copy the plugin DLL + `AngleSharp.dll` + `meta.json` into
   `/tmp/jf-test/config/plugins/InnerShelf/` and restart the container.
5. Verify in `http://localhost:8096`: plugin loads (Dashboard → Plugins),
   the InnerShelf settings page renders, a test scrape of one item works.
6. If everything works, bump `targetAbi` in **both** `meta.json` and
   `build.yaml` to the new version (e.g. `10.12.0.0`), then merge.
7. Tag a new plugin release (e.g. `v0.2.0`).

### Major release (e.g. 10.x → 11.0)

Don't expect Dependabot's PR to be mergeable. Major releases usually break
plugin APIs. Treat as a port:
1. Branch off `main` to `port/jf-11`.
2. Bump SDK refs, fix every compile error.
3. Fix every runtime error during the smoke test.
4. Decide whether to release as a separate plugin version line (keep `main`
   on 10.x, port branch on 11.x) or do a hard cutover.

## When something goes wrong

- **Build error after Dependabot PR**: a Jellyfin API got removed or
  changed. Look at the error, find the new equivalent in the changelog,
  patch the code in the PR branch.
- **Plugin loads but throws `MissingMethodException` on first use**:
  reflection-resolved API got renamed between SDK and runtime. Same fix
  as above, except harder to spot in advance — usually triggered by
  cross-major upgrades only.
- **`targetAbi` rejects install on the version you just tested**: you
  forgot to bump one of the two `targetAbi` fields. Both `meta.json` and
  `build.yaml` must match.
