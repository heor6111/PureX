using System.Diagnostics;

namespace PureX.Core.Services;

public class ToolboxService
{
    private readonly string _ffmpegPath;

    public ToolboxService()
    {
        var ffmpegDir = FFmpegResourceManager.FindFFmpegInCommonLocations();
        _ffmpegPath = ffmpegDir ?? string.Empty;
    }

    public async Task<(bool Success, string Message)> ExtractAudioAsync(
        string inputPath, 
        string outputPath, 
        string audioFormat = "mp3",
        int? bitrate = null,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(inputPath))
        {
            return (false, "输入文件不存在");
        }

        var ffmpegExe = Path.Combine(_ffmpegPath, "ffmpeg.exe");
        if (!File.Exists(ffmpegExe))
        {
            return (false, "FFmpeg未找到");
        }

        var args = $"-i \"{inputPath}\" -vn";
        
        if (bitrate.HasValue)
        {
            args += $" -b:a {bitrate}k";
        }
        
        args += $" -y \"{outputPath}\"";

        return await ExecuteFFmpegAsync(ffmpegExe, args, progress, cancellationToken);
    }

    public async Task<(bool Success, string Message)> MergeAudioAsync(
        List<string> audioFiles,
        string outputPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (audioFiles == null || audioFiles.Count == 0)
        {
            return (false, "没有要合并的音频文件");
        }

        var ffmpegExe = Path.Combine(_ffmpegPath, "ffmpeg.exe");
        if (!File.Exists(ffmpegExe))
        {
            return (false, "FFmpeg未找到");
        }

        var listFile = Path.Combine(Path.GetTempPath(), $"concat_{Guid.NewGuid():N}.txt");
        try
        {
            var lines = audioFiles.Select(f => $"file '{f}'");
            await File.WriteAllLinesAsync(listFile, lines, cancellationToken);

            var args = $"-f concat -safe 0 -i \"{listFile}\" -c copy -y \"{outputPath}\"";
            return await ExecuteFFmpegAsync(ffmpegExe, args, progress, cancellationToken);
        }
        finally
        {
            if (File.Exists(listFile))
            {
                File.Delete(listFile);
            }
        }
    }

    public async Task<(bool Success, string Message)> RepairFileAsync(
        string inputPath,
        string outputPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(inputPath))
        {
            return (false, "输入文件不存在");
        }

        var ffmpegExe = Path.Combine(_ffmpegPath, "ffmpeg.exe");
        if (!File.Exists(ffmpegExe))
        {
            return (false, "FFmpeg未找到");
        }

        var args = $"-i \"{inputPath}\" -c copy -y \"{outputPath}\"";
        return await ExecuteFFmpegAsync(ffmpegExe, args, progress, cancellationToken);
    }

    public async Task<(bool Success, string Message)> TrimMediaAsync(
        string inputPath,
        string outputPath,
        TimeSpan startTime,
        TimeSpan endTime,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(inputPath))
        {
            return (false, "输入文件不存在");
        }

        var ffmpegExe = Path.Combine(_ffmpegPath, "ffmpeg.exe");
        if (!File.Exists(ffmpegExe))
        {
            return (false, "FFmpeg未找到");
        }

        var duration = endTime - startTime;
        var args = $"-ss {startTime:hh\\:mm\\:ss\\.fff} -i \"{inputPath}\" -t {duration:hh\\:mm\\:ss\\.fff} -c copy -y \"{outputPath}\"";
        return await ExecuteFFmpegAsync(ffmpegExe, args, progress, cancellationToken);
    }

    public async Task<(bool Success, string Message)> CropVideoAsync(
        string inputPath,
        string outputPath,
        int x, int y, int width, int height,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(inputPath))
        {
            return (false, "输入文件不存在");
        }

        var ffmpegExe = Path.Combine(_ffmpegPath, "ffmpeg.exe");
        if (!File.Exists(ffmpegExe))
        {
            return (false, "FFmpeg未找到");
        }

        var args = $"-i \"{inputPath}\" -vf \"crop={width}:{height}:{x}:{y}\" -c:a copy -y \"{outputPath}\"";
        return await ExecuteFFmpegAsync(ffmpegExe, args, progress, cancellationToken);
    }

    public async Task<(bool Success, string Message)> JoinMediaAsync(
        List<string> mediaFiles,
        string outputPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (mediaFiles == null || mediaFiles.Count == 0)
        {
            return (false, "没有要合并的媒体文件");
        }

        var ffmpegExe = Path.Combine(_ffmpegPath, "ffmpeg.exe");
        if (!File.Exists(ffmpegExe))
        {
            return (false, "FFmpeg未找到");
        }

        var listFile = Path.Combine(Path.GetTempPath(), $"concat_{Guid.NewGuid():N}.txt");
        try
        {
            var lines = mediaFiles.Select(f => $"file '{f}'");
            await File.WriteAllLinesAsync(listFile, lines, cancellationToken);

            var args = $"-f concat -safe 0 -i \"{listFile}\" -c copy -y \"{outputPath}\"";
            return await ExecuteFFmpegAsync(ffmpegExe, args, progress, cancellationToken);
        }
        finally
        {
            if (File.Exists(listFile))
            {
                File.Delete(listFile);
            }
        }
    }

    public async Task<MediaInfo?> GetMediaInfoAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        var ffprobeExe = Path.Combine(_ffmpegPath, "ffprobe.exe");
        if (!File.Exists(ffprobeExe))
        {
            return null;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = ffprobeExe,
                Arguments = $"-v quiet -print_format json -show_format -show_streams \"{filePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return null;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return ParseMediaInfo(output);
        }
        catch
        {
            return null;
        }
    }

    private MediaInfo? ParseMediaInfo(string json)
    {
        try
        {
            var info = new MediaInfo();
            var jsonDoc = System.Text.Json.JsonDocument.Parse(json);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("format", out var format))
            {
                if (format.TryGetProperty("duration", out var duration))
                {
                    info.Duration = TimeSpan.FromSeconds(double.Parse(duration.GetString() ?? "0"));
                }
                if (format.TryGetProperty("bit_rate", out var bitRate))
                {
                    info.BitRate = int.Parse(bitRate.GetString() ?? "0");
                }
            }

            if (root.TryGetProperty("streams", out var streams))
            {
                foreach (var stream in streams.EnumerateArray())
                {
                    var codecType = stream.GetProperty("codec_type").GetString();
                    if (codecType == "video")
                    {
                        info.HasVideo = true;
                        if (stream.TryGetProperty("width", out var width))
                        {
                            info.Width = width.GetInt32();
                        }
                        if (stream.TryGetProperty("height", out var height))
                        {
                            info.Height = height.GetInt32();
                        }
                    }
                    else if (codecType == "audio")
                    {
                        info.HasAudio = true;
                        if (stream.TryGetProperty("sample_rate", out var sampleRate))
                        {
                            info.SampleRate = int.Parse(sampleRate.GetString() ?? "0");
                        }
                        if (stream.TryGetProperty("channels", out var channels))
                        {
                            info.Channels = channels.GetInt32();
                        }
                    }
                }
            }

            return info;
        }
        catch
        {
            return null;
        }
    }

    private async Task<(bool Success, string Message)> ExecuteFFmpegAsync(
        string ffmpegExe,
        string args,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegExe,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var errorOutput = new List<string>();

            _ = Task.Run(async () =>
            {
                while (!process.StandardError.EndOfStream)
                {
                    var line = await process.StandardError.ReadLineAsync(cancellationToken);
                    if (!string.IsNullOrEmpty(line))
                    {
                        errorOutput.Add(line);
                        progress?.Report(0.5);
                    }
                }
            }, cancellationToken);

            await process.WaitForExitAsync(cancellationToken);
            
            int exitCode = -1;
            try
            {
                exitCode = process.ExitCode;
            }
            catch
            {
            }

            if (exitCode == 0)
            {
                progress?.Report(1.0);
                return (true, "操作成功");
            }
            else
            {
                var errorMsg = errorOutput.Count > 0 
                    ? string.Join("\n", errorOutput.TakeLast(5))
                    : "未知错误";
                return (false, $"操作失败: {errorMsg}");
            }
        }
        catch (OperationCanceledException)
        {
            return (false, "操作已取消");
        }
        catch (Exception ex)
        {
            return (false, $"操作失败: {ex.Message}");
        }
    }
}

public class MediaInfo
{
    public TimeSpan Duration { get; set; }
    public int BitRate { get; set; }
    public bool HasVideo { get; set; }
    public bool HasAudio { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int SampleRate { get; set; }
    public int Channels { get; set; }
    public string DurationFormatted => Duration.ToString(@"hh\:mm\:ss");
}
