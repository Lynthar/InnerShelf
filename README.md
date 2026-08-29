<div align="center">

<img src="InnerShelf.png" alt="InnerShelf" width="96">

# InnerShelf

[![license](https://img.shields.io/github/license/Lynthar/InnerShelf)](LICENSE)

</div>

Custom React frontend for a vanilla Stash backend, aimed at JAV libraries. Early scaffold — not usable yet.

English | [简体中文](README.zh-CN.md)

> **Under construction, and early.** Right now this repository is a React
> scaffold that runs one sanity query against a Stash instance and lists the
> results. There is no UI to speak of, no router, no player, nothing to install,
> and no release. Watch it if the architecture below sounds interesting; don't
> expect to run it.

This is a browsing frontend for an adult media library, sitting on top of an
**unmodified Stash** installation. Stash keeps the library, the scanner and the
scrapers; this side handles browsing, detail views and editing, the way a JAV
library actually wants them: code as the primary key, one title in several
versions, performers as their own first-level entity.

## Architecture

**Stash stays vanilla.** No fork, no in-process patches. Everything here talks to
it over its GraphQL API, which also means Stash's AGPL-3.0 stays on Stash's side
of the line and this frontend can be MIT.

**Two UIs, one database.** Settings, scanning, scrapers and plugin management
stay in Stash's own interface. This frontend is meant to own browsing, detail
views, editing and search — the parts you interact with day to day.

**Extensions are sidecars.** Metadata aggregation and subtitle generation are
separate HTTP services rather than code in this repository, because Stash
plugins can't extend its GraphQL schema.

## Status

What exists today, in full: a React app of four source files, an Apollo client, a
`findScenes` query, and a list of the first twenty results.

Not implemented: the UI, routing, the video player, any of the sidecars, and the
backend — which is a third-party container, not code here. CI is currently
failing on the pnpm setup step. Third-party Jellyfin clients such as Infuse or
Swiftfin won't be supported; that was accepted when this stopped being a Jellyfin
plugin.

## Development

There is no installation method yet. This is how you'd run the scaffold. You
need Docker, Node 22, and pnpm via corepack.

```bash
git clone https://github.com/Lynthar/InnerShelf.git
cd InnerShelf
```

Point `docker/dev/docker-compose.yml` at a real media directory, then:

```bash
cd docker/dev && docker compose up -d
```

That brings up Stash on `:9999` and a metadata service on `:8080`. Open Stash,
walk its first-run wizard, and generate an API key.

```bash
cd ../../web
pnpm install
cp .env.example .env.local     # fill in VITE_STASH_API_KEY
pnpm codegen                   # introspects the running Stash for types
pnpm dev
```

It worked if the console prints how many scenes came back.

Gates: `pnpm lint`, `pnpm typecheck`, `pnpm test`, `pnpm build`.

## Configuration

Two variables in `web/.env.local`:

| Key | Default |
|---|---|
| `VITE_STASH_URL` | `http://localhost:9999` |
| `VITE_STASH_API_KEY` | — |

The API key here is for local development only. Vite bakes it into the bundle,
so a real deployment would need to collect it at runtime instead; that isn't
built yet.

## Roadmap

The order here was reversed deliberately: the backend stack comes first, and the
frontend gets redesigned around what one person actually needs.

- Run the backend properly — Stash plus the metadata service, on real hardware,
  with pinned image versions — and use Stash's own UI in the meantime.
- Write a scraper configuration pointing at the metadata service, so Stash's own
  scrape button gets multi-source metadata. No frontend code required.
- Generate subtitles out of band, dropping SRT files next to the videos for
  Stash to pick up on its next scan.
- Then a single frontend page: a code-centric grid plus a detail panel that can
  play, tag, rename, delete, scrape and subtitle. **File operations are the
  point** — `moveFiles` and `deleteFiles` are fully supported in Stash's API but
  its own UI never surfaced them.

## Looking for the Jellyfin plugin?

This repository used to be a Jellyfin plugin. That work has stopped. The last
release was v0.1.3 in April 2026, and the manifest on the `gh-pages` branch is
frozen there — installed plugins will keep resolving it, but there won't be new
versions or compatibility fixes for newer Jellyfin releases. The full source
history lives at
[Lynthar/InnerShelf-jellyfin](https://github.com/Lynthar/InnerShelf-jellyfin).

The releases listed on this repository are from that era and have nothing to do
with the current code.

## License

MIT — see [LICENSE](LICENSE). Copyright (c) 2026 Lynthar.

[Stash](https://github.com/stashapp/stash) is AGPL-3.0 and is used unmodified,
over the network, as a separate program.
