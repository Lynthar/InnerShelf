# InnerShelf

A Jellyfin plugin for managing adult video libraries. Install it on a vanilla Jellyfin Server.

[中文文档](README.zh-CN.md)

## Features

- **Product Code Detection** — Automatically extracts product codes (番号) from filenames, supporting censored, amateur, FC2, uncensored, HEYZO, and more
- **Metadata Scraping** — Built-in JavBus scraper fetches titles, release dates, genres, studios, actors, and cover images
- **Optional MetaTube Backend** — Connect to a [MetaTube](https://github.com/metatube-community) server for access to 37+ metadata sources
- **Rich Tagging** — Maps studios, labels, series, genres, and actors to Jellyfin's native metadata fields
- **Client Compatible** — Uses standard Jellyfin `Movie` type, works with Infuse, Swiftfin, Jellyfin Web, and all other clients
- **Actor Photos** — Fetches actress profile images automatically
- **Subtitle Generation** — Optional integration with [subtitle-forge](https://github.com/Lynthar/subtitle-forge) to generate and translate subtitles on a remote GPU host, triggered manually per-item

## Requirements

- **Jellyfin Server 10.11.0 or newer** — older patch releases will reject the install via `targetAbi` check.

## Installation

### From Plugin Repository (Recommended)

1. In Jellyfin Dashboard, go to **Administration → Plugins → Repositories** → **+** to add
2. Repository URL: `https://lynthar.github.io/InnerShelf/manifest.json`
3. Save → **Catalog** tab → install **InnerShelf**
4. Restart Jellyfin

### Manual Installation

1. Download the latest release ZIP from [Releases](https://github.com/Lynthar/InnerShelf/releases)
2. Extract to your Jellyfin plugins directory:
   - Linux: `~/.local/share/jellyfin/plugins/InnerShelf/`
   - Docker: `/config/plugins/InnerShelf/`
   - Windows: `%APPDATA%\jellyfin\plugins\InnerShelf\`
   - macOS: `~/.local/share/jellyfin/plugins/InnerShelf/`
3. Restart Jellyfin

## Configuration

After installation, go to **Administration → Plugins → InnerShelf** to configure:

| Setting | Description | Default |
|---------|-------------|---------|
| Enable JavBus | Use JavBus as a metadata source | On |
| Enable FANZA | _Planned_ — checkbox is wired to config but FANZA scraper is not yet implemented | On |
| MetaTube Server URL | Connect to a MetaTube backend (leave empty to disable) | Empty |
| Title Template | Display title format (`{code}`, `{title}`) | `{code} {title}` |
| HTTP Proxy | Proxy for metadata requests | Empty |
| Subtitle Forge Server URL | URL of a [subtitle-forge](https://github.com/Lynthar/subtitle-forge) server (leave empty to disable) | Empty |
| Subtitle Forge Token | Bearer token, must match `SUBTITLE_FORGE_TOKEN` on the GPU host | Empty |
| Subtitle Languages | Comma-separated target languages (e.g. `zh`, `zh,en`) | `zh` |
| Keep Original Subtitle | Save the source-language `.srt` alongside translations | On |
| Bilingual Subtitles | Merge source + target into one `.<src>-<tgt>.srt` | Off |
| Path Mappings | Rewrite Jellyfin-side paths to subtitle-forge-side paths | Empty |

## How It Works

### File Naming

InnerShelf parses product codes from your filenames. Supported formats:

| Type | Pattern | Example |
|------|---------|---------|
| Standard censored | `PREFIX-NUMBER` | `SSIS-001.mp4` |
| Amateur | `NNNPREFIX-NUMBER` | `390JAC-132.mp4` |
| FC2 | `FC2-PPV-NUMBER` | `FC2-PPV-1234567.mp4` |
| Uncensored | `NNNNNN-NNN` | `010120-001.mp4` |
| HEYZO | `HEYZO-NNNN` | `HEYZO-1234.mp4` |

Resolution tags (`1080p`, `4K`), codec tags (`x265`, `HEVC`), and bracket groups are automatically stripped.

Chinese subtitle suffixes (`-C`, `-ch`) and multi-disc indicators (`-cd1`, `-cd2`) are detected and preserved as metadata.

### Metadata Mapping

| Source Field | Jellyfin Field |
|-------------|---------------|
| Product Code | Provider ID (`InnerShelf`) |
| Japanese Title | Original Title |
| Display Title | Name (via template) |
| Release Date | Premiere Date |
| Genres | Genres |
| Studio/Maker | Studios |
| Label | Tag (`Label: ...`) |
| Series | Tag (`Series: ...`) |
| Actors | People (with photos) |
| Director | People (Director) |
| Front Cover | Primary Image |
| Full Cover | Backdrop Image |
| Rating | `XXX` |

## Subtitle Generation (Optional)

InnerShelf can hand off video files to a [subtitle-forge](https://github.com/Lynthar/subtitle-forge)
server running on a separate GPU machine. Generation is **manual per item** —
no automatic background processing, no scheduled scans.

### Setup

1. On the GPU host, install and run subtitle-forge in server mode (see its README).
   You'll need a bearer token — generate one with `openssl rand -hex 32`.
2. In InnerShelf settings, fill the **Subtitle Generation** section:
   - **Server URL** — `http://<gpu-host>:8765`
   - **Bearer Token** — same value as `SUBTITLE_FORGE_TOKEN` on the GPU host
   - **Path Mappings** — if Jellyfin and the GPU host see the storage at
     different paths (e.g. Jellyfin sees `/media/jav`, the GPU host has it
     mounted at `/Volumes/nas-jav` over SMB), add the rewrite rule.
     Longest prefix wins.

### Triggering generation

Two ways:

1. **Library-wide scheduled task** — Dashboard → Scheduled Tasks → "InnerShelf:
   Backfill subtitles" → Run now. Iterates every InnerShelf-managed movie,
   skips items that already have a `.<lang>.srt` sidecar for each configured
   target language, and submits jobs for the rest. Runs on demand by default;
   add a cron trigger in the Dashboard if you want it to run periodically.

2. **Per-item bookmarklet** for one-off generation — save this as a browser bookmark:

```javascript
javascript:(()=>{const m=location.hash.match(/[?&]id=([a-f0-9]{32})/i);if(!m){alert('Open a movie detail page first');return;}fetch('/InnerShelf/Subtitles/Generate?itemId='+m[1],{method:'POST',headers:{'X-Emby-Token':ApiClient.accessToken()}}).then(async r=>{const t=await r.text();alert(r.ok?('Submitted: '+t):('Failed '+r.status+': '+t));}).catch(e=>alert('Network error: '+e));})();
```

Open any movie detail page → click the bookmark → an alert shows the job id
on success. The endpoint requires admin (`RequiresElevation`).

### Endpoints

| Method | Path | Purpose |
|---|---|---|
| `POST` | `/InnerShelf/Subtitles/Generate?itemId={guid}&languages=zh,en` | Submit a job. `languages` optional, falls back to plugin config. |
| `GET`  | `/InnerShelf/Subtitles/Jobs/{jobId}` | Proxy to subtitle-forge `GET /jobs/{id}`; uses Jellyfin auth, no need to expose subtitle-forge token to clients. |

Output `.srt` files are written next to the source video; Jellyfin picks them
up automatically on the next library scan or item refresh.

## Building from Source

Requires [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).

```bash
git clone https://github.com/Lynthar/InnerShelf.git
cd InnerShelf
dotnet build
dotnet test
```

The compiled plugin DLL will be at `Jellyfin.Plugin.InnerShelf/bin/Debug/net9.0/Jellyfin.Plugin.InnerShelf.dll`.

## Architecture

```
Jellyfin.Plugin.InnerShelf/
├── Naming/          # Product code parsing from filenames
├── Sources/         # Metadata source abstraction
│   ├── BuiltIn/     # JavBus scraper
│   └── MetaTube/    # Optional MetaTube backend connector
├── Providers/       # Jellyfin metadata & image providers
├── Mapping/         # Internal models → Jellyfin types
├── ExternalIds/     # Product code as Jellyfin external ID
├── Subtitles/       # subtitle-forge HTTP client + REST controller + path mapper
└── Configuration/   # Plugin settings & web UI
```

## Maintaining

The plugin pins to Jellyfin Server `10.11.0+` via `targetAbi` in
`Jellyfin.Plugin.InnerShelf/meta.json` and `build.yaml` (both must match).
CI (`.github/workflows/build-test.yml`) runs `dotnet build` + `dotnet test`
on every push/PR; Dependabot (`.github/dependabot.yml`) opens a weekly PR
when the Jellyfin SDK has a new version. `TreatWarningsAsErrors=true` in
the csproj turns any deprecated-API warning into a CI failure, so a bad
SDK bump fails before merge.

### Releases

Tagging `vN.N.N` on `main` triggers `.github/workflows/release.yml`,
which builds + tests, packages `Jellyfin.Plugin.InnerShelf.dll` +
`AngleSharp.dll` + `meta.json` into `innershelf_<version>.0.zip`,
attaches the ZIP to a GitHub release, and appends the new version into
`manifest.json` on the `gh-pages` branch. The 3-part tag is padded to
4-part internally (`v0.1.1` → manifest version `0.1.1.0`) because
Jellyfin parses versions via `System.Version`, which mishandles the
missing 4th component when comparing.

**One-time setup for the plugin repository URL:**

1. Cut your first release tag (e.g. `v0.1.1`) — the workflow auto-creates
   the `gh-pages` branch on the first run.
2. Repo Settings → **Pages** → Source: `gh-pages` branch, root folder → Save.
3. Wait ~1 minute for the first publish; verify
   `https://lynthar.github.io/InnerShelf/manifest.json` returns the manifest.
4. (Optional) Repo Settings → **Branches/Rules** — if a "require signed
   commits" rule covers `gh-pages`, scope it to `refs/heads/main` only or
   add the `github-actions[bot]` as a bypass actor; the workflow's commits
   to `gh-pages` are unsigned.

When Jellyfin releases a new version:

- **Patch (e.g. `10.11.8` → `10.11.9`)** — merge the Dependabot PR if CI is green. Don't bump `targetAbi` (no need to exclude users still on the older patch).
- **Minor (e.g. `10.11` → `10.12`)** — merge after also smoke-testing on a real server (`docker run --rm -p 8096:8096 -v /tmp/jf:/config jellyfin/jellyfin:10.12.0`, copy the built DLL into `/tmp/jf/plugins/InnerShelf/`, restart, verify plugin loads + a scrape works). If green, bump `targetAbi` in **both** `meta.json` and `build.yaml`, then tag a release.
- **Major (e.g. `10.x` → `11.0`)** — treat as a port branch; major releases typically break plugin APIs.

## License

MIT
