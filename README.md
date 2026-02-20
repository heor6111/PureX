# PureX - 专业多媒体格式转换工具

<div align="center">

**一款简洁、高效的多媒体格式转换工具**

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)](https://www.microsoft.com/windows)

</div>

---

## 📖 简介

PureX 是一款基于 WPF 开发的多媒体格式转换工具，支持视频、音频、图片等多种格式的相互转换。采用 FFmpeg 作为核心转换引擎，提供稳定、高质量的转换服务。

## ✨ 功能特性

### 核心功能

- 🎬 **视频转换** - 支持 MP4、AVI、MKV、WebM、MOV 等主流视频格式
- 🎵 **音频转换** - 支持 MP3、WAV、AAC、FLAC、OGG 等音频格式
- 🖼️ **图片转换** - 支持 JPG、PNG、WebP、GIF、BMP、TIFF 等图片格式
- 📁 **批量处理** - 支持多文件批量转换，提高工作效率
- 🔄 **智能格式识别** - 自动识别源文件格式，推荐可转换的目标格式

### 工具箱功能

- 🎧 **音频提取** - 从视频中提取音频
- 🔗 **音频合成** - 将多个音频文件合并
- 🔧 **格式修复** - 修复损坏的媒体文件索引
- ✂️ **音视频裁剪** - 按时间裁剪音视频片段
- 🎞️ **音视频剪辑** - 合并多个音视频文件

### 用户体验

- 🖱️ **拖拽操作** - 支持文件拖拽添加
- 📊 **实时进度** - 显示转换进度、速度、剩余时间
- ⚡ **并发处理** - 支持多任务并发转换
- 💾 **磁盘检测** - 转换前检测磁盘空间
- 📝 **错误详情** - 转换失败时显示详细错误原因

## 📥 安装使用

### 系统要求

- Windows 10/11 (64位)
- 无需安装 .NET Runtime

### 下载

从 [Releases](../../releases) 页面下载最新版本。

### 使用

1. 解压到任意目录
2. 双击 `PureX.exe` 运行

## 🔨 开发构建

### 环境要求

- .NET 8.0 SDK
- Visual Studio 2022 或 JetBrains Rider

### 克隆项目

```bash
git clone https://github.com/your-username/PureX.git
cd PureX
```

### 下载 FFmpeg

⚠️ **重要**: FFmpeg 不包含在仓库中，需要手动下载：

1. 从 [FFmpeg官网](https://ffmpeg.org/download.html) 下载 Windows 版本
2. 解压到 `tools/ffmpeg/` 目录
3. 确保 `tools/ffmpeg/bin/ffmpeg.exe` 和 `ffprobe.exe` 存在

目录结构：
```
tools/
└── ffmpeg/
    └── bin/
        ├── ffmpeg.exe
        └── ffprobe.exe
```

### 编译运行

```bash
# 还原依赖
dotnet restore

# 编译
dotnet build

# 运行
dotnet run --project src/PureX/PureX.csproj

# 发布
.\build.bat
```

## 📁 项目结构

```
PureX/
├── src/
│   ├── PureX/                    # 主程序 (WPF)
│   │   ├── Models/               # 数据模型
│   │   ├── ViewModels/           # 视图模型
│   │   ├── Views/                # 视图
│   │   └── Converters/           # 值转换器
│   │
│   └── PureX.Core/               # 核心库
│       ├── Converters/           # 格式转换器
│       ├── Models/               # 数据模型
│       └── Services/             # 服务层
│
├── tools/                        # 工具目录 (需下载)
│   └── ffmpeg/
│
├── PureX.sln                     # 解决方案文件
├── build.bat                     # 构建脚本
└── README.md
```

## 🛠️ 技术栈

| 技术 | 说明 |
|------|------|
| .NET 8.0 | 运行时框架 |
| WPF | UI框架 |
| MVVM | 架构模式 |
| FFmpeg | 多媒体处理 |
| ImageMagick | 图片处理 |

## 📋 支持格式

### 视频

| 输入 | 输出 |
|------|------|
| MP4, AVI, MKV, MOV, WMV, FLV, WebM | MP4, AVI, MKV, WebM, MOV |

### 音频

| 输入 | 输出 |
|------|------|
| MP3, WAV, FLAC, AAC, OGG, M4A, WMA | MP3, WAV, FLAC, AAC, OGG, M4A |

### 图片

| 输入 | 输出 |
|------|------|
| JPG, PNG, WebP, GIF, BMP, TIFF, ICO | JPG, PNG, WebP, GIF, BMP, TIFF, ICO |

## 📄 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件

## 🙏 致谢

- [FFmpeg](https://ffmpeg.org/) - 多媒体处理框架
- [ImageMagick](https://imagemagick.org/) - 图片处理库
