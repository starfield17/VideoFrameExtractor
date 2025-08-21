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
        // Define supported video file extensions
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

            // Check if input path exists
            if (!Directory.Exists(inputPath) && !File.Exists(inputPath))
            {
                Console.WriteLine($"Error: Path '{inputPath}' does not exist.");
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
                    Console.WriteLine($"Error: Unknown command '{command}'.");
                    ShowUsage();
                    return 1;
            }

            return 0;
        }

        static void ShowUsage()
        {
            Console.WriteLine("==============================================");
            Console.WriteLine("          Video Processing Tool Guide");
            Console.WriteLine("==============================================");
            Console.WriteLine("Usage: VideoFrameExtractor <command> <path>");
            Console.WriteLine("");
            Console.WriteLine("Command Description:");
            Console.WriteLine("  getframe      Extract video frames");
            Console.WriteLine("  getmp4        Convert non-MP4 videos to MP4 format");
            Console.WriteLine("  mergevideo    Merge video files in directory (recursively process each subdirectory)");
            Console.WriteLine("");
            Console.WriteLine("Parameter Description:");
            Console.WriteLine("  <path>  Specify a single video file or directory containing video files.");
            Console.WriteLine("");
            Console.WriteLine("Supported Video Formats:");
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
                Console.WriteLine($"Detected directory: {inputPath}");
                Console.WriteLine("Starting to process video files in directory...");
                ProcessDirectory(inputPath, ProcessVideoForFrameExtraction);
                Console.WriteLine("All video files processed.");
            }
            else if (File.Exists(inputPath))
            {
                if (IsVideoFile(inputPath))
                {
                    Console.WriteLine($"Processing single video file: {inputPath}");
                    bool success = ProcessVideoForFrameExtraction(inputPath);
                    if (success)
                    {
                        Console.WriteLine("Video file processed successfully.");
                    }
                    else
                    {
                        Console.WriteLine("Video file processing failed.");
                    }
                }
                else
                {
                    Console.WriteLine($"Error: File '{inputPath}' is not a supported video format.");
                }
            }
            else
            {
                Console.WriteLine($"Error: '{inputPath}' is neither a file nor a directory.");
            }
        }

        static bool ProcessVideoForFrameExtraction(string videoPath)
        {
            if (!File.Exists(videoPath))
            {
                Console.WriteLine($"Error: File '{videoPath}' does not exist.");
                return false;
            }

            string videoDir = Path.GetDirectoryName(videoPath);
            string videoFilename = Path.GetFileName(videoPath);
            string videoName = Path.GetFileNameWithoutExtension(videoPath);

            string outputDir = Path.Combine(videoDir, $"{videoName}_frames");
            Directory.CreateDirectory(outputDir);

            // Get current operating system
            string ffmpegPath = GetFfmpegPath();

            if (string.IsNullOrEmpty(ffmpegPath))
            {
                Console.WriteLine("Error: Cannot find ffmpeg for current operating system.");
                return false;
            }

            // Build output file path
            string outputPattern = Path.Combine(outputDir, $"{videoName}_%d.png");

            // Set ffmpeg command parameters
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
                        Console.WriteLine($"Frames saved to directory: {outputDir}");
                        return true;
                    }
                    else
                    {
                        string error = process.StandardError.ReadToEnd();
                        Console.WriteLine($"Error: ffmpeg processing failed - {error}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Cannot start ffmpeg - {ex.Message}");
                return false;
            }
        }

        #endregion

        #region GetMp4 Functionality

        static void HandleGetMp4(string inputPath)
        {
            if (Directory.Exists(inputPath))
            {
                Console.WriteLine($"Detected directory: {inputPath}");
                Console.WriteLine("Starting to convert non-MP4 video files in directory...");
                ProcessDirectory(inputPath, ConvertVideoToMp4);
                Console.WriteLine("All video files converted.");
            }
            else if (File.Exists(inputPath))
            {
                if (IsVideoFile(inputPath))
                {
                    Console.WriteLine($"Processing single video file: {inputPath}");
                    bool success = ConvertVideoToMp4(inputPath);
                    if (success)
                    {
                        Console.WriteLine("Video file converted successfully.");
                    }
                    else
                    {
                        Console.WriteLine("Video file conversion failed.");
                    }
                }
                else
                {
                    Console.WriteLine($"Error: File '{inputPath}' is not a supported video format.");
                }
            }
            else
            {
                Console.WriteLine($"Error: '{inputPath}' is neither a file nor a directory.");
            }
        }

        static bool ConvertVideoToMp4(string videoPath)
        {
            if (!File.Exists(videoPath))
            {
                Console.WriteLine($"Error: File '{videoPath}' does not exist.");
                return false;
            }

            string videoDir = Path.GetDirectoryName(videoPath);
            string videoFilename = Path.GetFileName(videoPath);
            string videoName = Path.GetFileNameWithoutExtension(videoPath);
            string videoExtension = Path.GetExtension(videoPath).TrimStart('.').ToLower();

            // If already in mp4 format, skip conversion
            if (videoExtension == "mp4")
            {
                Console.WriteLine($"File '{videoPath}' is already in MP4 format, skipping conversion.");
                return true;
            }

            string outputPath = Path.Combine(videoDir, $"{videoName}.mp4");

            // Get current operating system
            string ffmpegPath = GetFfmpegPath();

            if (string.IsNullOrEmpty(ffmpegPath))
            {
                Console.WriteLine("Error: Cannot find ffmpeg for current operating system.");
                return false;
            }

            // Set ffmpeg command parameters
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
                        Console.WriteLine($"Converted '{videoPath}' to '{outputPath}'");
                        return true;
                    }
                    else
                    {
                        string error = process.StandardError.ReadToEnd();
                        Console.WriteLine($"Error: ffmpeg conversion failed - {error}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Cannot start ffmpeg - {ex.Message}");
                return false;
            }
        }

        #endregion

        #region MergeVideo Functionality

        static void HandleMergeVideo(string inputPath)
        {
            if (Directory.Exists(inputPath))
            {
                Console.WriteLine($"Detected directory: {inputPath}");
                Console.WriteLine("Starting to recursively merge video files in directory...");
                // Use recursion to traverse all subdirectories
                foreach (var dir in Directory.EnumerateDirectories(inputPath, "*", SearchOption.AllDirectories).Prepend(inputPath))
                {
                    Console.WriteLine($"Processing directory: {dir}");
                    bool success = MergeVideosInDirectory(dir);
                    if (!success)
                    {
                        Console.WriteLine($"Merge failed: {dir}");
                    }
                }
                Console.WriteLine("All video files in all directories merged.");
            }
            else
            {
                Console.WriteLine($"Error: 'mergevideo' command requires a directory as input.");
            }
        }

        static bool MergeVideosInDirectory(string directoryPath)
        {
            // Find video files in directory and sort by name
            var videoFiles = Directory.EnumerateFiles(directoryPath)
                                      .Where(file => IsVideoFile(file))
                                      .OrderBy(file => file)
                                      .ToList();

            int videoCount = videoFiles.Count;

            if (videoCount < 2)
            {
                Console.WriteLine($"Less than 2 video files in directory '{directoryPath}', skipping merge.");
                return true; // Not an error, just no files to merge
            }

            string mergedVideoPath = Path.Combine(directoryPath, "merged_output.mp4");

            // Create temporary file list
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

                // Get current operating system
                string ffmpegPath = GetFfmpegPath();

                if (string.IsNullOrEmpty(ffmpegPath))
                {
                    Console.WriteLine("Error: Cannot find ffmpeg for current operating system.");
                    return false;
                }

                // Set ffmpeg command parameters
                var startInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-f concat -safe 0 -i \"{tempFileListPath}\" -c copy \"{mergedVideoPath}\" -hide_banner -loglevel error",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Execute merge command
                using (var process = Process.Start(startInfo))
                {
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        Console.WriteLine($"Merged {videoCount} videos in directory '{directoryPath}' into '{mergedVideoPath}'");
                        return true;
                    }
                    else
                    {
                        string error = process.StandardError.ReadToEnd();
                        Console.WriteLine($"Error: ffmpeg merge failed - {error}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Error while merging videos - {ex.Message}");
                return false;
            }
            finally
            {
                // Delete temporary file list
                if (File.Exists(tempFileListPath))
                {
                    try
                    {
                        File.Delete(tempFileListPath);
                    }
                    catch
                    {
                        // Ignore delete failure errors
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
                    Console.WriteLine($"Processing video file: {file}");
                    bool success = processFunc(file);
                    if (!success)
                    {
                        Console.WriteLine($"Processing failed: {file}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Error while processing directory - {ex.Message}");
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
                // Can add support for more platforms as needed
                return null;
            }

            string ffmpegFullPath = Path.Combine(baseDir, ffmpegRelativePath);

            if (!File.Exists(ffmpegFullPath))
            {
                return null;
            }

            // Grant execute permission to ffmpeg executable for non-Windows systems
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
                    Console.WriteLine($"Warning: Cannot set ffmpeg execute permission - {ex.Message}");
                }
            }

            return ffmpegFullPath;
        }

        #endregion
    }
}
