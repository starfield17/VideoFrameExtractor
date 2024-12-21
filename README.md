```markdown
# VideoFrameExtractor

## 简介

**VideoFrameExtractor** 是一个使用 C# 开发的命令行工具，用于从视频文件中提取帧。该工具支持多种常见的视频格式，并利用 `ffmpeg` 进行帧提取，适用于需要批量处理视频文件的用户。

## 功能

- 支持多种视频格式：`mp4`, `avi`, `mkv`, `mov`, `flv`, `wmv`,`webm` 
- 可处理单个视频文件或整个目录中的视频文件
- 自动创建输出目录并保存提取的帧
- 跨平台支持（Windows 和 Linux）
- 内置 `ffmpeg` 管理，简化使用流程

## 安装

### 前提条件

- [.NET 6.0 SDK](https://dotnet.microsoft.com/download) 或更高版本
- `ffmpeg` 可执行文件（工具已内置在项目中）

### 下载

克隆本仓库到本地：

```bash
git clone https://github.com/yourusername/VideoFrameExtractor.git
```

### 构建

进入项目目录并构建项目：

```bash
cd VideoFrameExtractor
dotnet build -c Release
```

构建完成后，执行文件位于 `bin/Release/net6.0` 目录下。

## 使用说明

### 命令行用法

```bash
VideoFrameExtractor <视频文件或目录路径>
```

### 参数说明

- `<视频文件或目录路径>`：指定单个视频文件或包含视频文件的目录。

### 示例

处理单个视频文件：

```bash
VideoFrameExtractor /path/to/video/sample.mp4
```

处理整个视频目录：

```bash
VideoFrameExtractor /path/to/videos/
```

## 支持的格式

- **mp4**
- **avi**
- **mkv**
- **mov**
- **flv**
- **wmv**
- **webm**
- 
## ffmpeg

本工具内置了适用于不同操作系统的 `ffmpeg` 可执行文件。根据运行环境，`ffmpeg` 会自动选择合适的版本：

- **Windows**: `ffmpeg/windows/ffmpeg.exe`
- **Linux**: `ffmpeg/linux/ffmpeg`

如果 `ffmpeg` 不存在或无法执行，工具将提示错误信息。

## 错误处理

- **路径不存在**：如果指定的路径不存在，程序会输出错误信息并退出。
- **不支持的文件格式**：如果文件不是支持的视频格式，程序会提示错误。
- **ffmpeg 处理失败**：如果 `ffmpeg` 在处理视频时出错，程序会输出错误详情。
- **权限问题（Linux）**：工具会尝试为 `ffmpeg` 设置执行权限，如果失败，会输出警告信息。
