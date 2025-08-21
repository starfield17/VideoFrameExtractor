# VideoFrameExtractor

## Overview

**VideoFrameExtractor** is a command-line tool developed in C# for extracting frames from video files. This tool supports many common video formats and uses `ffmpeg` for frame extraction. It is ideal for users who need to process video files in bulk.

## Features

- Supports various video formats: `mp4`, `avi`, `mkv`, `mov`, `flv`, `wmv`, `webm`
- Can process a single video file or all video files in a directory
- Automatically creates output directory and saves extracted frames
- Cross-platform compatibility (Windows and Linux)
- Simplified usage through built-in `ffmpeg` management

## Installation

### Prerequisites

- [.NET 6.0 SDK](https://dotnet.microsoft.com/download) or higher
- `ffmpeg` executable (included with the project)

### Download

Clone the repository locally:

```bash
git clone https://github.com/starfield17/VideoFrameExtractor.git
```

### Build

Navigate to the project directory and build the project:

```bash
cd VideoFrameExtractor
dotnet build -c Release
```

After building, the executable will be located in the `bin/Release/net6.0` directory.

## Usage

### Command Line Usage

```bash
VideoFrameExtractor <path to video file or directory>
```

### Parameter Description

- `<path to video file or directory>`: Specifies a single video file or a directory containing video files.

### Usage Examples

Process a single video file:

```bash
VideoFrameExtractor /path/to/video/sample.mp4
```

Process an entire video directory:

```bash
VideoFrameExtractor /path/to/videos/
```

## Supported Formats

- **mp4**
- **avi**
- **mkv**
- **mov**
- **flv**
- **wmv**
- **webm**

## ffmpeg

This tool includes `ffmpeg` executables for different operating systems. The appropriate version of `ffmpeg` is automatically selected based on the execution environment:

- **Windows**: `ffmpeg/windows/ffmpeg.exe`
- **Linux**: `ffmpeg/linux/ffmpeg`

If `ffmpeg` does not exist or cannot be executed, the tool will display an error message.

## Error Handling

- **Path does not exist**: If the specified path does not exist, the program will output an error message and exit.
- **Unsupported file format**: If a file is not in a supported video format, an error will be displayed.
- **ffmpeg processing error**: If `ffmpeg` encounters an error during video processing, error details will be output.
- **Permission issues (Linux)**: The tool attempts to set execution permissions for `ffmpeg`. If this fails, a warning message will be displayed.
