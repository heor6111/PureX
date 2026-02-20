using PureX.Core.Models;

namespace PureX.Core.Services;

public class OutputSizeEstimator
{
    private const double VideoCompressionRatio = 0.3;
    private const double AudioCompressionRatio = 0.1;
    private const double ImageCompressionRatio = 0.5;
    private const double SafetyMargin = 1.1;

    public long EstimateOutputSize(string inputPath, string outputFormat, ConversionOptions? options = null)
    {
        if (!File.Exists(inputPath))
        {
            return 0;
        }

        var inputInfo = new FileInfo(inputPath);
        long inputSize = inputInfo.Length;
        string inputExt = Path.GetExtension(inputPath).ToLowerInvariant();
        string outputExt = outputFormat.ToLowerInvariant().TrimStart('.');

        var mediaType = DetermineMediaType(inputExt);
        double ratio = GetCompressionRatio(mediaType, inputExt, outputExt, options);

        ApplyOptionsAdjustments(ref ratio, options, mediaType);

        long estimatedSize = (long)(inputSize * ratio * SafetyMargin);

        return Math.Max(estimatedSize, 1024 * 1024);
    }

    public long EstimateBatchOutputSize(IEnumerable<(string InputPath, string OutputFormat, ConversionOptions? Options)> files)
    {
        long totalSize = 0;
        foreach (var file in files)
        {
            totalSize += EstimateOutputSize(file.InputPath, file.OutputFormat, file.Options);
        }
        return (long)(totalSize * SafetyMargin);
    }

    private EstimationMediaType DetermineMediaType(string extension)
    {
        var videoExts = new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".ts", ".mts" };
        var audioExts = new[] { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a", ".wma", ".ape", ".alac" };
        var imageExts = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".webp", ".ico" };

        if (videoExts.Contains(extension))
            return EstimationMediaType.Video;
        if (audioExts.Contains(extension))
            return EstimationMediaType.Audio;
        if (imageExts.Contains(extension))
            return EstimationMediaType.Image;

        return EstimationMediaType.Unknown;
    }

    private double GetCompressionRatio(EstimationMediaType mediaType, string inputExt, string outputExt, ConversionOptions? options)
    {
        return mediaType switch
        {
            EstimationMediaType.Video => GetVideoRatio(inputExt, outputExt, options),
            EstimationMediaType.Audio => GetAudioRatio(inputExt, outputExt, options),
            EstimationMediaType.Image => GetImageRatio(inputExt, outputExt, options),
            _ => 1.0
        };
    }

    private double GetVideoRatio(string inputExt, string outputExt, ConversionOptions? options)
    {
        double baseRatio = VideoCompressionRatio;

        if (options?.Video?.Bitrate.HasValue == true)
        {
            int bitrate = options.Video.Bitrate.Value;
            if (bitrate >= 10000) baseRatio = 0.8;
            else if (bitrate >= 5000) baseRatio = 0.5;
            else if (bitrate >= 2000) baseRatio = 0.3;
            else baseRatio = 0.15;
        }
        else if (options?.Video?.Quality.HasValue == true)
        {
            int crf = options.Video.Quality.Value;
            if (crf <= 18) baseRatio = 0.7;
            else if (crf <= 23) baseRatio = 0.4;
            else if (crf <= 28) baseRatio = 0.25;
            else baseRatio = 0.15;
        }

        if (options?.Video?.Width.HasValue == true || options?.Video?.Height.HasValue == true)
        {
            baseRatio *= 0.7;
        }

        if (outputExt == "webm")
            baseRatio *= 0.9;

        return baseRatio;
    }

    private double GetAudioRatio(string inputExt, string outputExt, ConversionOptions? options)
    {
        double baseRatio = AudioCompressionRatio;

        if (options?.Audio?.Bitrate.HasValue == true)
        {
            int bitrate = options.Audio.Bitrate.Value;
            if (bitrate >= 320) baseRatio = 0.3;
            else if (bitrate >= 192) baseRatio = 0.2;
            else if (bitrate >= 128) baseRatio = 0.12;
            else baseRatio = 0.06;
        }

        if (outputExt == "flac" || outputExt == "wav")
        {
            baseRatio = 3.0;
        }

        if (options?.Audio?.Channels.HasValue == true && options.Audio.Channels.Value == 1)
        {
            baseRatio *= 0.5;
        }

        return baseRatio;
    }

    private double GetImageRatio(string inputExt, string outputExt, ConversionOptions? options)
    {
        double baseRatio = ImageCompressionRatio;

        if (options?.Image?.Quality.HasValue == true)
        {
            int quality = options.Image.Quality.Value;
            if (quality >= 95) baseRatio = 0.9;
            else if (quality >= 85) baseRatio = 0.6;
            else if (quality >= 70) baseRatio = 0.4;
            else baseRatio = 0.25;
        }

        if (outputExt == "png")
        {
            baseRatio = 1.2;
        }
        else if (outputExt == "bmp")
        {
            baseRatio = 3.0;
        }
        else if (outputExt == "webp")
        {
            baseRatio = 0.5;
        }

        if (options?.Image?.Width.HasValue == true || options?.Image?.Height.HasValue == true)
        {
            int? w = options.Image.Width;
            int? h = options.Image.Height;
            if (w.HasValue && h.HasValue)
            {
                baseRatio *= 0.5;
            }
            else
            {
                baseRatio *= 0.7;
            }
        }

        return baseRatio;
    }

    private void ApplyOptionsAdjustments(ref double ratio, ConversionOptions? options, EstimationMediaType mediaType)
    {
        if (options == null) return;

        if (options.TimeRange != null)
        {
            ratio *= 0.3;
        }
    }
}

internal enum EstimationMediaType
{
    Unknown,
    Video,
    Audio,
    Image
}
