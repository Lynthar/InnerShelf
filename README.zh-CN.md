<div align="center">

<img src="InnerShelf.png" alt="InnerShelf" width="96">

# InnerShelf

[![license](https://img.shields.io/github/license/Lynthar/InnerShelf)](LICENSE)

</div>

面向 JAV 媒体库的 Stash 自研前端（vanilla Stash 后端 + React）。早期脚手架，尚不可用。

[English](README.md) | 简体中文

> **施工中，而且很早期。** 这个仓库现在是一个 React 脚手架：对着一个 Stash 实例跑一条
> sanity query，把结果列出来。没有像样的界面、没有路由、没有播放器、没有可安装的东西、
> 也没有 release。觉得下面这套架构有意思可以先关注，但别指望现在能跑起来用。

这是一个给成人媒体库用的浏览前端，跑在一个**未经修改的 Stash** 之上。Stash 负责
库、扫描和刮削；这边负责浏览、详情和编辑，按 JAV 库真正需要的方式来：品番当主键、
同一部作品的多个版本、女优是独立的一级实体。

## 架构

**Stash 保持原样。** 不 fork，不打进程内补丁。这边一切都走它的 GraphQL API——
这也意味着 Stash 的 AGPL-3.0 留在 Stash 那一侧，这个前端可以是 MIT。

**两个界面，一个数据库。** 设置、扫描、刮削器、插件管理都留在 Stash 自带的界面里。
这个前端要接管的是浏览、详情、编辑和搜索，也就是日常使用中最常打交道的部分。

**扩展做成侧车。** 元数据聚合与字幕生成是独立的 HTTP 服务，不是这个仓库里的代码——
因为 Stash 的插件机制扩展不了它的 GraphQL schema。

## 现状

今天真实存在的全部：一个四个源文件的 React 应用、一个 Apollo 客户端、
一条 `findScenes` 查询，以及前二十条结果的列表。

没有实现的：界面、路由、播放器、任何一个侧车，以及后端——后端是第三方容器，
不是这里的代码。CI 目前是红的，卡在 pnpm 那一步。不会兼容 Infuse、Swiftfin 这类
第三方 Jellyfin 客户端——这是当初离开 Jellyfin 时就接受的结果。

## 开发

**目前没有安装方式。** 下面是把脚手架跑起来的方法。需要 Docker、Node 22，
以及用 corepack 启用的 pnpm。

```bash
git clone https://github.com/Lynthar/InnerShelf.git
cd InnerShelf
```

把 `docker/dev/docker-compose.yml` 里的媒体目录指到真实路径，然后：

```bash
cd docker/dev && docker compose up -d
```

这会起一个 `:9999` 的 Stash 和一个 `:8080` 的元数据服务。打开 Stash 走完首次运行向导，
生成一个 API Key。

```bash
cd ../../web
pnpm install
cp .env.example .env.local     # 填 VITE_STASH_API_KEY
pnpm codegen                   # 内省跑着的 Stash 生成类型
pnpm dev
```

控制台打印出取回了多少条 scene，就算成功。

门禁：`pnpm lint`、`pnpm typecheck`、`pnpm test`、`pnpm build`。

## 配置

`web/.env.local` 里两个变量：

| 键 | 默认 |
|---|---|
| `VITE_STASH_URL` | `http://localhost:9999` |
| `VITE_STASH_API_KEY` | 无 |

这里的 API Key 只供本地开发。Vite 会把它打进 bundle，所以真正部署时要改成运行时录入，
那部分还没写。

## 路线图

这个顺序是特意反过来的：先让后端栈真正跑起来，前端按一个人实际需要的规模重新设计。

- 先把后端跑好——Stash 加元数据服务，装在真实机器上，镜像版本钉死——这期间日常浏览
  先用 Stash 自带界面。
- 写一份指向元数据服务的刮削器配置，让 Stash 自己的刮削按钮就能取到多源聚合的元数据。
  **不需要一行前端代码。**
- 字幕在别处定时生成，SRT 落在视频旁边，Stash 下次扫描自动挂上。
- 然后前端只做一页：以品番为中心的网格加一个详情面板，能播放、打标签、改名、删除、
  刮削、配字幕。**文件操作是重点**——`moveFiles` 和 `deleteFiles` 在 Stash 的 API 里是
  完整支持的，但它自带界面从没给过入口。

## 在找那个 Jellyfin 插件？

这个仓库以前是一个 Jellyfin 插件，那条线已经停了。最后一版是 2026 年 4 月的 v0.1.3，
`gh-pages` 分支上的 manifest 冻结在那一版——已经装了的仍然能解析到它，但不会再有新版本，
也不会有针对新版 Jellyfin 的兼容性修复。完整的源码历史在
[Lynthar/InnerShelf-jellyfin](https://github.com/Lynthar/InnerShelf-jellyfin)。

本仓库 Releases 里列出的那几个都是那个时期的产物，与现在的代码无关。

## 许可证

MIT —— 见 [LICENSE](LICENSE)。Copyright (c) 2026 Lynthar。

[Stash](https://github.com/stashapp/stash) 是 AGPL-3.0，本项目未修改它，
以独立程序的形式通过网络调用。
