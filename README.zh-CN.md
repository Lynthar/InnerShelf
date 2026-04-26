# InnerShelf

一个用于管理成人影片库的 Jellyfin 插件。直接安装在原版 Jellyfin Server 上。

[English](README.md)

## 功能特性

- **番号自动识别** — 从文件名中自动提取番号，支持有码、素人、FC2、无码、HEYZO 等多种格式
- **元数据刮削** — 内置 JavBus 爬虫，自动获取标题、发行日期、类型、制作商、演员和封面
- **可选 MetaTube 后端** — 连接 [MetaTube](https://github.com/metatube-community) 服务器，获得 37+ 个数据源支持
- **丰富的标签分类** — 将制作商、厂牌、系列、类型、演员映射到 Jellyfin 原生元数据字段
- **客户端兼容** — 使用标准 Jellyfin `Movie` 类型，兼容 Infuse、Swiftfin、Jellyfin Web 及所有其他客户端
- **演员头像** — 自动获取演员头像图片
- **字幕生成** — 可选与 [subtitle-forge](https://github.com/Lynthar/subtitle-forge) 集成，将视频交由远程 GPU 主机生成并翻译字幕，按影片手动触发

## 系统要求

- **Jellyfin Server 10.11.0 或更高版本** —— 更旧的 patch 版本会在 `targetAbi` 检查时拒绝安装。

## 安装

### 从插件仓库安装（推荐）

1. 在 Jellyfin 控制面板进入 **管理 → 插件 → 存储库** → **+** 添加
2. 仓库 URL：`https://lynthar.github.io/InnerShelf/manifest.json`
3. 保存 → **目录** 标签页 → 安装 **InnerShelf**
4. 重启 Jellyfin

### 手动安装

1. 从 [Releases](https://github.com/Lynthar/InnerShelf/releases) 下载最新版本的 ZIP 包
2. 解压到 Jellyfin 插件目录：
   - Linux: `~/.local/share/jellyfin/plugins/InnerShelf/`
   - Docker: `/config/plugins/InnerShelf/`
   - Windows: `%APPDATA%\jellyfin\plugins\InnerShelf\`
   - macOS: `~/.local/share/jellyfin/plugins/InnerShelf/`
3. 重启 Jellyfin

## 配置

安装后，进入 **管理 → 插件 → InnerShelf** 进行配置：

| 配置项 | 说明 | 默认值 |
|--------|------|--------|
| Enable JavBus | 启用 JavBus 数据源 | 开启 |
| Enable FANZA | _计划中_ —— 配置项已接入，但 FANZA 爬虫尚未实现 | 开启 |
| MetaTube Server URL | 连接 MetaTube 后端（留空则禁用） | 空 |
| Title Template | 显示标题格式（`{code}`、`{title}`） | `{code} {title}` |
| HTTP Proxy | 元数据请求代理 | 空 |
| Subtitle Forge Server URL | [subtitle-forge](https://github.com/Lynthar/subtitle-forge) 服务器地址（留空则禁用） | 空 |
| Subtitle Forge Token | Bearer Token，必须与 GPU 主机上的 `SUBTITLE_FORGE_TOKEN` 一致 | 空 |
| Subtitle Languages | 目标字幕语言，逗号分隔（如 `zh`、`zh,en`） | `zh` |
| Keep Original Subtitle | 保留源语言 `.srt`，与翻译版并存 | 开启 |
| Bilingual Subtitles | 将源 + 目标合并为一个 `.<src>-<tgt>.srt` | 关闭 |
| Path Mappings | Jellyfin 路径前缀 → subtitle-forge 路径前缀重写规则 | 空 |

## 工作原理

### 文件命名

InnerShelf 从文件名中解析番号，支持以下格式：

| 类型 | 格式 | 示例 |
|------|------|------|
| 有码 | `前缀-编号` | `SSIS-001.mp4` |
| 素人 | `NNN前缀-编号` | `390JAC-132.mp4` |
| FC2 | `FC2-PPV-编号` | `FC2-PPV-1234567.mp4` |
| 无码 | `NNNNNN-NNN` | `010120-001.mp4` |
| HEYZO | `HEYZO-NNNN` | `HEYZO-1234.mp4` |

分辨率标签（`1080p`、`4K`）、编码标签（`x265`、`HEVC`）和方括号内容会被自动忽略。

中文字幕后缀（`-C`、`-ch`）和多碟标识（`-cd1`、`-cd2`）会被检测并保存为元数据。

### 元数据映射

| 源字段 | Jellyfin 字段 |
|--------|--------------|
| 番号 | Provider ID (`InnerShelf`) |
| 日文标题 | Original Title |
| 显示标题 | Name（通过模板） |
| 发行日期 | Premiere Date |
| 类型 | Genres |
| 制作商 | Studios |
| 厂牌 | Tag (`Label: ...`) |
| 系列 | Tag (`Series: ...`) |
| 演员 | People（含头像） |
| 导演 | People (Director) |
| 正面封面 | Primary Image |
| 完整封面 | Backdrop Image |
| 分级 | `XXX` |

## 字幕生成（可选）

InnerShelf 可以把视频文件交给运行在另一台 GPU 主机上的 [subtitle-forge](https://github.com/Lynthar/subtitle-forge)
服务器处理。生成是**按影片手动触发**的 —— 没有自动后台处理、没有定时扫描。

### 部署步骤

1. 在 GPU 主机上以服务器模式运行 subtitle-forge（参见其 README）。
   需要一个 bearer token，用 `openssl rand -hex 32` 生成。
2. 在 InnerShelf 配置页填写 **Subtitle Generation** 区域：
   - **Server URL** — `http://<gpu-host>:8765`
   - **Bearer Token** — 与 GPU 主机上的 `SUBTITLE_FORGE_TOKEN` 一致
   - **Path Mappings** — 如果 Jellyfin 和 GPU 主机看到的存储路径不同
     （例如 Jellyfin 看到 `/media/jav`，GPU 主机通过 SMB 挂载到
     `/Volumes/nas-jav`），加一条重写规则。最长前缀匹配。

### 触发字幕生成

两种方式：

1. **库级定时任务** —— 控制台 → 计划任务 → "InnerShelf: Backfill subtitles"
   → 立即运行。遍历所有 InnerShelf 已识别的影片，跳过已经有
   `.<lang>.srt` 的项目，对剩下的提交字幕生成任务。默认手动触发；
   想定期跑就在控制台加个 cron 触发器。

2. **单条触发用书签** —— 把下面这段保存成浏览器书签：

```javascript
javascript:(()=>{const m=location.hash.match(/[?&]id=([a-f0-9]{32})/i);if(!m){alert('当前不是影片详情页');return;}fetch('/InnerShelf/Subtitles/Generate?itemId='+m[1],{method:'POST',headers:{'X-Emby-Token':ApiClient.accessToken()}}).then(async r=>{const t=await r.text();alert(r.ok?('已提交：'+t):('失败 '+r.status+'：'+t));}).catch(e=>alert('网络错误：'+e));})();
```

打开任意影片详情页 → 点击书签 → 弹窗显示 job id 即提交成功。
此接口要求管理员权限（`RequiresElevation`）。

### 接口

| 方法 | 路径 | 说明 |
|---|---|---|
| `POST` | `/InnerShelf/Subtitles/Generate?itemId={guid}&languages=zh,en` | 提交任务，`languages` 可选，未传时使用插件配置 |
| `GET`  | `/InnerShelf/Subtitles/Jobs/{jobId}` | 代理到 subtitle-forge 的 `GET /jobs/{id}`；走 Jellyfin 鉴权，无需把 subtitle-forge token 暴露给客户端 |

生成的 `.srt` 文件直接写到视频同目录，Jellyfin 下次扫库或刷新元数据时自动识别。

## 从源码构建

需要 [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)。

```bash
git clone https://github.com/Lynthar/InnerShelf.git
cd InnerShelf
dotnet build
dotnet test
```

编译后的插件 DLL 位于 `Jellyfin.Plugin.InnerShelf/bin/Debug/net9.0/Jellyfin.Plugin.InnerShelf.dll`。

## 项目结构

```
Jellyfin.Plugin.InnerShelf/
├── Naming/          # 文件名番号解析
├── Sources/         # 元数据源抽象层
│   ├── BuiltIn/     # JavBus 爬虫
│   └── MetaTube/    # 可选 MetaTube 后端连接器
├── Providers/       # Jellyfin 元数据和图片提供者
├── Mapping/         # 内部模型 → Jellyfin 类型映射
├── ExternalIds/     # 番号作为 Jellyfin 外部 ID
├── Subtitles/       # subtitle-forge HTTP 客户端 + REST 控制器 + 路径映射器
└── Configuration/   # 插件配置和 Web UI
```

## 维护

插件通过 `Jellyfin.Plugin.InnerShelf/meta.json` 和 `build.yaml` 里的
`targetAbi` 锁定最低 Jellyfin Server 版本 `10.11.0+`（两个值必须一致）。
CI（`.github/workflows/build-test.yml`）会在每次 push/PR 时跑
`dotnet build` + `dotnet test`；Dependabot（`.github/dependabot.yml`）
每周一扫一次 Jellyfin SDK 是否有新版，自动开 PR。csproj 里
`TreatWarningsAsErrors=true`，任何 deprecated 警告会让 CI 直接失败，
bad SDK bump 在 merge 前就被拦下。

### 发布

在 `main` 上打 `vN.N.N.N` tag 会触发 `.github/workflows/release.yml`：
build + test → 把 `Jellyfin.Plugin.InnerShelf.dll` + `AngleSharp.dll` +
`meta.json` 打成 `innershelf_<version>.zip` → 挂到 GitHub release →
向 `gh-pages` 分支的 `manifest.json` 追加新版本条目。

**插件仓库 URL 一次性配置**：

1. 打第一个 release tag（如 `v0.1.0.1`）—— 工作流首次运行时会自动创建
   `gh-pages` 分支。
2. 仓库 Settings → **Pages** → Source 选 `gh-pages` 分支 + 根目录 → 保存。
3. 等约 1 分钟首次发布，访问
   `https://lynthar.github.io/InnerShelf/manifest.json` 验证 manifest 出来了。
4. （可选）Settings → **Branches/Rules** —— 如果 "require signed commits"
   规则覆盖到 `gh-pages`，把它范围限制到 `refs/heads/main`，或者把
   `github-actions[bot]` 加成 bypass actor；工作流向 `gh-pages` 推的
   commit 是未签名的。

Jellyfin 出新版时：

- **Patch（如 `10.11.8` → `10.11.9`）** —— CI 绿就 merge Dependabot PR，不需要 bump `targetAbi`（没必要把还在旧 patch 的用户排除）。
- **Minor（如 `10.11` → `10.12`)** —— 同上，加上用 Docker 真机 smoke test（`docker run --rm -p 8096:8096 -v /tmp/jf:/config jellyfin/jellyfin:10.12.0`，把编译产物 copy 到 `/tmp/jf/plugins/InnerShelf/`，重启，验证插件能加载且能跑一次抓取）；通过就在 `meta.json` 和 `build.yaml` 同步 bump `targetAbi`，打 release tag。
- **Major（如 `10.x` → `11.0`）** —— 当 port 项目处理，Jellyfin 大版本通常会改 plugin API。

## 许可证

MIT
