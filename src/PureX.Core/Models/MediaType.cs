namespace PureX.Core.Models;

public enum MediaType
{
    Video,
    Audio,
    Image
}

public static class MediaTypeHelper
{
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpeg", ".mpg"
    };

    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a", ".opus"
    };

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".tiff", ".tif", ".ico", ".svg"
    };

    public static MediaType? GetMediaType(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        if (VideoExtensions.Contains(ext)) return MediaType.Video;
        if (AudioExtensions.Contains(ext)) return MediaType.Audio;
        if (ImageExtensions.Contains(ext)) return MediaType.Image;
        return null;
    }

    public static bool IsSupportedVideoFormat(string extension)
    {
        return VideoExtensions.Contains(extension);
    }

    public static bool IsSupportedAudioFormat(string extension)
    {
        return AudioExtensions.Contains(extension);
    }

    public static bool IsSupportedImageFormat(string extension)
    {
        return ImageExtensions.Contains(extension);
    }
}
