#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace VideoFrameExtractor
{
    /// <summary>
    /// Entry point and orchestration. Implements a thin CLI and delegates real work
    /// to small, focused helpers to keep cyclomatic complexity low.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Cli.PrintUsage();
                return 1;
            }

            var command = args[0].Trim().ToLowerInvariant();
            var inputPath = args[1].Trim();

            if (!File.Exists(inputPath) && !Directory.Exists(inputPath))
            {
                Console.Error.WriteLine($"Error: Path '{inputPath}' does not exist.");
                return 1;
            }

            try
            {
                var ffmpeg = FfmpegLocator.TryLocate();
                if (ffmpeg is null)
                {
                    Console.Error.WriteLine("Error: Could not locate ffmpeg for the current OS.");
                    return 1;
                }

                switch (command)
                {
                    case "getframe":
                        return new FrameExtractor(ffmpeg).Run(inputPath);
                    case "getmp4":
                        return new Mp4Converter(ffmpeg).Run(inputPath);
                    case "mergevideo":
                        return new VideoMerger(ffmpeg).Run(inputPath);
                    case "help":
                    case "-h":
                    case "--help":
                        Cli.PrintUsage();
                        return 0;
                    default:
                        Console.Error.WriteLine($"Error: Unknown command '{command}'.\n");
                        Cli.PrintUsage();
                        return 1;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected error: {ex.Message}");
                return 1;
            }
        }
    }

    /// <summary>
    /// Command-line UI helper.
    /// </summary>
    internal static class Cli
    {
        public static void PrintUsage()
        {
            Console.WriteLine(
                "VideoFrameExtractor\n" +
                "Usage: VideoFrameExtractor <command> <path>\n\n" +
                "Commands:\n" +
                "  getframe     Extract 1 frame/sec as PNGs (dir or single file).\n" +
                "  getmp4       Convert videos to MP4 using H.264 + AAC.\n" +
                "  mergevideo   Concatenate videos in each directory into merged_output.mp4.\n\n" +
                "Examples:\n" +
                "  VideoFrameExtractor getframe ./clips\n" +
                "  VideoFrameExtractor getmp4 video.avi\n" +
                "  VideoFrameExtractor mergevideo ./datasets\n");
        }
    }

    /// <summary>
    /// Filesystem helpers and constants.
    /// </summary>
    internal static class Fs
    {
        private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".avi", ".mkv", ".mov", ".flv", ".wmv", ".webm"
        };

        public static bool IsVideo(string path) => VideoExtensions.Contains(Path.GetExtension(path));

        public static IEnumerable<string> EnumerateVideos(string root)
        {
            if (File.Exists(root))
            {
                if (IsVideo(root)) yield return Path.GetFullPath(root);
                yield break;
            }

            foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            {
                if (IsVideo(file)) yield return Path.GetFullPath(file);
            }
        }

        public static IEnumerable<string> EnumerateImmediateVideos(string directory)
        {
            foreach (var file in Directory.EnumerateFiles(directory))
            {
                if (IsVideo(file)) yield return Path.GetFullPath(file);
            }
        }
    }

    /// <summary>
    /// Resolves ffmpeg path for the current platform and ensures non-Windows binaries are executable.
    /// </summary>
    internal static class FfmpegLocator
    {
        public static string? TryLocate()
        {
            var baseDir = AppContext.BaseDirectory;
            string relative = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine("ffmpeg", "windows", "ffmpeg.exe")
                : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? Path.Combine("ffmpeg", "macos", "ffmpeg")
                    : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                        ? Path.Combine("ffmpeg", "linux", "ffmpeg")
                        : string.Empty;

            if (string.IsNullOrEmpty(relative)) return null;

            var full = Path.Combine(baseDir, relative);
            if (!File.Exists(full)) return null;

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                TryChmodX(full);
            }

            return full;
        }

        private static void TryChmodX(string path)
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+x \"{path}\"",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                p?.WaitForExit();
            }
            catch
            {
                // non-fatal on platforms without chmod
            }
        }
    }

    /// <summary>
    /// Thin wrapper for executing ffmpeg commands.
    /// </summary>
    internal sealed class Ffmpeg
    {
        public Ffmpeg(string path) => PathToExe = path;
        private string PathToExe { get; }

        public int Run(string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = PathToExe,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc is null) throw new InvalidOperationException("Failed to start ffmpeg process.");
            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                string error = proc.StandardError.ReadToEnd();
                throw new InvalidOperationException($"ffmpeg failed (code {proc.ExitCode}). Error: {error}");
            }

            return proc.ExitCode;
        }
    }

    /// <summary>
    /// Base class with shared helpers for operations that accept either a file or directory input.
    /// </summary>
    internal abstract class Operation
    {
        protected Operation(string ffmpegPath)
        {
            Ffmpeg = new Ffmpeg(ffmpegPath);
        }

        protected Ffmpeg Ffmpeg { get; }

        public int Run(string inputPath)
        {
            if (File.Exists(inputPath))
            {
                return HandleFile(inputPath) ? 0 : 1;
            }

            int failures = 0;
            foreach (var file in Fs.EnumerateVideos(inputPath))
            {
                Console.WriteLine($"Processing: {file}");
                if (!HandleFile(file))
                {
                    Console.Error.WriteLine($"Failed: {file}");
                    failures++;
                }
            }

            return failures == 0 ? 0 : 1;
        }

        protected abstract bool HandleFile(string path);
    }

    /// <summary>
    /// Extracts 1 FPS PNG frames into <videoName>_frames directory.
    /// </summary>
    internal sealed class FrameExtractor : Operation
    {
        public FrameExtractor(string ffmpegPath) : base(ffmpegPath) { }

        protected override bool HandleFile(string path)
        {
            if (!Fs.IsVideo(path))
            {
                Console.Error.WriteLine($"Skip (not a supported video): {path}");
                return false;
            }

            var dir = Path.GetDirectoryName(path)!;
            var name = Path.GetFileNameWithoutExtension(path);
            var outDir = Path.Combine(dir, $"{name}_frames");
            Directory.CreateDirectory(outDir);

            var outputPattern = Path.Combine(outDir, $"{name}_%d.png");
            var args = $"-i \"{path}\" -vf fps=1 \"{outputPattern}\" -hide_banner -loglevel error";

            try
            {
                Ffmpeg.Run(args);
                Console.WriteLine($"Frames saved to: {outDir}");
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error extracting frames: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Converts any non-MP4 video into H.264/AAC MP4 next to the source.
    /// </summary>
    internal sealed class Mp4Converter : Operation
    {
        public Mp4Converter(string ffmpegPath) : base(ffmpegPath) { }

        protected override bool HandleFile(string path)
        {
            if (!Fs.IsVideo(path))
            {
                Console.Error.WriteLine($"Skip (not a supported video): {path}");
                return false;
            }

            if (string.Equals(Path.GetExtension(path), ".mp4", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Already MP4, skipping: {path}");
                return true;
            }

            var dir = Path.GetDirectoryName(path)!;
            var name = Path.GetFileNameWithoutExtension(path);
            var outPath = Path.Combine(dir, name + ".mp4");
            var args = $"-i \"{path}\" -c:v libx264 -c:a aac \"{outPath}\" -hide_banner -loglevel error";

            try
            {
                Ffmpeg.Run(args);
                Console.WriteLine($"Converted to: {outPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error converting to mp4: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Concatenates videos within each directory into a single merged_output.mp4.
    /// If inputPath is a directory, it processes that directory AND each subdirectory.
    /// If inputPath is a file, it processes only that file's directory.
    /// </summary>
    internal sealed class VideoMerger
    {
        private readonly Ffmpeg _ffmpeg;
        public VideoMerger(string ffmpegPath) => _ffmpeg = new Ffmpeg(ffmpegPath);

        public int Run(string inputPath)
        {
            var directories = Directory.Exists(inputPath)
                ? Directory.EnumerateDirectories(inputPath, "*", SearchOption.AllDirectories).Prepend(inputPath)
                : new[] { Path.GetDirectoryName(Path.GetFullPath(inputPath))! };

            int failures = 0;
            foreach (var dir in directories)
            {
                if (!MergeDirectory(dir)) failures++;
            }

            return failures == 0 ? 0 : 1;
        }

        private bool MergeDirectory(string directory)
        {
            try
            {
                var videos = Fs.EnumerateImmediateVideos(directory)
                                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                                .ToList();

                if (videos.Count < 2)
                {
                    Console.WriteLine($"Skip merge (need >=2 videos): {directory}");
                    return true; // Not an error
                }

                var listPath = Path.Combine(directory, "file_list.txt");
                WriteConcatList(listPath, videos);

                var merged = Path.Combine(directory, "merged_output.mp4");
                var args = $"-f concat -safe 0 -i \"{listPath}\" -c copy \"{merged}\" -hide_banner -loglevel error";

                try
                {
                    _ffmpeg.Run(args);
                    Console.WriteLine($"Merged {videos.Count} files -> {merged}");
                    return true;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error merging in '{directory}': {ex.Message}");
                    return false;
                }
                finally
                {
                    SafeDelete(listPath);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error preparing merge in '{directory}': {ex.Message}");
                return false;
            }
        }

        private static void WriteConcatList(string listPath, IEnumerable<string> videos)
        {
            using var writer = new StreamWriter(listPath);
            foreach (var v in videos)
            {
                // ffmpeg concat demuxer expects paths quoted like: file 'absolutePath'
                var escaped = v.Replace("'", "'\\''");
                writer.WriteLine($"file '{escaped}'");
            }
        }

        private static void SafeDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { /* ignore */ }
        }
    }
}
