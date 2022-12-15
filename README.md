# jellyfin-plugin-metashark

[![metashark](https://img.shields.io/github/v/release/cxfksword/jellyfin-plugin-metashark)](https://github.com/cxfksword/jellyfin-plugin-metashark/releases)
[![metashark](https://img.shields.io/badge/jellyfin-10.8.x-lightgrey)](https://github.com/cxfksword/jellyfin-plugin-metashark/releases)
[![metashark](https://img.shields.io/github/license/cxfksword/jellyfin-plugin-metashark)](https://github.com/cxfksword/jellyfin-plugin-metashark/main/LICENSE) 

jellyfin电影元数据插件，影片信息只要从豆瓣获取，并由TheMovieDb补全缺失的剧集数据。

功能：
* 支持从豆瓣和TMDB获取元数据
* 兼容anime动画命名格式

![preview](doc/logo.png)

## 安装插件

只支持最新的`jellyfin 10.8.x`版本

添加插件存储库：

国内加速：https://ghproxy.com/https://github.com/cxfksword/jellyfin-plugin-metashark/releases/download/manifest/manifest_cn.json

国外访问：https://github.com/cxfksword/jellyfin-plugin-metashark/releases/download/manifest/manifest.json

## 如何使用

1. 安装后，先进入`控制台 -> 插件`，查看下MetaShark插件是否是**Active**状态
2. 进入`控制台 -> 媒体库`，点击任一媒体库进入配置页，在元数据下载器选项中勾选**MetaShark**，并把**MetaShark**移动到第一位

   <img src="https://cdn.jsdelivr.net/gh/kozalak-robot/assets@main/img/3fZmJK.png"  width="400px" /> <img src="https://cdn.jsdelivr.net/gh/kozalak-robot/assets@main/img/hAovDC.png"  width="400px" />
   
3. 识别时默认不返回TheMovieDb结果，有需要可以到插件配置中打开
4. 假如网络原因访问TheMovieDb比较慢，可以到插件配置中关闭从TheMovieDb获取数据（关闭后不会再获取剧集信息）
  

## How to build

1. Clone or download this repository

2. Ensure you have .NET Core SDK setup and installed

3. Build plugin with following command.

```sh
dotnet restore 
dotnet publish --output=artifacts  Jellyfin.Plugin.MetaShark/Jellyfin.Plugin.MetaShark.csproj

# remove unused dll
cd artifacts
rm -rf MediaBrowser*.dll Microsoft*.dll Newtonsoft*.dll System*.dll Emby*.dll Jellyfin.Data*.dll Jellyfin.Extensions*.dll *.json *.pdb
```


## How to test

1. Build the plugin

2. Create a folder, like `metashark` and copy  `artifacts/*.dll` into it

3. Move folder `metashark` to jellyfin `data/plugins` folder


## QA

1. Plugin run in error: `System.BadImageFormatException: Bad IL format.` 

> Remove all hidden file in `metashark` plugin folder