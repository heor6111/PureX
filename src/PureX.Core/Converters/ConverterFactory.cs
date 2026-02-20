using PureX.Core.Models;

namespace PureX.Core.Converters;

public static class ConverterFactory
{
    private static string? _ffmpegPath;

    public static void Initialize(string ffmpegDirectory)
    {
        _ffmpegPath = ffmpegDirectory;
    }

    public static IConverter? CreateConverter(string inputPath, string outputPath)
    {
        var inputExt = Path.GetExtension(inputPath).ToLowerInvariant();
        var outputExt = Path.GetExtension(outputPath).ToLowerInvariant();

        var inputType = MediaTypeHelper.GetMediaType(inputPath);
        var outputType = MediaTypeHelper.GetMediaType(outputPath);

        if (inputType == MediaType.Image && outputType == MediaType.Image)
        {
            return new ImageConverter();
        }

        if ((inputType == MediaType.Video || inputType == MediaType.Audio) &&
            (outputType == MediaType.Video || outputType == MediaType.Audio))
        {
            if (string.IsNullOrEmpty(_ffmpegPath))
            {
                throw new InvalidOperationException("FFmpeg path not configured. Call Initialize() first.");
            }
            return new FFmpegConverter(_ffmpegPath);
        }

        if (inputType == MediaType.Video && outputType == MediaType.Audio)
        {
            if (string.IsNullOrEmpty(_ffmpegPath))
            {
                throw new InvalidOperationException("FFmpeg path not configured. Call Initialize() first.");
            }
            return new FFmpegConverter(_ffmpegPath);
        }

        return null;
    }

    public static bool CanConvert(string inputPath, string outputPath)
    {
        var inputType = MediaTypeHelper.GetMediaType(inputPath);
        var outputType = MediaTypeHelper.GetMediaType(outputPath);

        if (inputType == null || outputType == null)
            return false;

        if (inputType == MediaType.Image && outputType == MediaType.Image)
            return true;

        if ((inputType == MediaType.Video || inputType == MediaType.Audio) &&
            (outputType == MediaType.Video || outputType == MediaType.Audio))
            return true;

        if (inputType == MediaType.Video && outputType == MediaType.Audio)
            return true;

        return false;
    }
}
