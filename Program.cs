using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace VideoFrameExtractor
{
    class Program
    {
        // 定义支持的视频文件扩展名
        static readonly string[] VIDEO_EXTENSIONS = { "mp4", "avi", "mkv", "mov", "flv", "wmv" };

        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                ShowUsage();
                return 1;
            }

            string inputPath = args[0];

            if (Directory.Exists(inputPath))
            {
                Console.WriteLine($"检测到目录: {inputPath}");
                Console.WriteLine("开始处理目录中的视频文件...");
                ProcessDirectory(inputPath);
                Console.WriteLine("所有视频文件处理完成。");
            }
            else if (File.Exists(inputPath))
            {
                if (IsVideoFile(inputPath))
                {
                    Console.WriteLine($"处理单个视频文件: {inputPath}");
                    ProcessVideo(inputPath);
                    Console.WriteLine("视频文件处理完成。");
                }
                else
                {
                    Console.WriteLine($"错误: 文件 '{inputPath}' 不是支持的视频格式。");
                    return 1;
                }
            }
            else
            {
                Console.WriteLine($"错误: 路径 '{inputPath}' 不存在。");
                return 1;
            }

            return 0;
        }

        static void ShowUsage()
        {
            Console.WriteLine("==============================================");
            Console.WriteLine("          视频帧提取脚本使用指南");
            Console.WriteLine("==============================================");
            Console.WriteLine("用法: VideoFrameExtractor /path/to/video_or_directory");
            Console.WriteLine("");
            Console.WriteLine("参数说明:");
            Console.WriteLine("  /path/to/video_or_directory  指定单个视频文件或包含视频文件的目录。");
            Console.WriteLine("");
            Console.WriteLine("示例:");
            Console.WriteLine("  VideoFrameExtractor /var/home/user/Videos/sample.mp4");
            Console.WriteLine("  VideoFrameExtractor /var/home/user/Videos/");
            Console.WriteLine("");
            Console.WriteLine("支持的视频格式:");
            Console.WriteLine("  mp4, avi, mkv, mov, flv, wmv");
            Console.WriteLine("==============================================");
        }

        static bool IsVideoFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).TrimStart('.').ToLower();
            return VIDEO_EXTENSIONS.Contains(extension);
        }

        static void ProcessDirectory(string directoryPath)
        {
            try
            {
                var files = Directory.EnumerateFiles(directoryPath, "*.*", SearchOption.AllDirectories)
                                     .Where(file => IsVideoFile(file));

                foreach (var file in files)
                {
                    Console.WriteLine($"处理视频文件: {file}");
                    bool success = ProcessVideo(file);
                    if (!success)
                    {
                        Console.WriteLine($"处理失败: {file}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: 处理目录时出错 - {ex.Message}");
            }
        }

        static bool ProcessVideo(string videoPath)
        {
            if (!File.Exists(videoPath))
            {
                Console.WriteLine($"错误: 文件 '{videoPath}' 不存在。");
                return false;
            }

            string videoDir = Path.GetDirectoryName(videoPath);
            string videoFilename = Path.GetFileName(videoPath);
            string videoName = Path.GetFileNameWithoutExtension(videoPath);

            string outputDir = Path.Combine(videoDir, $"{videoName}_frames");
            Directory.CreateDirectory(outputDir);

            // 获取当前操作系统
            string ffmpegPath = GetFfmpegPath();

            if (string.IsNullOrEmpty(ffmpegPath))
            {
                Console.WriteLine("错误: 无法找到适用于当前操作系统的 ffmpeg。");
                return false;
            }

            // 构建输出文件路径
            string outputPattern = Path.Combine(outputDir, $"{videoName}_%d.png");

            // 设置 ffmpeg 命令参数
            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-i \"{videoPath}\" -vf fps=1 \"{outputPattern}\" -hide_banner -loglevel error",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (var process = Process.Start(startInfo))
                {
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        Console.WriteLine($"帧已保存到目录: {outputDir}");
                        return true;
                    }
                    else
                    {
                        string error = process.StandardError.ReadToEnd();
                        Console.WriteLine($"错误: ffmpeg 处理失败 - {error}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: 无法启动 ffmpeg - {ex.Message}");
                return false;
            }
        }

        static string GetFfmpegPath()
        {
            string baseDir = AppContext.BaseDirectory;
            string ffmpegRelativePath = string.Empty;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ffmpegRelativePath = Path.Combine("ffmpeg", "windows", "ffmpeg.exe");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ffmpegRelativePath = Path.Combine("ffmpeg", "linux", "ffmpeg");
            }
            else
            {
                // 可以根据需要添加更多平台支持
                return null;
            }

            string ffmpegFullPath = Path.Combine(baseDir, ffmpegRelativePath);

            if (!File.Exists(ffmpegFullPath))
            {
                return null;
            }

            // 对 Linux 可执行文件赋予执行权限
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    Process chmod = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "chmod",
                            Arguments = $"+x \"{ffmpegFullPath}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    chmod.Start();
                    chmod.WaitForExit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"警告: 无法设置 ffmpeg 执行权限 - {ex.Message}");
                }
            }

            return ffmpegFullPath;
        }
    }
}
