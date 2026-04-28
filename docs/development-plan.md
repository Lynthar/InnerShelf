# InnerShelf 开发计划

> 本文档基于 2026-04-27 对 Stash 项目（截至 v0.31.1）的研究撰写，2026-04-28 增补。
> 适用对象：本仓库的 **InnerShelf**——基于 Stash 后端的自定义前端。
> 路线已确认：**vanilla Stash 后端 + 自写 React 前端 + 多个 HTTP 侧车（MetaTube / subtitle-forge / AI Service）**。

## 目录

0. [项目定位与命名](#0-项目定位与命名)
1. [设计目标](#1-设计目标)
2. [架构总览](#2-架构总览)
3. [技术选型](#3-技术选型)
4. [开发步骤](#4-开发步骤)
5. [安装与部署](#5-安装与部署)
6. [更新策略](#6-更新策略)
7. [其他注意事项](#7-其他注意事项)

---

## 0. 项目定位与命名

InnerShelf 是基于 Stash 后端的自定义 React 前端 + HTTP 侧车（MetaTube / subtitle-forge / 未来的 AI Service）。仓库 URL、品牌、icon 沿用现有 InnerShelf；版本号 `vX.Y.Z`，Docker tag 同步。

> **历史沿革**：本仓库 2026-04-28 之前曾作为 InnerShelf Jellyfin 插件主仓，是 JAV metadata 管理的过渡方案。Pivot 后插件代码已迁移至 [`Lynthar/InnerShelf-jellyfin`](https://github.com/Lynthar/InnerShelf-jellyfin)（归档、停止主动开发；最后一次发布的 manifest 仍保留在原仓库 `gh-pages` 分支供已有用户安装）。本仓库专注 Stash 前端方向。

---

## 1. 设计目标

### 1.1 核心目标

1. 提供**专门针对成人/色情视频（特别是 JAV）**优化的自托管媒体管理 Web 应用
2. 数据模型一等公民：**Scene / Performer（女优）/ Studio（厂牌）/ Group（系列）/ Tag**——围绕这些做深度交互而非泛通用化
3. 完善的标签 + 评分体系，多维筛选与搜索
4. 元数据抓取专为成人内容场景调优（JavBus、MetaTube、JavLibrary、Heyzo、Caribbean、1pondo 等）
5. 标准化外部工具接入：
   - **字幕生成**（已有 subtitle-forge GPU 服务复用）
   - **AI 智能识别和分类**（语音转写、视觉分类、人物识别等）
6. **Docker 优先**，同时支持 Linux/Windows/macOS 原生运行

### 1.2 用户场景

- 单用户家庭/个人媒体库，规模 1k-50k 视频
- 内网或反向代理后访问
- 高频操作：批量打标签、批量编辑、智能识别、字幕生成
- 在自己最熟悉/喜欢的 UI 中浏览、过滤、播放

### 1.3 非目标（Out of Scope）

- 多用户/多账户系统（Stash 后端不支持，前置网关代价大于收益——v1 不做）
- 移动客户端原生 app（Web + PWA 优先，原生 app 是 v2+ 议题）
- 通用媒体管理（电影、剧集、音乐）——这些场景 Jellyfin/Plex 已经做得好
- 与 Plex/Emby/Jellyfin 客户端协议兼容（明确放弃 Infuse 等第三方播放器接入；要兼容时改回 Jellyfin 插件路线）

---

## 2. 架构总览

### 2.1 系统拓扑

```
┌────────────────────────────────────────────────────────────────┐
│                      浏览器（用户）                              │
│   日常浏览/详情/编辑 → 自定义前端                                 │
│   管理类操作         → Stash 自带 UI（同后端，路径或端口分流）    │
└────────────────────────────────┬───────────────────────────────┘
                                 │ HTTP(S)
                ┌────────────────▼───────────────┐
                │  Reverse Proxy (Caddy/nginx)   │ ← 可选；v1 默认
                │  - HTTPS 终止                   │   可由前端 nginx
                │  - 路径分发                     │   自反代代替
                └──────┬───────────────┬─────────┘
                       │               │
            ┌──────────▼─┐       ┌─────▼──────────────┐
            │  Frontend   │       │   Stash Backend     │
            │  (静态 SPA) │       │   (vanilla)         │
            │  nginx 服务 │       │   - GraphQL API     │
            │             │       │   - 自带 Web UI     │ ← 双 UI 共存
            └──────┬──────┘       │   - 媒体流 / 转码    │
                   │ GraphQL      │   - SQLite          │
                   └──────────────│   Go                │
                                  └─┬────┬────┬────────┘
                                    │    │    │
        ┌───────────────────────────┴─┬──┴────┴───────────────────┐
        │                              │                            │
  ┌─────▼────────┐  ┌──────────────────▼───┐  ┌──────────────────▼──┐
  │ subtitle-    │  │   MetaTube            │  │   AI Service         │
  │ forge        │  │  (元数据聚合 API)      │  │ (Python/FastAPI,     │
  │ (GPU,已有)   │  │   多 JAV 站点抓取      │  │  v1.1)               │
  │              │  │   独立 HTTP 服务       │  │  Whisper/CLIP/       │
  │              │  │                       │  │  Face recognition    │
  └──────────────┘  └───────────────────────┘  └──────────────────────┘
          ▲                    ▲                         ▲
          └────────────────────┴─────────────────────────┘
                               │ HTTP
       Frontend 直调 / Stash plugin 触发 / 侧车互相调用 / 写回 Stash GraphQL

  v1.5+（可选）：InnerShelf Scraper Service —— 把归档插件
                 (Lynthar/InnerShelf-jellyfin) 内的 .NET 抓取代码
                 （JavBus + Cloudflare 处理 + 多源 fallback）抽成独立
                 HTTP 服务，叠在 MetaTube 之上做高级 fallback。
```

### 2.2 各组件职责

| 组件 | 职责 | 技术 | 是否自研 |
|--|--|--|--|
| Frontend | 自定义 UI、路由、用户交互、调度 | React + TS + Vite + Apollo | ✓ 自研 |
| Stash Backend | 数据存储、抓取调度、媒体扫描、转码、**GraphQL API + 自带 Web UI** | Go (vanilla) | ✗ 不动 |
| 反向代理 | HTTPS、路径分发、CORS 兜底 | Caddy 或 nginx（可选） | 配置 |
| **MetaTube** | JAV 元数据聚合 API（多源统一抓取） | Go (`metatube-community/metatube-server`) | ✗ 第三方 |
| subtitle-forge | 字幕生成 | 已有 GPU 服务 | ✗ 已有 |
| AI Service | 视觉/音频分类、人物识别 | Python FastAPI + ML 模型 | ✓ 自研（v1.1） |
| Stash Plugins | 后端钩子 / 任务触发（少量） | YAML + Python/JS | ✓ 少量自研 |
| **InnerShelf Scraper Service**（可选 v1.5+） | 从归档插件移植 JavBus + Cloudflare 处理代码到独立 HTTP 服务 | .NET / Python | ✓ 抽取自归档插件 |

> **关于 Stash 自带 Web UI**：Stash 镜像里同时打包了它的原生前端，访问 :9999 直接就能用。**这是有意保留的**——MVP 阶段所有"管理类"页面（设置、扫描配置、Scraper/Plugin 管理、首次安装向导）**全部跳过自研**，让用户跳路径或换端口去用 Stash 自带 UI。两个 UI 共享同一份 SQLite 数据，互相能立即看到对方的修改。

### 2.3 关键数据流

**场景 A：浏览 Scene 列表**
1. Frontend → GraphQL `findScenes(filter, page)` → Stash
2. Stash 查 SQLite，返回给 Frontend 渲染网格
3. 缩略图 URL 形如 `/scene/{id}/screenshot`，浏览器直接拉

**场景 B：编辑 Scene 标签**
1. Frontend → GraphQL `sceneUpdate(input)` → Stash
2. Stash 更新 DB，Apollo cache 同步本地

**场景 C：触发字幕生成**
1. Frontend → 直接 POST 到 subtitle-forge HTTP（不经 Stash），传该 Scene 的视频路径
2. subtitle-forge 完成后写 sidecar SRT 到视频旁
3. 下次 Stash 扫描或刷新该 Scene 自动识别字幕

**场景 D：AI 智能识别（v1.1）**
1. Frontend → POST 到 AI Service 提交分类任务（Scene id + path）
2. AI Service 处理后，调 Stash GraphQL `sceneUpdate` 写回标签/演员
3. Frontend 通过 Stash GraphQL 订阅 `jobsSubscribe` 看 Stash 自家 task 进度；AI 任务进度走 AI Service 自己的 SSE

**场景 E：通过 MetaTube 抓取/补全元数据**
1. Frontend → POST 到 MetaTube `GET /v1/movies/{code}` —— MetaTube 内部多源聚合（JavBus / FANZA / MGS / JavLibrary 等）返回统一 JSON
2. Frontend 拿到结果 → 通过 Stash GraphQL `sceneUpdate` 把 title / cover / performers / tags / studio / release_date 等写回到 Scene
3. 备选路径：用户在 Stash 自带 UI 用 YAML scraper（指向 MetaTube API）触发刮削；适合批量场景

### 2.4 关键架构决策

| 决策 | 选择 | 理由 |
|--|--|--|
| 后端是否自研？ | **否，用 Stash vanilla** | 转码/扫描/抓取调度/SQLite 已经成熟，自研代价高且无差异化 |
| 前后端通信？ | **GraphQL only** | Stash 主接口；类型安全 + Codegen 工具成熟；订阅天然支持 |
| AI / 字幕 / 元数据如何集成？ | **独立 sidecar HTTP 服务** | **关键发现：Stash plugin 不能扩展 GraphQL**，sidecar 模式最灵活；subtitle-forge / MetaTube / AI Service 都按这个模式接 |
| 前端要不要 fork `ui/v2.5/`？ | **不 fork，从零写** | Stash UI 栈偏老（React 17 / Bootstrap 4 / Formik / RR5）；fork 即 AGPL 强制传染；upstream rebase 长期不可控；本来就是为了换 UI——改样式 ≠ 改体验 |
| 要不要把前端塞进 Stash 镜像（"单容器派生"）？ | **不要，独立容器** | 省一个容器的微小收益 ≪ 维护边界扩大代价（你会变成事实上的 Stash 维护者，每季度 rebase 上游） |
| Stash 自带 Web UI 怎么办？ | **保留，作为管理后台** | 设置/扫描/Scraper 管理/Plugin 管理/首次安装向导全部由它承担，MVP 不重写这些页面 |
| 多用户支持？ | **不支持** | Stash 后端单用户；前置网关代价大于收益 |
| Stash plugin 用不用？ | **少量用** | 仅在需要 hook（如 Scene 创建后自动触发字幕生成）时；UI 走自研前端 |
| 反向代理是否必需？ | **不必需** | v1 默认让前端 nginx 自反代后端（一个对外端口、URL 同源、零 CORS 问题）；公网/HTTPS 时再加 Caddy |
| 何时锁版本？ | **生产严格锁定 `v0.X.Y`** | Stash 版本 API 稳定但每个 minor 都可能动 schema |

---

## 3. 技术选型

### 3.1 前端

| 层 | 选择 | 版本 | 备选 |
|--|--|--|--|
| 框架 | **React** | 18.x | Vue 3 / Svelte（已论证 React 最优） |
| 语言 | **TypeScript** | 5.x | — |
| 构建 | **Vite** | 5+ | — |
| 路由 | **TanStack Router** | latest | react-router 6 |
| GraphQL 客户端 | **Apollo Client** | 3.x | urql |
| 类型生成 | **graphql-codegen** + `client-preset` | latest | — |
| 样式 | **Tailwind v4** + **shadcn/ui** | — | Mantine（落选：定制性差） |
| 视频播放 | **Vidstack** | latest | video.js（落选：更老） |
| 表单 | **react-hook-form** + **zod** | — | — |
| 列表虚拟化 | **@tanstack/react-virtual** | — | — |
| 轻量状态 | **Zustand** | — | jotai |
| WebSocket 订阅 | **graphql-ws** + Apollo subscriptions link | — | — |
| 包管理 | **pnpm** | 9+ | — |
| Lint/Format | **Biome** | — | ESLint+Prettier |
| 测试 | **Vitest** + **Testing Library** | — | — |
| E2E | **Playwright** | — | — |

> 注：Stash 自家 UI 也是 React + Vite + Apollo + pnpm（不过它锁在 React 17）。我们直接用 React 18，独立构建，跟 Stash UI 完全解耦。

### 3.1.1 前端策略：从零写，不 fork，双 UI 共存

**为什么不 fork `ui/v2.5/`**：

- 栈偏老（React 17 / Bootstrap 4 / Formik / React Router 5），换栈成本接近重写
- AGPL-3.0 强制传染——fork 后整个前端必须 AGPL 公开
- 长期 rebase 维护负担不可控
- 你不喜欢的那个 UI 还在——改样式 ≠ 改体验

**为什么不塞进 Stash 镜像（单容器派生）**：

- 维护边界从"自己的前端"扩到"Stash 全栈"
- 你成为事实上的 Stash 维护者，每季度 rebase 上游
- 失去独立演进能力（栈/许可证/发布节奏全部被绑死）

**Stash 自带 Web UI 怎么用**：

| 你做什么 | 用哪个 UI |
|--|--|
| 浏览/搜索/播放 | 自定义前端 |
| 编辑标签/评分/演员/标题 | 自定义前端 |
| 触发字幕生成 / AI 分类 / MetaTube 抓取 | 自定义前端 |
| 配置媒体库路径 / 扫描周期 | **Stash 自带 :9999** |
| 装/卸 scraper / plugin | **Stash 自带 :9999** |
| 管理 API Key、用户密码 | **Stash 自带 :9999** |
| 首次安装向导 | **Stash 自带 :9999** |
| GraphQL Playground 调试 | **Stash 自带 :9999/playground** |

两个 UI 共享同一个 SQLite，互相能立即看到对方的修改——MVP 因此**砍掉一大块"管理后台"工作量**，只做差异化部分。

**参考但不复制**：开发时 clone 一份 `stashapp/stash` 到本地（不放进自己 repo），需要写某个 GraphQL 查询、播放器集成、字幕轨切换时去翻 `ui/v2.5/src/components/` 和 `ui/v2.5/graphql/` 找参考实现，**自己重写**。这样既不背 AGPL 又能复用别人的探索成果。

### 3.2 后端（不开发，仅集成）

| 项 | 值 | 备注 |
|--|--|--|
| 项目 | Stash | AGPL-3.0 |
| 部署 | Docker `stashapp/stash:vX.Y.Z` | 生产**不要**用 `latest` |
| 数据库 | SQLite | 不可换 Postgres；50k+ 规模仍 OK |
| GraphQL 端点 | `/graphql` | HTTP + WebSocket 同路径 |
| Playground | `/playground`（默认开） | 生产用反代过滤 |
| 认证 | API Key | HTTP header：`ApiKey: <jwt>`（注意大小写） |
| 默认端口 | 9999 | |

### 3.3 外部服务 / 侧车

所有外部能力（元数据、字幕、AI）都按统一的"独立 HTTP 服务"模式接入。这是因为 Stash plugin **不能扩展 GraphQL**——硬性约束决定的形态。

| 角色 | 选择 | 备注 |
|--|--|--|
| **元数据聚合** | **MetaTube**（`metatube-community/metatube-server`） | 独立 Go 服务；多 JAV 站点统一 API；自带 SQLite 缓存和 token 鉴权；前端直调或 Stash YAML scraper 调；**v1 起步就部署** |
| 字幕生成 | subtitle-forge（已有 GPU 服务） | 复用；前端直接 POST，写回 sidecar SRT 到 `/data` |
| AI 服务运行时 | Python 3.11 + FastAPI + Uvicorn | v1.1 起 |
| 音频转写 | faster-whisper | 比 OpenAI Whisper 快几倍 |
| 视觉分类 | CLIP / 自训练分类器 | 视频抽帧 → 分类 |
| 人物识别 | InsightFace / ArcFace | 比对 Performer 头像 |
| 部署 | 各服务独立 Docker 容器；AI/字幕容器挂 `/data` volume（只读） | 可选 GPU |
| **InnerShelf Scraper Service**（v1.5+ 可选） | 从归档仓库 `Lynthar/InnerShelf-jellyfin` 拿 `Sources/BuiltIn/JavBusSource.cs` + `CloudflareDetector.cs` + `MetaTubeApiClient.cs` 抽成独立 .NET HTTP 服务 | 在 MetaTube 之上提供更高级别的多源 fallback、Cloudflare 处理、版本聚合；如果未来恢复 Jellyfin 端开发，归档插件也可复用同一个服务 |

### 3.4 仓库与基础设施

| 项 | 选择 |
|--|--|
| Git 仓库 | 当前 InnerShelf（不另开） |
| CI | GitHub Actions |
| 容器仓库 | GitHub Container Registry (ghcr.io) |
| 反向代理 | **Caddy**（推荐，自动 HTTPS）或 nginx |
| 备份 | cron + tar + 本地磁盘/远端对象存储 |

---

## 4. 开发步骤

### Phase 0：环境准备 + 仓库结构（1 周）

- [ ] 装 Docker Desktop / Docker Engine + Compose
- [ ] 跑起 Stash 容器（详见 §5.1），扫一个测试目录
- [ ] 浏览 GraphQL Playground（`http://localhost:9999/playground`），翻一遍核心 query/mutation
- [ ] 仓库结构调整：
  - 新增 `web/`、`docker/`、`docs/`（已存在）
  - `.gitignore` 加 Node 条目
  - 给现有 `release.yml` / `build-test.yml` 加 `paths:` 过滤，避免改 `web/` 触发 .NET 构建
- [ ] 装 Node ≥ 20 和 pnpm（`corepack enable && corepack prepare pnpm@latest --activate`）

### Phase 1：脚手架（1 周）

- [ ] `cd web && pnpm create vite@latest . -- --template react-ts`
- [ ] 装核心依赖：
  ```
  pnpm add @apollo/client graphql graphql-ws
  pnpm add -D @graphql-codegen/cli @graphql-codegen/client-preset
  pnpm add -D tailwindcss@next @tailwindcss/vite
  pnpm add @tanstack/react-router @tanstack/react-virtual
  pnpm add @vidstack/react
  pnpm add react-hook-form zod
  pnpm add zustand
  pnpm add -D @biomejs/biome
  pnpm add -D vitest @testing-library/react
  ```
- [ ] 配置 `codegen.ts`：`schema: { 'http://localhost:9999/graphql': { headers: { ApiKey: '...' } } }`
- [ ] 第一个查询：`findScenes(filter: {}, filter_pagination: { per_page: 20 })`，前端能在 console 打印结果
- [ ] CI: 加 `web-build.yml`（pnpm install / typecheck / build）

### Phase 2：MVP（4-6 周）

按优先级实现：

1. **登录 / API Key 配置页** — key 持久化到 localStorage，挂到 Apollo `headers`
2. **Scenes 列表页** — 网格 + 筛选侧栏（关键字、Performer、Studio、Tag、评分）+ 排序 + 分页 + 虚拟滚动
3. **Scene 详情页** — Vidstack 播放器 + 元数据 + 标签编辑 + 同番版本切换
4. **Performer 列表 + 详情** — 出演列表、合作演员、统计
5. **Studio 列表 + 详情** — 该厂牌作品
6. **Tag 列表 + 详情** — 该 tag 下的 Scenes
7. **全局搜索**（命令面板风格 Cmd+K）

**MVP 不做的**：设置/扫描/Scraper 管理（先用 Stash 自带页面顶替——用菜单链接跳到 9999）、用户管理、批量操作、Markers、Galleries、Images。

### Phase 3：JAV 专属优化（3-4 周）

按 Stash 项目 [Issue #3065](https://github.com/stashapp/stash/issues/3065)（"Make Stash more suitable for JAV" RFC）思路：

1. **品番为核心标识**——详情页大字号显示，列表页 hover 时品番替代标题
2. **同番多版本聚合**——用 Stash **Group** 关联同一品番的不同版本（中字/无码流出/4K/Hack），详情页加切换条
3. **女优主页强化**——多语言名称（日文+罗马字+中文）、近期作品时间线、合作矩阵、出道至今统计
4. **厂牌-Label 二级关系**——用 Studio 的 `parent_studio` 表达 maker → label
5. **多 URL 支持**——Stash 的 `URLs []` 已有，前端展示完善

### Phase 4：外部服务集成（4-6 周）

> 注：MetaTube 集成可以**前移到 Phase 2 中后期**，因为是 v1 起步刚需的元数据来源；这里把它和字幕、AI 一起列以保持"外部服务集成"分类完整。

1. **MetaTube 元数据集成（v1 必需）**
   - 部署 MetaTube 容器（Compose 已包含）
   - 前端在 Scene 详情页加"从 MetaTube 抓取/补全"按钮 → POST 到 `metatube:8080/v1/movies/{code}` → 拿到结果通过 Stash GraphQL `sceneUpdate` 写回
   - 批量入库时也调 MetaTube（按品番自动）
   - 多源 fallback 逻辑暂在前端做（v1.5 抽到独立服务）
2. **subtitle-forge 集成（v1 必需）**
   - Frontend 直接 POST 到 subtitle-forge（无需 Stash 中转）
   - 任务进度通过 SSE 或轮询
   - 完成后 sidecar SRT 自动落到 `/data` 下
   - 可选：加一个 Stash plugin（hook `Scene.Create.Post`）自动为新 Scene 触发字幕生成
3. **AI Service 骨架（v1.1）**
   - 独立 Python 项目 `ai-service/`
   - FastAPI，Docker 镜像 `ghcr.io/<owner>/innershelf-ai`
   - 第一个端点：`POST /classify/scene` 接受 `scene_id` + 文件路径
   - 处理后通过 Stash GraphQL `sceneUpdate` 写回 tags/performers
4. **AI Service 第一个能力**：从音频提取主要语种 + Whisper 转写若干秒预览，作为 description 候选
5. **可选**：人物识别——比对 Performer 的 image，回写到 Scene 的 performers 关联
6. **InnerShelf Scraper Service 抽取（v1.5 可选）**：从归档仓库 `Lynthar/InnerShelf-jellyfin` 拿现成的 .NET 抓取代码（JavBus + Cloudflare + 多源 fallback）抽成独立 HTTP 服务，给 Stash 前端在 MetaTube 之外做备用抓取层

### Phase 5：生产部署 + 文档（2-3 周）

1. 完整的 `docker/production/docker-compose.yml`：Stash + Frontend + AI Service + Caddy
2. 备份脚本：定期 dump SQLite + 配置
3. 用户文档：装机手册、常见问题、配置指南
4. 监控：基础健康检查端点，可选 Grafana

**总计：约 15-20 周（4-5 个月）做完 v1。** AI 部分（Phase 4）可以推迟到 v1.1。

---

## 5. 安装与部署

### 5.1 本地开发

```bash
# 第一步：起 Stash 后端（容器内）
cd docker/dev
docker compose up -d stash

# Stash 首次启动会引导你进 http://localhost:9999 设置：
# - Settings → Library → 加你的测试目录
# - Settings → Security → 设置 username/password 和生成 API Key

# 第二步：起前端 dev server
cd ../../web
pnpm install
pnpm codegen      # 拉 schema 生成 TS 类型
pnpm dev          # http://localhost:3000
```

`web/.env.local`（仅本地开发用）：

```
VITE_STASH_URL=http://localhost:9999
VITE_STASH_API_KEY=<你刚生成的 dev key>
```

> ⚠️ API Key 仅本地开发图方便存到 .env.local；**生产前端**不能内嵌 key，由用户在登录页录入并存浏览器 localStorage。

`docker/dev/docker-compose.yml`：

```yaml
services:
  stash:
    image: stashapp/stash:v0.31.1
    ports: ["9999:9999"]
    environment:
      - STASH_STASH=/data/
      - STASH_GENERATED=/generated/
      - STASH_METADATA=/metadata/
      - STASH_CACHE=/cache/
    volumes:
      - ./stash-config:/root/.stash
      - /path/to/your/test/media:/data
      - ./stash-metadata:/metadata
      - ./stash-cache:/cache
      - ./stash-generated:/generated
      - ./stash-blobs:/blobs

  metatube:
    image: ghcr.io/metatube-community/metatube-server:latest
    ports: ["8080:8080"]
    volumes:
      - ./metatube-config:/config
    environment:
      - TOKEN=dev-token-change-me      # 前端调用时带这个 token
    restart: unless-stopped
```

### 5.2 生产部署

提供两种形态。**v1 默认推荐方案 A**（更简）；公网/HTTPS 时切到方案 B。

#### 方案 A：前端 nginx 自反代（推荐，无 Caddy）

把"反代到后端"的工作交给前端容器里的 nginx——前端镜像本来就是 nginx 跑静态文件，加几条 `location` 反代规则就行，**少一个容器**。

`docker/production/docker-compose.yml`：

```yaml
services:
  stash:
    image: stashapp/stash:v0.31.1   # ⚠️ 锁定版本，不要 latest
    expose: ["9999"]                  # 不对外暴露
    environment:
      - STASH_STASH=/data/
      - STASH_GENERATED=/generated/
      - STASH_METADATA=/metadata/
      - STASH_CACHE=/cache/
      - STASH_PORT=9999
    volumes:
      - /etc/localtime:/etc/localtime:ro
      - ./stash-config:/root/.stash
      - /your/media/path:/data
      - ./stash-metadata:/metadata
      - ./stash-cache:/cache
      - ./stash-blobs:/blobs
      - ./stash-generated:/generated
    restart: unless-stopped

  metatube:
    image: ghcr.io/metatube-community/metatube-server:latest
    expose: ["8080"]                  # 不对外暴露
    volumes:
      - ./metatube-config:/config
    environment:
      - TOKEN=<your-secret-token>     # 强烈建议设
    restart: unless-stopped

  frontend:
    image: ghcr.io/<owner>/innershelf-web:v0.1.0
    ports: ["80:80"]                  # 唯一对外端口
    depends_on: [stash, metatube]
    restart: unless-stopped

  # v1.1 加
  # ai-service:
  #   image: ghcr.io/<owner>/innershelf-ai:v0.1.0
  #   expose: ["8000"]
  #   volumes:
  #     - /your/media/path:/data:ro
  #   deploy:
  #     resources:
  #       reservations:
  #         devices:
  #           - driver: nvidia
  #             count: 1
  #             capabilities: [gpu]
  #   restart: unless-stopped
```

前端镜像里的 `nginx.conf`：

```nginx
server {
    listen 80;

    # 反代到 Stash GraphQL + 媒体路径
    location /graphql        { proxy_pass http://stash:9999; }
    location /scene/         { proxy_pass http://stash:9999; }
    location /performer/     { proxy_pass http://stash:9999; }
    location /studio/        { proxy_pass http://stash:9999; }
    location /tag/           { proxy_pass http://stash:9999; }
    location /image/         { proxy_pass http://stash:9999; }

    # Stash 自带管理 UI（保留入口给管理用）
    location /admin/ {
        proxy_pass http://stash:9999/;
    }

    # 屏蔽 playground
    location /playground { return 404; }

    # MetaTube（前端直调）
    location /metatube/ {
        proxy_pass http://metatube:8080/;
    }

    # AI service（v1.1）
    # location /ai/ {
    #     proxy_pass http://ai-service:8000/;
    # }

    # WebSocket 订阅需要 Upgrade 头
    location = /graphql {
        proxy_pass http://stash:9999;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }

    # 其余走 SPA
    location / {
        root /usr/share/nginx/html;
        try_files $uri /index.html;
    }
}
```

访问：

| URL | 看到什么 |
|--|--|
| `http://192.168.1.10/` | 自定义前端（日常用） |
| `http://192.168.1.10/admin/` | Stash 自带 UI（管理用） |
| `http://192.168.1.10/graphql` | GraphQL 端点（自定义前端通过它取数据） |

**总容器数：3**（Stash + MetaTube + Frontend）。无 Caddy。

#### 方案 B：加 Caddy（公网 / 自动 HTTPS）

公网暴露时切到这套——Caddy 自动签 Let's Encrypt 证书：

```yaml
services:
  caddy:
    image: caddy:2-alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./Caddyfile:/etc/caddy/Caddyfile
      - caddy_data:/data
    restart: unless-stopped

  stash: # 同方案 A，仅把 ports 换成 expose
    image: stashapp/stash:v0.31.1
    expose: ["9999"]
    # ...

  metatube:
    image: ghcr.io/metatube-community/metatube-server:latest
    expose: ["8080"]
    # ...

  frontend:
    image: ghcr.io/<owner>/innershelf-web:v0.1.0
    expose: ["80"]                    # 不对外，由 Caddy 反代
    # ...

volumes:
  caddy_data:
```

`Caddyfile`：

```caddy
media.your-domain.com {
    # Stash GraphQL 和媒体路径
    handle /graphql* { reverse_proxy stash:9999 }
    handle /scene/*       { reverse_proxy stash:9999 }
    handle /performer/*   { reverse_proxy stash:9999 }
    handle /studio/*      { reverse_proxy stash:9999 }
    handle /tag/*         { reverse_proxy stash:9999 }
    handle /image/*       { reverse_proxy stash:9999 }

    # Stash 自带管理 UI
    handle /admin/* {
        uri strip_prefix /admin
        reverse_proxy stash:9999
    }

    # MetaTube（前端调）
    handle /metatube/* {
        uri strip_prefix /metatube
        reverse_proxy metatube:8080
    }

    # AI service（v1.1）
    # handle /ai/* {
    #     uri strip_prefix /ai
    #     reverse_proxy ai-service:8000
    # }

    # 屏蔽 playground
    handle /playground* { respond 404 }

    # 其余走前端 SPA
    handle {
        reverse_proxy frontend:80
    }
}
```

**总容器数：4**（多一个 Caddy）。`docker compose up -d`，Caddy 自动签证书。

#### 选哪个

| 场景 | 选方案 |
|--|--|
| 内网/LAN 用、HTTP 够 | **A** |
| 用 Tailscale / VPN 接入 | **A** |
| 公网暴露、要 HTTPS | **B** |
| 多个子域名/服务复用 80/443 | **B** |
| 用现有外部 Caddy（如 Debian NAS 上已有的）| 把 Stash + 前端 + MetaTube 都 expose 到 docker network，外部 Caddy 反代过来——本质还是方案 A 的容器组合，少了 docker-compose 内的 Caddy |

### 5.3 原生（非 Docker）部署

适用场景：Windows 桌面单机使用、需要直接访问硬件设备等。

1. 下载 Stash 原生二进制：https://github.com/stashapp/stash/releases （Linux amd64/arm64/arm, Windows amd64, macOS amd64/arm64, FreeBSD amd64）
2. 首次运行 Stash 会下载 ffmpeg 到 `~/.stash/`（注意：原生二进制**不**捆绑 ffmpeg）
3. 前端单独构建：`cd web && pnpm build`，把 `dist/` 用任意静态服务器（甚至 Stash 内置反代能力）发布
4. AI 服务：手装 Python + 依赖；非 Docker 部署体验明显差，**强烈推荐 Docker**

### 5.4 备份策略

需要备份的目录：
- `stash-config/` — `config.yml`、`stash-go.sqlite`（DB）、`scrapers/`、`plugins/`
- `stash-metadata/` — Stash 自带的导出/备份文件
- `stash-blobs/` — 图像二进制（如配置了 blob-on-disk）

**不需要备份**：`stash-cache/`、`stash-generated/`（缩略图/sprite/preview，可重新生成，浪费存储）。

媒体目录 `/data` 由用户自己负责（一般有独立 NAS/RAID）。

推荐：cron 每日 `tar` 打包 config + DB 到独立磁盘 / 远程对象存储；保留近 14 天。

```bash
# 示例 backup.sh
DATE=$(date +%Y-%m-%d)
tar czf /backup/innershelf-$DATE.tar.gz \
  stash-config/ stash-metadata/ stash-blobs/ metatube-config/
find /backup -name "innershelf-*.tar.gz" -mtime +14 -delete
```

### 5.5 参考网络拓扑（家庭部署示例）

针对典型家庭"OpenWRT 网关 + 多 NAS"环境的部署建议：

```
                Internet
                   │
          :80 / :443 (端口转发)
                   │
        ┌──────────▼──────────┐
        │  OpenWRT (网关)      │  ← 只做 NAT/防火墙/DHCP/DNS
        │  ✓ 端口转发到 Debian │     不跑 Caddy/不跑 Docker
        │  ✗ 不跑应用服务      │
        └──────────┬──────────┘
                   │
            内网交换机
        ┌──────────┴────────────────┐
        │                            │
┌───────▼──────────────┐   ┌────────▼─────────┐
│  Debian NAS           │   │  TrueNAS          │
│  ┌──────────────────┐ │   │  ┌─────────────┐ │
│  │ 现有 Caddy        │ │   │  │ 媒体存储     │ │
│  │ (反代所有内网服务) │ │   │  │ (RAID/ZFS)  │ │
│  └────────┬─────────┘ │   │  └─────────────┘ │
│           │           │   │                  │
│  ┌────────▼─────────┐ │   │  应用层不跑       │
│  │ Docker Compose:  │ │   │  （专心做存储）   │
│  │  - Stash         │ │   │                  │
│  │  - MetaTube      │ │   │                  │
│  │  - 自定义前端     │ │   │                  │
│  │  - subtitle-forge │ │   │                  │
│  │  - AI Service    │ │   │                  │
│  └────────┬─────────┘ │   │                  │
│           │ NFS 挂载   │   │                  │
│           └────────────┼───┤                  │
│                        │   │                  │
└────────────────────────┘   └──────────────────┘
                  ▲
                  │ 内网客户端
              ─────────────────
              (电视/手机/电脑)
```

**关键决策**：

1. **Caddy 留在 Debian NAS**（已有的那一份），不要装到 OpenWRT 上——路由器是 L3/L4 工作，反代是 L7 工作，分开
2. **InnerShelf 服务全跑 Debian NAS**（Docker Compose），跟 Caddy 同机，省一跳
3. **TrueNAS 专心存储**，通过 NFS 把媒体目录暴露给 Debian（Stash 容器以只读卷挂入）
4. **OpenWRT 只做端口转发**（外部 80/443 → Debian 内网 IP）
5. **现有 Caddy 加几条规则即可**——不需要新起 Caddy 容器

NFS 挂载示例（Debian 上）：

```bash
# /etc/fstab
truenas.lan:/mnt/pool/media  /mnt/media  nfs  ro,nfsvers=4.1,_netdev  0  0
```

然后 Compose 里：

```yaml
services:
  stash:
    volumes:
      - /mnt/media:/data:ro       # 挂的就是 TrueNAS 那份
```

千兆网就够 4K 流畅播放；担心带宽就走万兆链路或 SMB multichannel。

---

## 6. 更新策略

### 6.1 Stash 后端更新

- **生产环境用 `v0.X.Y` 锁定版本，不要用 `latest` 或 `development` 标签**
- 更新流程：
  1. 看 release notes（https://github.com/stashapp/stash/releases）找 breaking changes
  2. 在测试环境跑：
     ```bash
     # 先备份
     tar czf stash-config-backup-$(date +%F).tar.gz stash-config/
     # 升级
     docker compose pull stash && docker compose up -d stash
     ```
  3. Stash 检测到新版本会重定向到 `/migrate`，**自动备份 DB 再迁移**，失败时自动回滚
  4. 测试前端跑回归（脚本化几个关键 query/mutation）
  5. 通过后再升生产
- **升级节奏**：跟随 Stash 的 minor release（约每季度一次），point release 跳过即可

### 6.2 GraphQL Schema 兼容性

- **每次 Stash minor 升级前**，重跑 `pnpm codegen` 看 TS 类型变化
- 关注 `@deprecated` 标注的字段，提前迁移到新字段
- Movie 类型迟早会被移除（已被 Group 取代），**新代码直接用 Group**
- CI 加一步：跑 codegen 后比 git diff，有 schema 变化人工 review

### 6.3 前端更新

- 走标准 GitHub Release 流程：
  - 打 tag `web-v0.x.y`
  - GitHub Actions build Docker image，push 到 ghcr.io
  - 生产 `docker compose pull frontend && docker compose up -d frontend`
- 因为前端无状态，可随时回滚（`docker compose up -d frontend` 改回旧 tag）

### 6.4 AI Service 更新

类似前端：独立版本号（`ai-v0.x.y`），独立 image。模型权重单独挂 volume，**不**打进 image（image 体积可控，模型重训不要 rebuild image）。

### 6.5 MetaTube 更新

- 跟 Stash 一样**锁定具体 tag**（不要 `latest`），生产环境定期升
- MetaTube 自带 SQLite 缓存，升级时缓存通常不需要重建
- 关注社区版 release notes：抓取规则会随上游站点改版调整
- 升级前备份 `metatube-config/` 目录

### 6.6 数据迁移与不兼容变更

- Stash 的 SQLite migrations 是**自动 + 自带回滚**的，相对安全
- 真正风险来自 schema 大重构（如未来 1.0 大版本）。建议：
  - 订阅 Stash GitHub Releases 通知
  - 每次升级前先在测试环境跑回归
  - **关键 query/mutation 写自动化集成测试**（Phase 2 后期补——这是最重要的护城河）

---

## 7. 其他注意事项

### 7.1 AGPL-3.0 许可证（重要，落实前必读）

Stash 是 **AGPL-3.0**，包含网络分发条款（Affero clause）：

| 行为 | 是否触发 AGPL | 说明 |
|--|--|--|
| 写独立前端，通过 GraphQL 调 Stash | ✗ 不触发 | FSF 主流解读：HTTP client 不构成 derivative work，前端**任意许可证均可** |
| Fork Stash 或 patch Stash 进程 | ✓ 触发 | 整个 fork 自动 AGPL，必须公开源码 |
| 修改 CommunityScrapers / CommunityScripts | ✓ 触发 | 它们也是 AGPL；改后再分发要公开 |
| 写新 Stash plugin 提交回 CommunityScripts | ✓ 部分触发 | 仅那个 plugin 是 AGPL，**不传染**你的前端 |

**实操建议**：

1. 不动 Stash 后端（vanilla）——避开第一颗雷
2. 自研前端许可证你自由选（MIT / Apache-2.0 / 商用闭源都可）
3. **如果将来商业化或闭源发布，必须找律师明确确认**——AGPL 的"网络分发"条款在不同司法管辖区有解释差异
4. 个人非商业自用：基本无忧

### 7.2 Stash 单用户模型

Stash 后端**只支持单用户**：一个 username + 一个 API Key，API Key 等于"完整管理员权限"。

影响：

- 家庭多人共用无法做"每人独立观看历史"
- API Key 不能下放只读权限

**短期对策**：v1 假设单用户。如果将来要多用户：
- 选项 A：前置一个网关（OIDC + 中转 GraphQL），改造成本高
- 选项 B：fork Stash 加用户系统，AGPL 上身 + 维护负担大
- **v2+ 议题**

### 7.3 抓取器策略：MetaTube 起步，必要时抽 InnerShelf Scraper Service

Stash 的 [CommunityScrapers](https://github.com/stashapp/CommunityScrapers) 仅有 JavLibrary、Heyzo、1pondo 等少数 JAV 站点，**缺 JavBus、Fanza/DMM、MGS** 等主流——但 **MetaTube 已经覆盖了这些**，是 v1 抓取层的起步首选。

**分阶段策略**：

#### v1 起步：MetaTube 直接打底

- 部署 MetaTube 容器（compose 已含）
- 前端通过 HTTP API 调 MetaTube：`GET /v1/movies/{code}`、`GET /v1/actors/{name}` 等
- 用 MetaTube 自带的多源聚合能力（JavBus / FANZA / MGS / JavLibrary）
- 写回 Stash 走 GraphQL `sceneUpdate`
- **零自研代码**，纯集成

#### v1.5：补 Stash YAML scraper 走 MetaTube

- 让 Stash 自带 UI 的"刮削元数据"按钮也能用——给它加 YAML scraper 配置指向 MetaTube API
- 用户点 Stash 自带的"Scrape"按钮时也能拿到结果
- 适合批量场景或不想用自定义前端时

#### v1.5+：抽 InnerShelf Scraper Service（如有需要）

如果 MetaTube 在某些场景下不够（比如 Cloudflare 拦截更严、特定站点 MetaTube 没覆盖、想要更精细的版本聚合），从归档仓库 [`Lynthar/InnerShelf-jellyfin`](https://github.com/Lynthar/InnerShelf-jellyfin) 的 `Jellyfin.Plugin.InnerShelf/Sources/BuiltIn/` 下取代码：

- `JavBusSource.cs` + `JavBusParser.cs` — 现成的 JavBus 解析
- `CloudflareDetector.cs` — 已有的 Cloudflare 检测逻辑
- `MetaTubeApiClient.cs` — 现成的 MetaTube 客户端封装

抽成独立 HTTP 服务（继续用 .NET 或重写 Python），**叠在 MetaTube 之上**提供更高级别能力（多源 fallback、Cloudflare 处理、版本聚合）。Stash 前端在 MetaTube 不够用时调它；如果未来恢复 Jellyfin 端开发，归档插件也能直接复用同一份服务。

**为什么不直接 v1 就抽 InnerShelf Scraper Service？** —— 抽取本身有工作量；MetaTube 大概率已经满足 v1 需求。等真撞到具体不够用的场景再抽，避免过早抽象。

### 7.4 性能与规模

- **SQLite 在 50k+ scenes 时 OK**，100k+ 时索引和 vacuum 要注意；Stash 已有优化但**不能换 Postgres**
- 视频缩略图 / sprites / preview 由 Stash 后台任务生成，吃磁盘（每个 scene 几 MB），**不要把 `/generated/` 放小盘**
- 前端虚拟列表（`@tanstack/react-virtual`）必须上，否则万级列表卡顿
- WebSocket 订阅（`jobsSubscribe`）适合做实时进度，但**不要订阅大频率事件**（比如每个 scene 创建都触发 UI 重渲染——做节流）

### 7.5 安全

| 要点 | 措施 |
|--|--|
| Stash 默认无认证 | **必须**在 Settings → Security 设置用户名密码 + API Key |
| Stash CORS 默认全开 | 内网部署 OK；公网必须前置 Caddy/nginx 限制来源 |
| Stash API Key 等于全权限 | **不要**嵌入前端 bundle；用户登录后存 localStorage（XSS 风险，所以 CSP 要严格） |
| **MetaTube token** | 部署时设 `TOKEN` 环境变量；前端调时带在 header；token 存配置而不是写死前端 |
| **MetaTube 不要直接公网暴露** | 仅在 docker network 内可达；通过反代或前端代理调；上游站点抓取被滥用会被封 IP |
| HTTPS 必须 | Caddy 自动签证书；或 Tailscale serve |
| Playground 生产关掉 | Stash 默认开启且**没有 config 开关**——用反代过滤 `/playground` 路径返回 404 |
| 媒体文件直接暴露 | 缩略图 URL 形如 `/scene/{id}/screenshot`，反代时记得限制只在登录后能访问 |
| Stash 自带 UI 路径 | 通过反代暴露在 `/admin/`（方案 A）或 `/admin/*`（方案 B）；登录认证由 Stash 自己处理 |

### 7.6 测试策略

- **前端单元测试**：Vitest + Testing Library，覆盖关键组件（filter 逻辑、表单校验、Apollo cache 操作）
- **GraphQL 集成测试**：CI 跑临时 Stash 容器（fixtures 数据），跑端到端 query/mutation
- **E2E 测试**：Playwright 跑关键用户流（登录 → 浏览 → 编辑标签 → 提交字幕任务）
- **回归测试**：每次 Stash 升级前必跑，作为 schema 兼容性的护城河
- **CI**：每次 PR 跑 lint + typecheck + 单测；nightly 跑 E2E

### 7.7 风险与缓解

| 风险 | 影响 | 对策 |
|--|--|--|
| Stash 大版本不兼容（如未来 v1.0 重构） | 前端要大改 | 锁定版本 + 关注 RFC + 集成测试护城河 |
| Stash 项目突然停止维护 | 整个后端需重写 | 短期：fork stash 自维护；长期：评估迁移；项目当前活跃度高，短期低风险 |
| 抓取站点封 IP / 改 HTML 结构 | 抓取失败 | 多源 fallback（JavBus + JavLibrary + MetaTube）、代理切换、错误监控 |
| AGPL 边界纠纷 | 法律风险 | 商业化前法律 review；非商业自用基本无忧 |
| 单 SQLite 扩不动 | 大库性能 | v2+ 议题；50k 以下不担心 |
| AI 模型推理成本 | GPU 资源 | sidecar 模式可水平扩展；按需启停容器 |
| 前端开发周期超预期 | 延期 | MVP 切小，先用 Stash 自带 UI 顶替设置/扫描页 |

### 7.8 待决问题（需要你后续决定）

- [ ] 中文产品名？（现在是 InnerShelf，要不要起个中文名？）
- [ ] 前端许可证选什么？（MIT / Apache-2.0 / GPL / 闭源）
- [ ] 公开发布渠道？（GitHub Releases + Docker Hub + 自建文档站）
- [ ] 多语言 UI 支持优先级？（中文 / 日文 / 英文）
- [ ] AI 模型选型（Whisper variant、视觉模型）何时定？

---

## 附：参考资料

- Stash 主仓：https://github.com/stashapp/stash
- Stash 文档：https://docs.stashapp.cc/
- GraphQL Schema：https://github.com/stashapp/stash/tree/develop/graphql/schema
- CommunityScrapers：https://github.com/stashapp/CommunityScrapers
- CommunityScripts：https://github.com/stashapp/CommunityScripts
- JAV-suitability RFC：https://github.com/stashapp/stash/issues/3065
- Stash Discourse 论坛：https://discourse.stashapp.cc/
- Stash Docker Hub：https://hub.docker.com/r/stashapp/stash
- Stash 开发文档：https://github.com/stashapp/stash/blob/develop/docs/DEVELOPMENT.md
- **MetaTube 主仓**：https://github.com/metatube-community/metatube-server
- **MetaTube Jellyfin 插件**（参考集成方式）：https://github.com/metatube-community/jellyfin-plugin-metatube

---

*文档版本：v0.3（2026-04-28 更新）。*

*v0.3 变更：移除"两产品并存"框架——Jellyfin 插件已归档至 [`Lynthar/InnerShelf-jellyfin`](https://github.com/Lynthar/InnerShelf-jellyfin) 停止主动开发，本仓库专注 Stash 前端。§0、§2.1、§2.2、§3.3、§4 Phase 4、§7.3 同步更新；InnerShelf Scraper Service 改为"从归档仓库移植代码"。*

*v0.2 变更：补充 MetaTube 元数据侧车、前端策略（不 fork ui/v2.5、不合并镜像）、Stash 自带 UI 双 UI 共存、部署形态变体（前端 nginx 自反代为默认）、参考网络拓扑（OpenWRT + Debian + TrueNAS）、Phase 4 元数据流、§7.3 抓取器策略调整为 MetaTube 起步。*

*请在每次重要决策变更后更新本文档。*
