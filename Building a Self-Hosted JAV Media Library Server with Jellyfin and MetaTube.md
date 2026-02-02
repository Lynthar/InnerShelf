# Building a self-hosted JAV media library server

**Jellyfin paired with MetaTube offers the most practical solution** for a self-hosted JAV media server. This combination provides full open-source licensing, native Docker deployment, excellent client compatibility (including Infuse), and the most comprehensive metadata scraping ecosystem supporting **30+ Japanese AV sources**. For low-spec hardware, direct-play configurations with Intel Quick Sync or Rockchip RK3588 boards deliver the best performance per watt.

## The recommended architecture: Jellyfin + MetaTube

The optimal stack combines **Jellyfin** as the media server backbone with **MetaTube** as the primary metadata engine. This architecture satisfies all core requirements while providing maximum flexibility for custom integrations.

**Jellyfin** (https://github.com/jellyfin/jellyfin, 45.7k stars, GPL-2.0) stands out as the clear choice over alternatives. Unlike Emby's proprietary premium tiers or Plex's cloud dependency, Jellyfin is fully free with no feature locks. Its plugin architecture uses **.NET Standard 2.0 DLLs**, enabling deep customization for metadata providers. The InfuseSync plugin ensures seamless compatibility with Infuse's Direct Mode (introduced in 7.7), while built-in DLNA support covers VLC, Kodi, and other traditional clients.

**MetaTube** (https://github.com/metatube-community/jellyfin-plugin-metatube, ~3,900 stars) operates as a client-server metadata system. The backend API server (metatube-sdk-go) scrapes content and returns structured JSON, while plugins for Jellyfin, Emby, and Plex consume this data. Key capabilities include:

- **30+ provider sources**: JavBus, JavDB, JavLibrary, DMM/FANZA, MGStage, AVBASE, Caribbeancom, HEYZO, 1Pondo, FC2, and more
- **Face detection** for intelligent primary image cropping
- **Auto-translation** of metadata to preferred languages
- **Actor provider integration** with GFriends repository for actress images
- **Configurable source priorities** via environment variables

Deployment requires two containers—the MetaTube server and Jellyfin itself:

```yaml
services:
  metatube-server:
    image: metatube/metatube-server
    ports:
      - "8080:8080"
    restart: unless-stopped
    
  jellyfin:
    image: jellyfin/jellyfin:latest
    ports:
      - "8096:8096"
      - "7359:7359/udp"  # Client discovery
    volumes:
      - ./config:/config
      - ./cache:/cache
      - /path/to/media:/media:ro
    devices:
      - /dev/dri/renderD128:/dev/dri/renderD128  # Intel/AMD HWA
    restart: unless-stopped
```

## JAV metadata ecosystem deep dive

The metadata scraping landscape divides into three tiers: **integrated plugins**, **standalone scrapers**, and **metadata sources**.

### Integrated plugins (install directly into media server)

**MetaTube** remains the most comprehensive option, but alternatives exist for specific needs:

| Plugin | Platform | Stars | Key Strength |
|--------|----------|-------|--------------|
| MetaTube | Jellyfin/Emby/Plex | 3,900 | Most sources, active development |
| Emby.Plugins.JavScraper | Emby/Jellyfin | 3,700 | Native integration, JavBus/JavDB/FC2 |
| PhoenixAdult | Jellyfin | - | Western adult content focus |

**Emby.Plugins.JavScraper** (https://github.com/JavScraper/Emby.Plugins.JavScraper) offers simpler setup as a single DLL file but supports fewer sources. It integrates directly without requiring a separate backend server.

### Standalone scrapers (pre-process files before importing)

For batch processing or when tighter control over metadata is needed, standalone tools generate NFO files that Jellyfin reads automatically:

**JavSP** (https://github.com/Yuukiy/JavSP, 4.8k stars) leads in popularity and active maintenance (updated February 2025). Written in Python, it automatically recognizes video codes from filenames, downloads HD covers, and generates Kodi-compatible NFO files. Multi-threaded fetching handles large libraries efficiently.

**MDCX** (https://github.com/sqzw-x/mdcx, 3.3k stars) provides similar functionality with an active November 2025 update, supporting batch and single-file scraping with subfolder organization.

**Javinizer** (https://github.com/javinizer/Javinizer) offers the most customization through PowerShell scripting. Its Docker image includes a web GUI, and format strings enable highly specific folder structures. Notably, it provides direct Emby/Jellyfin API integration for pushing actress thumbnails without manual intervention.

### Metadata source evaluation

Understanding source characteristics helps optimize scraper configurations:

**DMM/FANZA** is the only source with an official API (v3.0, free with affiliate registration). The `dmm-search3` Python library provides programmatic access. This should be the primary source for accuracy.

**JavBus** and **JavDB** offer comprehensive coverage but require HTML scraping. JavDB needs a `_jdb_session` cookie for FC2 content and may require proxies in certain regions. The community-maintained JavBus API wrapper (https://github.com/ovnrain/javbus-api) provides a self-hosted API layer.

**JavLibrary** implements aggressive Cloudflare protection. Access requires FlareSolverr or manual cookie extraction (`cf_clearance`, session cookies). Its value lies in user ratings and reviews unavailable elsewhere.

**Rate limiting mitigation** strategies across tools include built-in request throttling, proxy support (HTTP/SOCKS), cookie persistence, and fallback source configurations. MetaTube allows disabling problematic sources via environment variables: `MT_MOVIE_PROVIDER_JAVBUS__PRIORITY=0`.

## Intelligent categorization architecture

JAV content organization relies on **structured metadata fields** that all major scrapers capture: actors/actresses, tags/genres, series/labels, studios/makers, directors, and content IDs. The challenge lies in ensuring consistent data across sources.

**Actor management** benefits from the **GFriends repository** (https://github.com/gfriends/gfriends, 2.7k stars), which maintains actress avatar images updated daily. MetaTube integrates GFriends automatically; standalone scrapers like Javinizer can push these images directly to Jellyfin's actor database via API.

**Tag normalization** addresses inconsistent genre names across sources. Javinizer implements this through CSV mapping files that translate source-specific tags to standardized terms. For Jellyfin, the **CustomMetadataDB plugin** (https://github.com/arabcoders/jf-custom-metadata-db) enables custom tag hierarchies.

**Series and studio organization** works best with consistent folder structures. The community convention follows: `[STUDIO]/[SERIES]/CODE-### [TITLE]/CODE-###.mkv`. Tools like JavSP and Javinizer support dynamic folder generation using format strings.

## Subtitle integration with custom Whisper tools

Jellyfin's architecture supports multiple integration patterns for your Whisper+Ollama subtitle tool.

### Webhook-based automation

The **Jellyfin Webhook Plugin** (https://github.com/jellyfin/jellyfin-plugin-webhook) triggers HTTP requests on media events. Configure it to POST to your subtitle service when new items are added:

```
Event: Item Added → POST http://subtitle-service:5000/process
Payload: { "itemId": "{{ItemId}}", "path": "{{Path}}" }
```

**Subgen** (https://github.com/McCloudS/subgen) demonstrates this pattern for Whisper integration specifically. While you have custom tooling, Subgen's architecture—webhook receiver, Whisper processing, SRT generation, metadata update—provides a reference implementation. Key features include batch transcription via `TRANSCRIBE_FOLDERS` environment variable and automatic subtitle attachment to media items.

### NFO sidecar approach

Your tool can generate SRT files alongside media files following Jellyfin's naming convention: `VIDEO.en.srt` or `VIDEO.en.forced.srt`. Enable the NFO saver in library settings, and Jellyfin automatically discovers and indexes these files during library scans.

### Post-processing script integration

For Live TV/DVR workflows (adaptable to general use), Jellyfin supports post-processing scripts that receive file paths as arguments:

```bash
#!/bin/bash
exec > "/logs/$(date +%Y-%m-%d_%H-%M-%S)-subtitle.log" 2>&1
/usr/bin/python3 /scripts/whisper_process.py "$1"
```

Configure in Dashboard → DVR → Recording Post Processing. The script receives the media filepath, enabling direct integration with your Whisper+Ollama pipeline.

## Low-spec hardware optimization

Performance on constrained hardware depends on **avoiding transcoding** through direct-play configurations and selecting appropriate acceleration methods when transcoding is unavoidable.

### Hardware recommendations by tier

**ARM single-board computers**: Most SBCs lack adequate hardware acceleration. The **Rockchip RK3588/RK3588S** stands as the exception—Orange Pi 5 Plus and Rock 5B outperform Intel N100 in transcoding benchmarks (2.86x faster). Raspberry Pi 5 explicitly is not recommended due to poor HWA support.

**Budget x86**: Intel N100/N305 mini PCs with Quick Sync provide the best transcoding quality-to-price ratio. The integrated GPU handles **4+ simultaneous 1080p transcodes** with minimal power draw (~15W TDP).

**Encoder quality hierarchy**: Apple VideoToolbox ≥ Intel QSV ≥ NVIDIA NVENC >>> AMD AMF. AMD's H.264/H.265 encoding produces notably poor quality; only AV1 encoding is acceptable.

### Docker resource optimization

```yaml
services:
  jellyfin:
    image: lscr.io/linuxserver/jellyfin:latest
    volumes:
      - /dev/shm:/transcode  # RAM-based transcoding
      - /ssd/config:/config  # SSD for database
      - /hdd/media:/media:ro
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 2G
        reservations:
          memory: 512M
```

Mount the transcode directory to `/dev/shm` (RAM) when memory permits, or a dedicated SSD partition. Never use spinning disks for transcoding scratch space.

### Database tuning

Jellyfin uses SQLite with configurable locking strategies. For busy servers, edit `database.xml`:

```xml
<LockingBehavior>Optimistic</LockingBehavior>
```

WAL mode (Write-Ahead Logging) enables better concurrent read performance, particularly important during library scans on large collections.

## Client compatibility matrix

| Client | Connection Method | Notes |
|--------|-------------------|-------|
| **Infuse** | Jellyfin API + InfuseSync | Direct Mode (7.7+) or Library Mode; requires InfuseSync plugin for efficient syncing |
| **Kodi** | Jellyfin for Kodi addon | Full library integration with watched status sync |
| **VLC** | DLNA | Requires host network mode in Docker; install jellyfin-plugin-dlna |
| **Web browsers** | Native web UI | No additional configuration needed |
| **iOS/Android apps** | Jellyfin mobile apps | Official apps in app stores |

**Infuse-specific configuration**: Install **InfuseSync** (https://github.com/firecore/InfuseSync) from the plugin catalog. This tracks media changes for delta updates instead of full library rescans, dramatically reducing sync times for large libraries. Infuse displays server metadata directly—it won't fetch its own TMDB data, ensuring your JAV metadata appears correctly.

**DLNA limitations**: DLNA requires Docker host network mode for broadcast discovery. It cannot function remotely (subnet broadcast limitation) and the Base URL setting breaks DLNA—remove it if using DLNA clients.

## Multi-user architecture preparation

Jellyfin's permission model supports future multi-user requirements without architectural changes:

- **Per-user library access**: Assign specific libraries to users
- **Tag-based restrictions**: Block content with specific tags per user
- **Rating-based controls**: Maximum allowed content rating per user
- **Transcoding permissions**: Enable/disable per user to manage server load
- **Access schedules**: Time-based restrictions
- **Remote access controls**: Separate LAN vs remote permissions

Create an "Adult" library from the start. When adding users later, simply exclude this library from their access. The parental control system uses a **block-based model** (default allow, selectively block), so plan tag hierarchies accordingly.

## Recommended implementation sequence

**Phase 1 - Core infrastructure**: Deploy Jellyfin and MetaTube containers. Configure MetaTube server URL in the Jellyfin plugin. Add media library pointing to your content directory.

**Phase 2 - Metadata optimization**: Configure MetaTube provider priorities (DMM first, then JavDB, JavBus as fallbacks). Install GFriends actor images integration. Run initial library scan.

**Phase 3 - Pre-processing pipeline**: Set up JavSP or Javinizer for batch processing of new content. Configure folder structure conventions. Automate with file watchers or cron jobs.

**Phase 4 - Subtitle integration**: Configure Jellyfin webhooks to trigger your Whisper+Ollama service. Implement SRT file generation following Jellyfin naming conventions. Test with sample media.

**Phase 5 - Client deployment**: Install InfuseSync plugin. Configure DLNA if needed (host network mode). Set up remote access via reverse proxy with SSL.

**Phase 6 - User management**: Create additional users with appropriate library restrictions. Configure parental controls and access schedules as needed.

## Key GitHub repositories reference

| Project | URL | Purpose |
|---------|-----|---------|
| Jellyfin | github.com/jellyfin/jellyfin | Media server |
| MetaTube | github.com/metatube-community/jellyfin-plugin-metatube | JAV metadata plugin |
| JavSP | github.com/Yuukiy/JavSP | Standalone scraper |
| MDCX | github.com/sqzw-x/mdcx | Standalone scraper |
| Javinizer | github.com/javinizer/Javinizer | PowerShell scraper |
| GFriends | github.com/gfriends/gfriends | Actress avatars |
| JavScraper | github.com/JavScraper/Emby.Plugins.JavScraper | Emby plugin |
| InfuseSync | github.com/firecore/InfuseSync | Infuse optimization |
| Subgen | github.com/McCloudS/subgen | Whisper integration reference |
| JavBus API | github.com/ovnrain/javbus-api | Self-hosted metadata API |

This architecture scales from single-user deployments on Raspberry Pi alternatives to multi-user servers with hardware transcoding, while maintaining the flexibility to integrate custom tooling through standardized APIs and file conventions.