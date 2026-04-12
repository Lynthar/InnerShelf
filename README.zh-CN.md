# InnerShelf

一个用于管理成人影片库的 Jellyfin 插件（主要针对 JAV/日本成人影片）。直接安装在原版 Jellyfin Server 上，无需 fork。

[English](README.md)

## 功能特性

- **番号自动识别** — 从文件名中自动提取番号，支持有码、素人、FC2、无码、HEYZO 等多种格式
- **元数据刮削** — 内置 JavBus 爬虫，自动获取标题、发行日期、类型、制作商、演员和封面
- **可选 MetaTube 后端** — 连接 [MetaTube](https://github.com/metatube-community) 服务器，获得 37+ 个数据源支持
- **丰富的标签分类** — 将制作商、厂牌、系列、类型、演员映射到 Jellyfin 原生元数据字段
- **客户端兼容** — 使用标准 Jellyfin `Movie` 类型，兼容 Infuse、Swiftfin、Jellyfin Web 及所有其他客户端
- **演员头像** — 自动获取演员头像图片

## 安装

### 从插件仓库安装（推荐）

1. 在 Jellyfin 控制面板中，进入 **管理 → 插件 → 存储库**
2. 添加 InnerShelf 插件仓库 URL（即将推出）
3. 从目录中安装 **InnerShelf**
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
| Enable FANZA | 启用 FANZA/DMM 数据源 | 开启 |
| MetaTube Server URL | 连接 MetaTube 后端（留空则禁用） | 空 |
| Title Template | 显示标题格式（`{code}`、`{title}`） | `{code} {title}` |
| HTTP Proxy | 元数据请求代理 | 空 |

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
│   ├── BuiltIn/     # JavBus、FANZA 爬虫
│   └── MetaTube/    # 可选 MetaTube 后端连接器
├── Providers/       # Jellyfin 元数据和图片提供者
├── Mapping/         # 内部模型 → Jellyfin 类型映射
├── ExternalIds/     # 番号作为 Jellyfin 外部 ID
└── Configuration/   # 插件配置和 Web UI
```

## 许可证

MIT
