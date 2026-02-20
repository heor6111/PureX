using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using PureX.Core.Models;

namespace PureX.Core.Converters;

public class FFmpegConverter : IConverter
{
    private readonly string _ffmpegPath;
    private readonly string _ffprobePath;

    public FFmpegConverter(string ffmpegDirectory)
    {
        _ffmpegPath = Path.Combine(ffmpegDirectory, "ffmpeg.exe");
        _ffprobePath = Path.Combine(ffmpegDirectory, "ffprobe.exe");
        
        if (!File.Exists(_ffmpegPath))
        {
            throw new FileNotFoundException($"FFmpeg not found at: {_ffmpegPath}");
        }
    }

    public async Task<ConversionResult> ConvertAsync(string inputPath, string outputPath, 
        ConversionOptions? options = null, 
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var fileSize = new FileInfo(inputPath).Length;
        
        try
        {
            if (!File.Exists(inputPath))
            {
                return ConversionResult.Failed("转换失败: 找不到输入文件");
            }

            options ??= new ConversionOptions();
            
            var inputExt = Path.GetExtension(inputPath).ToLowerInvariant();
            var outputExt = Path.GetExtension(outputPath).ToLowerInvariant();

            if (options.OverwriteExisting && File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            var duration = await GetVideoDurationAsync(inputPath);
            var arguments = BuildFFmpegArguments(inputPath, outputPath, inputExt, outputExt, options);
            
            var result = await ExecuteFFmpegAsync(arguments, duration, fileSize, progress, cancellationToken);
            
            stopwatch.Stop();

            if (result.ExitCode == 0 && File.Exists(outputPath))
            {
                var originalSize = new FileInfo(inputPath).Length;
                var outputSize = new FileInfo(outputPath).Length;
                return ConversionResult.Succeeded(outputPath, stopwatch.Elapsed, originalSize, outputSize);
            }
            else
            {
                var errorMessage = ParseFFmpegError(result.Error, result.ExitCode);
                return ConversionResult.Failed(errorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            return ConversionResult.Failed("转换已取消");
        }
        catch (Exception ex)
        {
            return ConversionResult.Failed($"转换失败: {ex.Message}");
        }
    }

    private async Task<double?> GetVideoDurationAsync(string filePath)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ffprobePath,
                    Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{filePath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (double.TryParse(output.Trim(), out var duration))
            {
                return duration;
            }
        }
        catch
        {
        }

        return null;
    }

    private string BuildFFmpegArguments(string inputPath, string outputPath, 
        string inputExt, string outputExt, ConversionOptions options)
    {
        var args = new List<string>();

        if (options.TimeRange?.StartTime != null)
        {
            args.Add("-ss");
            args.Add(options.TimeRange.StartTime.Value.ToString(@"hh\:mm\:ss\.fff"));
        }

        args.Add("-i");
        args.Add($"\"{inputPath}\"");

        if (options.TimeRange?.Duration != null)
        {
            args.Add("-t");
            args.Add(options.TimeRange.Duration.Value.ToString(@"hh\:mm\:ss\.fff"));
        }
        else if (options.TimeRange?.EndTime != null && options.TimeRange.StartTime != null)
        {
            var duration = options.TimeRange.EndTime.Value - options.TimeRange.StartTime.Value;
            args.Add("-t");
            args.Add(duration.ToString(@"hh\:mm\:ss\.fff"));
        }

        var isVideoInput = MediaTypeHelper.IsSupportedVideoFormat(inputExt);
        var isAudioInput = MediaTypeHelper.IsSupportedAudioFormat(inputExt);
        var isVideoOutput = MediaTypeHelper.IsSupportedVideoFormat(outputExt);
        var isAudioOutput = MediaTypeHelper.IsSupportedAudioFormat(outputExt);

        if (isVideoInput && isVideoOutput)
        {
            args.AddRange(BuildVideoArgs(outputExt, options));
        }
        else if (isAudioInput && isAudioOutput)
        {
            args.AddRange(BuildAudioArgs(outputExt, options));
        }
        else if (isVideoInput && isAudioOutput)
        {
            args.Add("-vn");
            args.AddRange(BuildAudioArgs(outputExt, options));
        }

        if (!string.IsNullOrEmpty(options.CustomFFmpegArgs))
        {
            args.Add(options.CustomFFmpegArgs);
        }

        if (options.OverwriteExisting)
        {
            args.Add("-y");
        }

        args.Add($"\"{outputPath}\"");

        return string.Join(" ", args);
    }

    private List<string> BuildVideoArgs(string outputExt, ConversionOptions options)
    {
        var args = new List<string>();
        var video = options.Video ?? new VideoOptions();
        var audio = options.Audio ?? new AudioOptions();

        var codec = video.Codec ?? GetDefaultVideoCodec(outputExt);
        args.Add("-c:v");
        args.Add(codec);

        if (!string.IsNullOrEmpty(video.Preset) && (codec == "libx264" || codec == "libx265"))
        {
            args.Add("-preset");
            args.Add(video.Preset);
        }

        if (video.Quality.HasValue && (codec == "libx264" || codec == "libx265"))
        {
            args.Add("-crf");
            args.Add(video.Quality.Value.ToString());
        }
        else if (video.Bitrate.HasValue)
        {
            args.Add("-b:v");
            args.Add($"{video.Bitrate.Value}k");
        }
        else if (options.VideoBitrate.HasValue)
        {
            args.Add("-b:v");
            args.Add($"{options.VideoBitrate.Value}k");
        }
        else if (options.VideoQuality.HasValue)
        {
            var crf = Math.Clamp(23 - options.VideoQuality.Value / 4, 0, 51);
            args.Add("-crf");
            args.Add(crf.ToString());
        }

        if (video.Framerate.HasValue)
        {
            args.Add("-r");
            args.Add(video.Framerate.Value.ToString());
        }

        var hasResize = video.Width.HasValue || video.Height.HasValue || 
                        options.Width.HasValue || options.Height.HasValue;
        
        if (hasResize)
        {
            var width = video.Width ?? options.Width;
            var height = video.Height ?? options.Height;
            var keepAspect = video.KeepAspectRatio ?? true;

            if (width.HasValue || height.HasValue)
            {
                args.Add("-vf");
                var scaleFilter = BuildScaleFilter(width, height, keepAspect);
                
                var filters = new List<string> { scaleFilter };
                
                if (video.Rotation.HasValue && video.Rotation.Value != 0)
                {
                    filters.Add(BuildRotationFilter(video.Rotation.Value));
                }
                
                if (video.FlipHorizontal == true)
                {
                    filters.Add("hflip");
                }
                
                if (video.FlipVertical == true)
                {
                    filters.Add("vflip");
                }
                
                args.Add($"\"{string.Join(",", filters)}\"");
            }
        }
        else if (video.Rotation.HasValue || video.FlipHorizontal == true || video.FlipVertical == true)
        {
            var filters = new List<string>();
            
            if (video.Rotation.HasValue && video.Rotation.Value != 0)
            {
                filters.Add(BuildRotationFilter(video.Rotation.Value));
            }
            
            if (video.FlipHorizontal == true)
            {
                filters.Add("hflip");
            }
            
            if (video.FlipVertical == true)
            {
                filters.Add("vflip");
            }
            
            if (filters.Count > 0)
            {
                args.Add("-vf");
                args.Add($"\"{string.Join(",", filters)}\"");
            }
        }

        if (!string.IsNullOrEmpty(video.PixelFormat))
        {
            args.Add("-pix_fmt");
            args.Add(video.PixelFormat);
        }

        if (video.GopSize.HasValue)
        {
            args.Add("-g");
            args.Add(video.GopSize.Value.ToString());
        }

        args.Add("-c:a");
        args.Add(audio.Codec ?? GetDefaultAudioCodec(outputExt));

        if (audio.Bitrate.HasValue)
        {
            args.Add("-b:a");
            args.Add($"{audio.Bitrate.Value}k");
        }
        else if (options.AudioBitrate.HasValue)
        {
            args.Add("-b:a");
            args.Add($"{options.AudioBitrate.Value}k");
        }

        if (audio.SampleRate.HasValue)
        {
            args.Add("-ar");
            args.Add(audio.SampleRate.Value.ToString());
        }

        if (audio.Channels.HasValue)
        {
            args.Add("-ac");
            args.Add(audio.Channels.Value.ToString());
        }

        if (audio.Volume.HasValue && audio.Volume.Value != 1.0)
        {
            args.Add("-af");
            args.Add($"\"volume={audio.Volume.Value:F2}\"");
        }

        return args;
    }

    private List<string> BuildAudioArgs(string outputExt, ConversionOptions options)
    {
        var args = new List<string>();
        var audio = options.Audio ?? new AudioOptions();

        args.Add("-c:a");
        args.Add(audio.Codec ?? GetDefaultAudioCodec(outputExt));

        switch (outputExt)
        {
            case ".mp3":
                if (audio.Bitrate.HasValue)
                {
                    args.Add("-b:a");
                    args.Add($"{audio.Bitrate.Value}k");
                }
                else if (options.AudioBitrate.HasValue)
                {
                    args.Add("-b:a");
                    args.Add($"{options.AudioBitrate.Value}k");
                }
                else if (audio.Quality.HasValue)
                {
                    args.Add("-q:a");
                    args.Add(audio.Quality.Value.ToString());
                }
                else
                {
                    args.Add("-q:a");
                    args.Add("2");
                }
                break;

            case ".wav":
                break;

            case ".aac":
                args.Add("-b:a");
                args.Add($"{audio.Bitrate ?? options.AudioBitrate ?? 192}k");
                break;

            case ".flac":
                break;

            case ".ogg":
                if (audio.Quality.HasValue)
                {
                    args.Add("-q:a");
                    args.Add(audio.Quality.Value.ToString());
                }
                else
                {
                    args.Add("-q:a");
                    args.Add("5");
                }
                break;
        }

        if (audio.SampleRate.HasValue)
        {
            args.Add("-ar");
            args.Add(audio.SampleRate.Value.ToString());
        }

        if (audio.Channels.HasValue)
        {
            args.Add("-ac");
            args.Add(audio.Channels.Value.ToString());
        }

        if (audio.Volume.HasValue && audio.Volume.Value != 1.0)
        {
            args.Add("-af");
            args.Add($"\"volume={audio.Volume.Value:F2}\"");
        }

        return args;
    }

    private static string GetDefaultVideoCodec(string outputExt)
    {
        return outputExt.ToLowerInvariant() switch
        {
            ".mp4" or ".mkv" or ".m4v" => "libx264",
            ".webm" => "libvpx-vp9",
            ".avi" => "mpeg4",
            _ => "libx264"
        };
    }

    private static string GetDefaultAudioCodec(string outputExt)
    {
        return outputExt.ToLowerInvariant() switch
        {
            ".mp3" => "libmp3lame",
            ".wav" => "pcm_s16le",
            ".aac" or ".m4a" => "aac",
            ".flac" => "flac",
            ".ogg" => "libvorbis",
            ".webm" => "libopus",
            _ => "aac"
        };
    }

    private static string BuildScaleFilter(int? width, int? height, bool keepAspect)
    {
        if (width.HasValue && height.HasValue)
        {
            if (keepAspect)
            {
                return $"scale={width.Value}:{height.Value}:force_original_aspect_ratio=decrease";
            }
            return $"scale={width.Value}:{height.Value}";
        }
        else if (width.HasValue)
        {
            return $"scale={width.Value}:-1";
        }
        else if (height.HasValue)
        {
            return $"scale=-1:{height.Value}";
        }
        return "";
    }

    private static string BuildRotationFilter(int rotation)
    {
        return rotation switch
        {
            90 => "transpose=1",
            180 => "transpose=1,transpose=1",
            270 => "transpose=2",
            _ => ""
        };
    }

    private async Task<(int ExitCode, string Error)> ExecuteFFmpegAsync(
        string arguments, 
        double? duration,
        long fileSize,
        IProgress<ConversionProgress>? progress,
        CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        var errorBuilder = new StringBuilder();
        var stopwatch = Stopwatch.StartNew();
        double lastProgress = 0;
        var lastBytesProcessed = 0L;

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                errorBuilder.AppendLine(e.Data);

                if (progress != null && duration.HasValue && duration.Value > 0)
                {
                    var timeMatch = Regex.Match(e.Data, @"time=(\d+):(\d+):(\d+\.?\d*)");
                    if (timeMatch.Success)
                    {
                        var hours = double.Parse(timeMatch.Groups[1].Value);
                        var minutes = double.Parse(timeMatch.Groups[2].Value);
                        var seconds = double.Parse(timeMatch.Groups[3].Value);
                        var currentTime = hours * 3600 + minutes * 60 + seconds;
                        
                        var currentProgress = Math.Min(100, (currentTime / duration.Value) * 100);
                        
                        if (currentProgress - lastProgress >= 1 || currentProgress >= 99)
                        {
                            var elapsed = stopwatch.Elapsed;
                            var speed = 0.0;
                            
                            if (elapsed.TotalSeconds > 0 && currentProgress > 0)
                            {
                                speed = fileSize / 1024.0 / 1024.0 / elapsed.TotalSeconds * (currentProgress / 100);
                            }

                            var estimatedRemaining = currentProgress > 0 
                                ? TimeSpan.FromSeconds(elapsed.TotalSeconds / currentProgress * (100 - currentProgress))
                                : TimeSpan.Zero;

                            progress.Report(new ConversionProgress
                            {
                                Progress = currentProgress,
                                ElapsedTime = elapsed,
                                EstimatedTimeRemaining = estimatedRemaining,
                                SpeedMBps = speed
                            });

                            lastProgress = currentProgress;
                            lastBytesProcessed = (long)(fileSize * currentProgress / 100);
                        }
                    }
                }
            }
        };

        process.Start();
        process.BeginErrorReadLine();
        
        int exitCode = -1;
        try
        {
            await process.WaitForExitAsync(cancellationToken);
            exitCode = process.ExitCode;
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await Task.Delay(500, CancellationToken.None);
                }
            }
            catch
            {
            }
            throw;
        }
        finally
        {
            try
            {
                process.CancelErrorRead();
            }
            catch
            {
            }
        }

        return (exitCode, errorBuilder.ToString());
    }

    private string ParseFFmpegError(string errorOutput, int exitCode)
    {
        if (string.IsNullOrEmpty(errorOutput))
        {
            return exitCode switch
            {
                1 => "转换失败: 未知错误",
                _ => $"转换失败: FFmpeg退出代码 {exitCode}"
            };
        }

        var lines = errorOutput.Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .ToList();

        var errorLines = lines.Where(l => 
            l.Contains("Error", StringComparison.OrdinalIgnoreCase) ||
            l.Contains("Invalid", StringComparison.OrdinalIgnoreCase) ||
            l.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
            l.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
            l.Contains("cannot", StringComparison.OrdinalIgnoreCase) ||
            l.Contains("Unable", StringComparison.OrdinalIgnoreCase) ||
            l.Contains("No such", StringComparison.OrdinalIgnoreCase) ||
            l.Contains("Permission denied", StringComparison.OrdinalIgnoreCase) ||
            l.Contains("Unsupported", StringComparison.OrdinalIgnoreCase)
        ).ToList();

        if (errorLines.Count == 0)
        {
            errorLines = lines.Where(l => l.StartsWith("[") && l.Contains("]")).TakeLast(5).ToList();
        }

        if (errorLines.Count > 0)
        {
            var errorMsg = errorLines.First();
            
            if (errorMsg.Contains("No such file", StringComparison.OrdinalIgnoreCase))
            {
                return "转换失败: 找不到输入文件";
            }
            if (errorMsg.Contains("Permission denied", StringComparison.OrdinalIgnoreCase))
            {
                return "转换失败: 没有文件访问权限";
            }
            if (errorMsg.Contains("Invalid data", StringComparison.OrdinalIgnoreCase))
            {
                return "转换失败: 输入文件数据无效或已损坏";
            }
            if (errorMsg.Contains("codec not found", StringComparison.OrdinalIgnoreCase) ||
                errorMsg.Contains("Encoder", StringComparison.OrdinalIgnoreCase) ||
                errorMsg.Contains("Decoder", StringComparison.OrdinalIgnoreCase))
            {
                return "转换失败: 不支持该编码格式";
            }
            if (errorMsg.Contains("Unsupported", StringComparison.OrdinalIgnoreCase))
            {
                return "转换失败: 不支持的格式或编码";
            }
            if (errorMsg.Contains("out of memory", StringComparison.OrdinalIgnoreCase))
            {
                return "转换失败: 内存不足";
            }
            if (errorMsg.Contains("disk full", StringComparison.OrdinalIgnoreCase) ||
                errorMsg.Contains("No space left", StringComparison.OrdinalIgnoreCase))
            {
                return "转换失败: 磁盘空间不足";
            }

            var cleanMsg = errorMsg;
            var bracketIndex = cleanMsg.IndexOf(']');
            if (bracketIndex > 0 && bracketIndex < cleanMsg.Length - 1)
            {
                cleanMsg = cleanMsg.Substring(bracketIndex + 1).Trim();
            }
            
            if (cleanMsg.Length > 100)
            {
                cleanMsg = cleanMsg.Substring(0, 100) + "...";
            }
            
            return $"转换失败: {cleanMsg}";
        }

        return "转换失败: 请检查文件格式是否正确";
    }

    public IEnumerable<string> GetSupportedInputFormats()
    {
        return new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a" };
    }

    public IEnumerable<string> GetSupportedOutputFormats()
    {
        return new[] { ".mp4", ".avi", ".mkv", ".webm", ".mp3", ".wav", ".aac", ".flac", ".ogg" };
    }
}
