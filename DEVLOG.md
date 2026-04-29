# DEVLOG

Running log of what's been built, what's pending, and *why* particular
architectural choices were made. Read this when picking work back up after
a break — it should let you (or a future Claude session) get oriented
without re-deriving the trail of decisions.

For depth on architecture and the full roadmap see
[`docs/development-plan.md`](docs/development-plan.md). This file is the
operational complement: shorter, dated, and focused on *trail of decisions*
rather than spec.

Newest entries on top.

---

## Now (2026-04-28, evening)

**Status**: **Phase 1 scaffold landed**. `web/` is now a working
Vite + React 19 + TS project with Apollo Client 4, Tailwind v4, Biome,
Vitest, and graphql-codegen wired up. `.github/workflows/web-build.yml`
runs lint + typecheck + test + build on every PR touching `web/**`.
Lockfile `pnpm-lock.yaml` is committed; first sample query
(`findScenes`) is in place but **not yet exercised against a live
Stash** — that's the immediate next validation step.

Local verification gates passed:
- `pnpm typecheck` clean
- `pnpm build` clean (387 KB JS / 7.24 KB CSS gzipped to 118 KB / 2.2 KB)
- `pnpm test` 1/1 passed
- `pnpm lint` clean

**Single source of truth for scope/architecture**:
[`docs/development-plan.md`](docs/development-plan.md). Phase 1
deviations from §3.1 (newer major versions, Apollo 4 import paths)
are noted in the plan and below.

**Reference checkout** (read-only, *not* in this repo):
`C:\Users\Force\GitHub_Repository\stash` — `stashapp/stash` clone for
looking up GraphQL operations and UI patterns. Reference, don't copy
(AGPL-3.0).

**Immediate next step**: bring up Stash via dev compose, generate an
API key, copy `web/.env.example` → `web/.env.local` with the key,
run `pnpm codegen` against live Stash, then `pnpm dev` and verify the
DevTools console shows `[Phase 1] findScenes returned N scenes`. Once
that round-trips, Phase 2 MVP can start.

## Pending

In rough priority order:

- **A. Local-env validation of the Phase 1 scaffold (per-machine, not committed)**
  - Install Docker Desktop / Engine + Compose *(Node 24 + pnpm 10 + Docker
    29 confirmed installed at scaffold time on this dev machine; if you're
    picking this up on a different box redo this gate)*
  - Edit `docker/dev/docker-compose.yml`: replace
    `/path/to/your/test/media` with the host path to your test media
    directory (the `FIXME` comment marks the spot)
  - `docker compose up -d` from `docker/dev/`; walk through Stash's
    first-run wizard at `http://localhost:9999` (set username/password,
    generate API key, point library at `/data`)
  - Copy `web/.env.example` → `web/.env.local`, paste the API key into
    `VITE_STASH_API_KEY`
  - `cd web && pnpm codegen` to materialize `src/gql/` from the live
    Stash schema (output is gitignored, regenerate per machine until we
    snapshot a stable schema)
  - `pnpm dev` and confirm DevTools console prints
    `[Phase 1] findScenes returned N scenes` plus a non-empty list in
    the page body. This is the gate from "Phase 1 scaffold compiles" to
    "Phase 1 actually wired to Stash"
  - Optional: open the GraphQL Playground at
    `http://localhost:9999/playground` and skim the core types
    (`Scene`, `Performer`, `Studio`, `Group`, `Tag`) plus operations
    (`findScenes`, `sceneUpdate`)

- **B. Phase 2 — MVP pages (4-6 week target)**
  - Login / API-key page (key → localStorage → Apollo headers)
  - Scenes list (grid + filter sidebar + virtual scroll)
  - Scene detail (Vidstack player + metadata edit + 同番 version switch)
  - Performer / Studio / Tag list+detail
  - Global Cmd+K search

  Settings / scan config / scraper management are *deliberately not in
  MVP* — those pages stay on Stash's native UI at `/admin/`.

- **Phase 3+** (JAV-specific UX, sidecar integration, AI service,
  production deploy): see dev-plan §4.

## Architectural decisions

These are the load-bearing whys for the post-pivot design. Fuller
treatment (with tradeoff tables) lives in `docs/development-plan.md` §2.4
and §3.1.1 — recorded here in shorter form so future sessions can pick
up the trail.

### Why React 19 + Apollo 4, deviating from plan's 18 + 3 — 2026-04-28

`docs/development-plan.md` §3.1 was authored conservatively (React 18 /
Apollo 3 / Vite 5+ / TS 5). When `pnpm add` resolved against the npm
registry's `latest` tag for these packages, it picked: React 19.2,
Apollo Client 4.1, Vite 8.0, Vitest 4.1, TypeScript 6.0. All of these
are 12+ months stable in the broader ecosystem as of April 2026 and
fully supported by the rest of the stack we picked.

Decision: **adopt the resolved versions, update plan §3.1 to match,
note the deviation here.** Forcing `react@^18` / `@apollo/client@^3`
just to match a paragraph in a doc would mean either (a) carrying
year-old majors against a fresh ecosystem, or (b) updating the doc
anyway in a few weeks. The doc lost the race; fix the doc.

One concrete consequence to remember: **Apollo Client 4 split its
public API into subpath exports**:
- `ApolloClient`, `HttpLink`, `InMemoryCache`, `split`, `gql` — root
  `@apollo/client`
- `ApolloProvider`, `useQuery`, `useMutation`, etc. — `@apollo/client/react`
- `MockedProvider` — `@apollo/client/testing/react` (was `@apollo/client/testing` in v3)
- `getMainDefinition` — `@apollo/client/utilities` (unchanged)
- `GraphQLWsLink` — `@apollo/client/link/subscriptions` (unchanged)

`@apollo/client/v4-migration` is a runtime helper for codemods if we
ever pull in v3 code from elsewhere.

`@vidstack/react@0.6.x` still pins `react@^18` in its peer deps, so
pnpm logs a warning. Phase 1 doesn't actually use Vidstack — it's
installed for Phase 2's player. Verify a newer version works (or pick
a different player) when Phase 2 wires the player up.

### Why pivot from Jellyfin plugin to Stash frontend — 2026-04-28

Jellyfin's data model (Movie / Series / Episode) and UI ceiling never
matched JAV-specific organization: 品番 as primary identity, 同番 as
multi-version aggregation, 女优 as first-class entity, maker → label as
two-level studio hierarchy. Stash's data model (Scene / Performer /
Studio / Group / Tag / Gallery / Image) maps cleanly onto these. User
tried Stash and disliked its UI/UX but agreed the *data model* was right.
Conclusion: keep Stash's backend, replace its frontend.

Tradeoff accepted: third-party-client compatibility (Infuse / Swiftfin
/ AppleTV via Jellyfin protocol) was explicitly dropped as not a hard
requirement. If that ever flips, the path back is restoring the Jellyfin
plugin from `Lynthar/InnerShelf-jellyfin`, possibly fronting a Stash
metadata source.

### Why vanilla Stash, not a fork — 2026-04-28

Two reasons, either alone sufficient:

1. **AGPL-3.0 traversal**. Forking or in-process patching of Stash
   propagates the AGPL to everything in the same process / fork,
   including the frontend. Independent client over GraphQL is *network*
   client, not derivative work — frontend can be MIT (or whatever).
2. **Maintenance burden**. Stash is a moving target. Every minor
   release shifts schema and internals. Owning a fork means
   continuous rebases plus inheriting upstream's bugs without
   upstream's test infrastructure.

Hard rule: **never modify Stash itself**. All extension goes through
GraphQL + sidecar HTTP services.

### Why custom frontend, not a fork of `ui/v2.5/` — 2026-04-28

Stash's bundled UI is React 17 + Bootstrap 4 + Formik + React Router 5.
Migrating that stack to current React 18 + Tailwind v4 + TanStack Router
is close in cost to writing new. Add AGPL traversal (a fork forces the
whole frontend AGPL) and continuous upstream rebase, and "fork ui/v2.5"
becomes the worst of both worlds. Restyling wouldn't fix the UX
complaints anyway — the redesign *is* the project.

### Why double-UI (custom + Stash native at `/admin`) — 2026-04-28

Stash ships its own React UI at port 9999 and shares the same SQLite
with anything else hitting GraphQL. Keeping the native UI reachable
behind `/admin/` lets MVP punt all administrative pages — settings,
library scan, scraper/plugin management, install wizard, GraphQL
playground — to Stash's existing UI. Estimated MVP saving: weeks. The
custom frontend only has to do the parts where the user actually wants
something different (browse / detail / edit / search).

### Why HTTP sidecars instead of Stash plugins — 2026-04-28

Confirmed hard constraint from upstream: **Stash plugins cannot extend
GraphQL**. They can only run as YAML hooks or scripts on existing
events. Anything that needs its own API surface (MetaTube metadata
aggregation, subtitle-forge, future AI service) has to be a standalone
HTTP service. Frontend calls each sidecar directly; sidecars write
results back through Stash GraphQL when needed.

### Why MetaTube as the v1 scraper baseline — 2026-04-28

Stash's CommunityScrapers covers very few JAV sites (JavLibrary,
Heyzo, 1pondo) and misses the major ones (JavBus, FANZA/DMM, MGS).
MetaTube (`metatube-community/metatube-server`) covers all of those
behind one HTTP API with built-in caching and token auth. Zero
self-written scraping code at v1 — pure integration.

If MetaTube proves insufficient (Cloudflare, niche sites, finer-grained
multi-version aggregation), the .NET scraping code from the archived
Jellyfin plugin (`Sources/BuiltIn/JavBusSource.cs`,
`CloudflareDetector.cs`, `MetaTubeApiClient.cs`) can be extracted into
a standalone HTTP service and stacked on top of MetaTube. Treat that
as a v1.5+ option, not a v1 commitment — premature extraction is its
own cost.

### Why archive (not delete) the old Jellyfin plugin — 2026-04-28

Existing Jellyfin installs read their update manifest from this repo's
`gh-pages` branch. Deleting that breaks every existing user. Resolution:

- Push the entire history (all commits + tags + `gh-pages`) to a new
  repo `Lynthar/InnerShelf-jellyfin` and archive it there
- Leave this repo's `gh-pages` untouched so manifest URLs keep serving
  the final released version (the release workflow is gone, so the
  manifest is now frozen — that's the intent)
- Reset `main` to Stash-frontend scope: drop all `.NET` code, replace
  README / CLAUDE.md, add `docs/development-plan.md`, scaffold
  `web/` + `docker/dev/`

The user's `gh-pages` keeps the manifest serving; the archive repo
preserves the trail for any future maintainer or for reusing the
scraping code as a service.

## Session log

### 2026-04-28 (pm) — Phase 1 scaffold

**Built**: `web/` is now a working Vite + React 19 + TS app skeleton.

- `package.json` with scripts: `dev` / `build` / `preview` / `typecheck`
  / `lint` / `lint:fix` / `format` / `test` / `test:watch` / `codegen`
  / `codegen:watch`. `packageManager` pins `pnpm@10.33.0`.
- TS project references: `tsconfig.json` (root references) +
  `tsconfig.app.json` (src) + `tsconfig.node.json` (config files).
  Strict everything, `verbatimModuleSyntax`, `erasableSyntaxOnly`.
- `vite.config.ts`: React plugin + Tailwind v4 plugin + Vitest test
  config (jsdom env, globals, setup file).
- `vitest.setup.ts`: imports `@testing-library/jest-dom/vitest` for
  matchers.
- `codegen.ts`: reads `VITE_STASH_URL` / `VITE_STASH_API_KEY` from env,
  writes to `src/gql/`, runs `biome format --write` after generation.
- `biome.json`: 2-space, single quotes, no semis, line-width 100,
  recommended rules. `useIgnoreFile` so it inherits `.gitignore`.
- `index.html`: minimal shell with `#root` and module-script entry.
- `src/main.tsx`: React 19 `createRoot`, `<StrictMode>`, Apollo
  provider, `App`.
- `src/App.tsx`: shows a config-hint when no API key, else fires
  `findScenes` and renders count + first 20 scene IDs/titles.
  `console.log("[Phase 1] findScenes returned ...")` is the literal
  console-print the dev plan asks for.
- `src/apollo.ts`: `HttpLink` for queries/mutations + `GraphQLWsLink`
  for subscriptions, `split` between them by operation kind. Exports
  a `stashConfig` snapshot so components can branch on
  `hasApiKey` without re-reading `import.meta.env`.
- `src/queries/scenes.ts`: raw `gql` template literal for the sanity
  query (typed inline in `App.tsx`). Will swap to codegen-generated
  `graphql` tag once `pnpm codegen` runs.
- `src/index.css`: `@import "tailwindcss"` (v4 zero-config) plus a
  tiny base reset.
- `src/App.test.tsx`: renders `App` inside Apollo's `MockedProvider`
  and asserts the no-key hint shows. (1 test, sanity gate.)
- `.env.example`: documents `VITE_STASH_URL` / `VITE_STASH_API_KEY`
  with a security note that prod must not ship a baked-in key.
- `web/.gitignore`: `src/gql/` and `coverage/` (root `.gitignore`
  already covers `node_modules`, `dist`, `.env*`).

**Decisions**:
- Hand-wrote files instead of running `pnpm create vite . --template
  react-ts`. Equivalent end state, but: avoids interactive prompts in
  bash, lets us shape strict TS / project refs / Vitest in vite.config
  / Tailwind v4 plugin / Biome from the start. Lockfile and
  `node_modules` come from a single explicit `pnpm add` pass.
- **Routing deferred.** `@tanstack/react-router` is installed but not
  wired up. Phase 1 has one page; pulling in file-based routing now
  is overhead. Phase 2's first multi-page commit introduces
  `@tanstack/router-plugin/vite` + `src/routes/`.
- **Codegen output gitignored** (`src/gql/`). Two reasons: (1) we
  don't have a stable Stash schema captured yet — every dev
  regenerates locally; (2) committing now would either lock CI to a
  specific Stash version implicitly, or rot when the dev's local
  Stash drifts. Plan: when we settle on a Stash version per
  release cycle, snapshot the schema (or commit the generated `gql/`)
  so CI can typecheck without a live server. For now CI uses raw
  `gql` template strings, no generated types.
- Apollo's `MockedProvider` no longer takes `addTypename` in v4 — it
  defaults to true. (Removed the prop after typecheck flagged it.)

**Verified end-to-end** (without live Stash):
- `pnpm typecheck` clean
- `pnpm lint` clean (Biome 2.4)
- `pnpm test` 1/1 passed (Vitest 4.1 + jsdom 29)
- `pnpm build` clean — `dist/` 387 KB JS / 7.24 KB CSS (118 KB / 2.18
  KB gzipped)

**Not verified**:
- `pnpm codegen` against real Stash schema — needs Docker + Stash up
- `pnpm dev` actually fetching scenes — same dependency
- E2E (Playwright) — Phase 2 work, not installed yet

**CI**:
- `.github/workflows/web-build.yml` added. Triggers on PRs and
  `main` pushes touching `web/**` or the workflow itself. Runs
  pnpm install (frozen lockfile), lint, typecheck, test, build on
  Ubuntu + Node 22 + pnpm via `pnpm/action-setup@v4`. Concurrency
  cancels superseded runs on the same ref.

### 2026-04-28 — pivot day (`6851674`)

**What landed in the pivot commit**:
- Removed all `.NET` / Jellyfin SDK code, project files, and tests
- Removed `.github/workflows/release.yml` and `build-test.yml` (the
  Phase 0 checklist's "add `paths:` filter to those" task is now moot
  — the workflows are gone)
- Retargeted `.github/dependabot.yml` from `nuget` to `github-actions`
  (monthly)
- Reset `README.md` + `README.zh-CN.md` to a Stash-frontend stub that
  points at the dev plan and at the archive repo for old plugin users
- Replaced `CLAUDE.md` with Stash-frontend orientation: project
  description, hard constraints (vanilla Stash, pinned versions, no
  GraphQL via plugins, no deprecated `Movie` type, no Jellyfin/.NET in
  this repo), reference-checkout location
- Added `docs/development-plan.md` v0.3 — canonical roadmap (sections
  0-7: positioning, design goals, architecture, stack, phases,
  deployment, update strategy, notes/risks/open questions)
- Added `docker/dev/docker-compose.yml` — Stash `v0.31.1` (pinned) +
  MetaTube, with a `FIXME` placeholder for the user's media path
- Added `docker/dev/.gitignore` to keep runtime bind-mounts
  (`stash-config/`, `stash-cache/`, `metatube-config/`, etc.) out of git
- `web/` created with a single `.gitkeep` — Phase 1 will scaffold here
- `.gitignore` already carried Node / Vite / env entries from a prior
  cleanup; no further changes needed

**Archive side** (separate repo, recorded here for completeness):
- `Lynthar/InnerShelf-jellyfin` created with full plugin history,
  release tags, and `gh-pages`
- This repo's `gh-pages` deliberately untouched so existing Jellyfin
  installs keep receiving the final released manifest

**Not done in this commit** (intentional, deferred to Phase 1):
- No `pnpm` / Vite scaffold under `web/`
- No `web-build.yml` workflow (Phase 1)
- No production `docker-compose.yml` (Phase 5)
- No AI service skeleton (Phase 4 / v1.1)

**Open questions still on the board** (dev-plan §7.8):
Chinese product name, frontend license choice (currently MIT on the
repo as a whole), public-release channels, multilingual UI priority,
AI model selection cadence.

---

— *Claude (Opus 4.7), 2026-04-28*
