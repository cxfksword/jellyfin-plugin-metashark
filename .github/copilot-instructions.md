# GitHub Copilot 指南：jellyfin-plugin-metashark

欢迎来到 `jellyfin-plugin-metashark`！本指南将帮助 AI 更好地理解项目，并提供有效的协助。

## 1. 项目概述

`jellyfin-plugin-metashark` 是一个为 Jellyfin 开发的元数据插件。它主要从豆瓣（Douban）获取电影和剧集的元数据，并利用 TheMovieDB (TMDB) 来补充剧集信息和图片。

该插件还集成了 `AnitomySharp` 库，用于解析动漫风格的文件名，从而实现更精准的匹配。

## 2. 核心架构

项目的核心逻辑围绕 Jellyfin 的 `I...Provider` 接口实现，这些实现位于 `Jellyfin.Plugin.MetaShark/Providers/` 目录中。

-   **插件入口**: `Jellyfin.Plugin.MetaShark/Plugin.cs` 是插件的起点，定义了插件的基本信息和配置页面。
-   **元数据提供者**:
    -   `MovieProvider.cs`: 负责电影的元数据搜索 (`GetSearchResults`) 和获取 (`GetMetadata`)。
    -   `SeriesProvider.cs`: 负责剧集的元数据。
    -   `EpisodeProvider.cs`: 负责剧集单集的元数据。
    -   这些提供者是插件功能的主要实现者。
-   **API 客户端**:
    -   与外部服务（如豆瓣、TMDB）的交互逻辑被封装在 `Jellyfin.Plugin.MetaShark/Api/` 目录中的各个 `...Api.cs` 文件中。这有助于将数据获取逻辑与 Jellyfin 的提供者逻辑分离。
-   **配置**:
    -   插件的配置项定义在 `Jellyfin.Plugin.MetaShark/Configuration/PluginConfiguration.cs` 中。

## 3. 关键模式和约定

-   **唯一的提供者 ID (Provider ID)**:
    -   为了防止与其他插件或元数据源冲突，本项目使用复合的 `ProviderId`。例如，`Douban_xxxx` 或 `Tmdb_yyyy`。
    -   在 `MovieProvider.cs` 和其他提供者中，您可以看到这样的实现：
        ```csharp
        // ...
        ProviderIds = new Dictionary<string, string> { { DoubanProviderId, x.Sid }, { Plugin.ProviderId, $"{MetaSource.Douban}_{x.Sid}" } }
        // ...
        ```
    -   这是一个非常重要的约定，确保 Jellyfin 能够正确区分来自不同来源的元数据。

-   **元数据获取流程**:
    1.  优先使用豆瓣 ID (`DoubanProviderId`) 进行精确查找。
    2.  如果找不到，则根据文件名和年份进行猜测匹配 (`GuessByDoubanAsync`)。
    3.  获取到豆瓣数据后，会尝试通过 IMDB ID 关联 TMDB ID，以获取更丰富的信息（如电影系列、分级）。
    4.  如果用户直接通过 TMDB 进行识别，则会调用 `GetMetadataByTmdb` 方法。

-   **文件名解析**:
    -   项目使用 `AnitomySharp` 库来解析复杂的文件名，特别是动漫。`NameParser.Parse()` 是处理此逻辑的入口。
    -   这对于处理特典（Extras）、特别篇（Specials）等非正片内容至关重要。

## 4. 开发工作流

-   **构建项目**:
    -   使用标准的 .NET 命令进行构建。
    ```sh
    dotnet restore
    dotnet publish --configuration=Release Jellyfin.Plugin.MetaShark/Jellyfin.Plugin.MetaShark.csproj
    ```

-   **测试和部署**:
    1.  构建插件后，将生成的 `Jellyfin.Plugin.MetaShark.dll` 和相关依赖项复制到一个新文件夹（例如 `metashark`）中。
    2.  将该文件夹移动到 Jellyfin 服务器的 `plugins` 目录下。
    3.  重启 Jellyfin 服务器以加载插件。

-   **调试**:
    -   调试通常需要将调试器附加到正在运行的 `jellyfin.dll` 进程上。

在您开始编码之前，请务必理解上述约定，特别是关于 **唯一提供者 ID** 的部分。
