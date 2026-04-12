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

## Installation

### From Plugin Repository (Recommended)

1. In Jellyfin Dashboard, go to **Administration → Plugins → Repositories**
2. Add the InnerShelf plugin repository URL (coming soon)
3. Install **InnerShelf** from the catalog
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
| Enable FANZA | Use FANZA/DMM as a metadata source | On |
| MetaTube Server URL | Connect to a MetaTube backend (leave empty to disable) | Empty |
| Title Template | Display title format (`{code}`, `{title}`) | `{code} {title}` |
| HTTP Proxy | Proxy for metadata requests | Empty |

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
│   ├── BuiltIn/     # JavBus, FANZA scrapers
│   └── MetaTube/    # Optional MetaTube backend connector
├── Providers/       # Jellyfin metadata & image providers
├── Mapping/         # Internal models → Jellyfin types
├── ExternalIds/     # Product code as Jellyfin external ID
└── Configuration/   # Plugin settings & web UI
```

## License

MIT
