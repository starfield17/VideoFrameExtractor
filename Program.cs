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
        static readonly string[] VIDEO_EXTENSIONS = { "mp4", "avi", "mkv", "mov", "flv", "wmv", "webm" };

        static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                ShowUsage();
                return 1;
            }

            string command = args[0].ToLower();
            string inputPath = args[1];

            // 检查输入路径是否存在
            if (!Directory.Exists(inputPath) && !File.Exists(inputPath))
            {
                Console.WriteLine($"错误: 路径 '{inputPath}' 不存在。");
                return 1;
            }

            switch (command)
            {
                case "getframe":
                    HandleGetFrame(inputPath);
                    break;
                case "getmp4":
                    HandleGetMp4(inputPath);
                    break;
                case "mergevideo":
                    HandleMergeVideo(inputPath);
                    break;
                default:
                    Console.WriteLine($"错误: 未知命令 '{command}'。");
                    ShowUsage();
                    return 1;
            }

            return 0;
        }

        static void ShowUsage()
        {
            Console.WriteLine("==============================================");
            Console.WriteLine("          视频处理工具使用指南");
            Console.WriteLine("==============================================");
            Console.WriteLine("用法: VideoFrameExtractor <command> <path>");
            Console.WriteLine("");
            Console.WriteLine("命令说明:");
            Console.WriteLine("  getframe      提取视频帧");
            Console.WriteLine("  getmp4        将非 MP4 视频转换为 MP4 格式");
            Console.WriteLine("  mergevideo    合并目录中的视频文件（递归处理每个子目录）");
            Console.WriteLine("");
            Console.WriteLine("参数说明:");
            Console.WriteLine("  <path>  指定单个视频文件或包含视频文件的目录。");
            Console.WriteLine("");
            Console.WriteLine("支持的视频格式:");
            Console.WriteLine("  mp4, avi, mkv, mov, flv, wmv, webm");
            Console.WriteLine("==============================================");
        }

        static bool IsVideoFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).TrimStart('.').ToLower();
            return VIDEO_EXTENSIONS.Contains(extension);
        }

        #region GetFrame Functionality

        static void HandleGetFrame(string inputPath)
        {
            if (Directory.Exists(inputPath))
            {
                Console.WriteLine($"检测到目录: {inputPath}");
                Console.WriteLine("开始处理目录中的视频文件...");
                ProcessDirectory(inputPath, ProcessVideoForFrameExtraction);
                Console.WriteLine("所有视频文件处理完成。");
            }
            else if (File.Exists(inputPath))
            {
                if (IsVideoFile(inputPath))
                {
                    Console.WriteLine($"处理单个视频文件: {inputPath}");
                    bool success = ProcessVideoForFrameExtraction(inputPath);
                    if (success)
                    {
                        Console.WriteLine("视频文件处理完成。");
                    }
                    else
                    {
                        Console.WriteLine("视频文件处理失败。");
                    }
                }
                else
                {
                    Console.WriteLine($"错误: 文件 '{inputPath}' 不是支持的视频格式。");
                }
            }
            else
            {
                Console.WriteLine($"错误: '{inputPath}' 既不是文件也不是目录。");
            }
        }

        static bool ProcessVideoForFrameExtraction(string videoPath)
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

        #endregion

        #region GetMp4 Functionality

        static void HandleGetMp4(string inputPath)
        {
            if (Directory.Exists(inputPath))
            {
                Console.WriteLine($"检测到目录: {inputPath}");
                Console.WriteLine("开始转换目录中的非 MP4 视频文件...");
                ProcessDirectory(inputPath, ConvertVideoToMp4);
                Console.WriteLine("所有视频文件转换完成。");
            }
            else if (File.Exists(inputPath))
            {
                if (IsVideoFile(inputPath))
                {
                    Console.WriteLine($"处理单个视频文件: {inputPath}");
                    bool success = ConvertVideoToMp4(inputPath);
                    if (success)
                    {
                        Console.WriteLine("视频文件转换完成。");
                    }
                    else
                    {
                        Console.WriteLine("视频文件转换失败。");
                    }
                }
                else
                {
                    Console.WriteLine($"错误: 文件 '{inputPath}' 不是支持的视频格式。");
                }
            }
            else
            {
                Console.WriteLine($"错误: '{inputPath}' 既不是文件也不是目录。");
            }
        }

        static bool ConvertVideoToMp4(string videoPath)
        {
            if (!File.Exists(videoPath))
            {
                Console.WriteLine($"错误: 文件 '{videoPath}' 不存在。");
                return false;
            }

            string videoDir = Path.GetDirectoryName(videoPath);
            string videoFilename = Path.GetFileName(videoPath);
            string videoName = Path.GetFileNameWithoutExtension(videoPath);
            string videoExtension = Path.GetExtension(videoPath).TrimStart('.').ToLower();

            // 如果已经是 mp4 格式，跳过转换
            if (videoExtension == "mp4")
            {
                Console.WriteLine($"文件 '{videoPath}' 已经是 MP4 格式，跳过转换。");
                return true;
            }

            string outputPath = Path.Combine(videoDir, $"{videoName}.mp4");

            // 获取当前操作系统
            string ffmpegPath = GetFfmpegPath();

            if (string.IsNullOrEmpty(ffmpegPath))
            {
                Console.WriteLine("错误: 无法找到适用于当前操作系统的 ffmpeg。");
                return false;
            }

            // 设置 ffmpeg 命令参数
            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-i \"{videoPath}\" -c:v libx264 -c:a aac \"{outputPath}\" -hide_banner -loglevel error",
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
                        Console.WriteLine($"已将 '{videoPath}' 转换为 '{outputPath}'");
                        return true;
                    }
                    else
                    {
                        string error = process.StandardError.ReadToEnd();
                        Console.WriteLine($"错误: ffmpeg 转换失败 - {error}");
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

        #endregion

        #region MergeVideo Functionality

        static void HandleMergeVideo(string inputPath)
        {
            if (Directory.Exists(inputPath))
            {
                Console.WriteLine($"检测到目录: {inputPath}");
                Console.WriteLine("开始递归合并目录中的视频文件...");
                // 使用递归遍历所有子目录
                foreach (var dir in Directory.EnumerateDirectories(inputPath, "*", SearchOption.AllDirectories).Prepend(inputPath))
                {
                    Console.WriteLine($"处理目录: {dir}");
                    bool success = MergeVideosInDirectory(dir);
                    if (!success)
                    {
                        Console.WriteLine($"合并失败: {dir}");
                    }
                }
                Console.WriteLine("所有目录中的视频文件合并完成。");
            }
            else
            {
                Console.WriteLine($"错误: 'mergevideo' 命令需要一个目录作为输入。");
            }
        }

        static bool MergeVideosInDirectory(string directoryPath)
        {
            // 查找目录中的视频文件，并按名称排序
            var videoFiles = Directory.EnumerateFiles(directoryPath)
                                      .Where(file => IsVideoFile(file))
                                      .OrderBy(file => file)
                                      .ToList();

            int videoCount = videoFiles.Count;

            if (videoCount < 2)
            {
                Console.WriteLine($"在目录 '{directoryPath}' 中的视频文件少于2个，跳过合并。");
                return true; // 不是错误，只是没有需要合并的文件
            }

            string mergedVideoPath = Path.Combine(directoryPath, "merged_output.mp4");

            // 创建临时文件列表
            string tempFileListPath = Path.Combine(directoryPath, "file_list.txt");
            try
            {
                using (var writer = new StreamWriter(tempFileListPath))
                {
                    foreach (var video in videoFiles)
                    {
                        string absolutePath = Path.GetFullPath(video).Replace("'", "'\\''");
                        writer.WriteLine($"file '{absolutePath}'");
                    }
                }

                // 获取当前操作系统
                string ffmpegPath = GetFfmpegPath();

                if (string.IsNullOrEmpty(ffmpegPath))
                {
                    Console.WriteLine("错误: 无法找到适用于当前操作系统的 ffmpeg。");
                    return false;
                }

                // 设置 ffmpeg 命令参数
                var startInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-f concat -safe 0 -i \"{tempFileListPath}\" -c copy \"{mergedVideoPath}\" -hide_banner -loglevel error",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // 执行合并命令
                using (var process = Process.Start(startInfo))
                {
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        Console.WriteLine($"已合并目录 '{directoryPath}' 中的 {videoCount} 个视频为 '{mergedVideoPath}'");
                        return true;
                    }
                    else
                    {
                        string error = process.StandardError.ReadToEnd();
                        Console.WriteLine($"错误: ffmpeg 合并失败 - {error}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: 合并视频时出错 - {ex.Message}");
                return false;
            }
            finally
            {
                // 删除临时文件列表
                if (File.Exists(tempFileListPath))
                {
                    try
                    {
                        File.Delete(tempFileListPath);
                    }
                    catch
                    {
                        // 忽略删除失败的错误
                    }
                }
            }
        }

        #endregion

        #region Common Methods

        static void ProcessDirectory(string directoryPath, Func<string, bool> processFunc)
        {
            try
            {
                var files = Directory.EnumerateFiles(directoryPath, "*.*", SearchOption.AllDirectories)
                                     .Where(file => IsVideoFile(file));

                foreach (var file in files)
                {
                    Console.WriteLine($"处理视频文件: {file}");
                    bool success = processFunc(file);
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
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                ffmpegRelativePath = Path.Combine("ffmpeg", "macos", "ffmpeg");
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

            // 对非 Windows 系统的 ffmpeg 可执行文件赋予执行权限
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    var chmod = new Process
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

        #endregion
    }
}
