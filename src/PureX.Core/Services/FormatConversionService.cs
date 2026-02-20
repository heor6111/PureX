using PureX.Core.Models;

namespace PureX.Core.Services;

public class FormatConversionService
{
    private readonly Dictionary<string, List<string>> _conversionMap;

    public FormatConversionService()
    {
        _conversionMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            { ".mp4", new List<string> { ".avi", ".mkv", ".webm", ".mov", ".wmv", ".flv", ".mp3", ".aac", ".wav" } },
            { ".avi", new List<string> { ".mp4", ".mkv", ".webm", ".mov", ".wmv", ".flv", ".mp3", ".aac", ".wav" } },
            { ".mkv", new List<string> { ".mp4", ".avi", ".webm", ".mov", ".wmv", ".flv", ".mp3", ".aac", ".wav" } },
            { ".mov", new List<string> { ".mp4", ".avi", ".mkv", ".webm", ".wmv", ".flv", ".mp3", ".aac", ".wav" } },
            { ".wmv", new List<string> { ".mp4", ".avi", ".mkv", ".webm", ".mov", ".flv", ".mp3", ".aac", ".wav" } },
            { ".flv", new List<string> { ".mp4", ".avi", ".mkv", ".webm", ".mov", ".wmv", ".mp3", ".aac", ".wav" } },
            { ".webm", new List<string> { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".mp3", ".aac", ".wav" } },
            { ".m4v", new List<string> { ".mp4", ".avi", ".mkv", ".webm", ".mp3", ".aac", ".wav" } },
            { ".ts", new List<string> { ".mp4", ".avi", ".mkv", ".webm", ".mp3", ".aac", ".wav" } },
            { ".mts", new List<string> { ".mp4", ".avi", ".mkv", ".webm", ".mp3", ".aac", ".wav" } },
            
            { ".mp3", new List<string> { ".wav", ".flac", ".aac", ".ogg", ".m4a", ".wma" } },
            { ".wav", new List<string> { ".mp3", ".flac", ".aac", ".ogg", ".m4a", ".wma" } },
            { ".flac", new List<string> { ".mp3", ".wav", ".aac", ".ogg", ".m4a", ".wma" } },
            { ".aac", new List<string> { ".mp3", ".wav", ".flac", ".ogg", ".m4a", ".wma" } },
            { ".ogg", new List<string> { ".mp3", ".wav", ".flac", ".aac", ".m4a", ".wma" } },
            { ".m4a", new List<string> { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma" } },
            { ".wma", new List<string> { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a" } },
            { ".ape", new List<string> { ".mp3", ".wav", ".flac", ".aac", ".ogg" } },
            { ".alac", new List<string> { ".mp3", ".wav", ".flac", ".aac", ".ogg" } },
            
            { ".jpg", new List<string> { ".png", ".webp", ".gif", ".bmp", ".tiff", ".ico" } },
            { ".jpeg", new List<string> { ".png", ".webp", ".gif", ".bmp", ".tiff", ".ico" } },
            { ".png", new List<string> { ".jpg", ".webp", ".gif", ".bmp", ".tiff", ".ico" } },
            { ".webp", new List<string> { ".jpg", ".png", ".gif", ".bmp", ".tiff" } },
            { ".gif", new List<string> { ".jpg", ".png", ".webp", ".bmp" } },
            { ".bmp", new List<string> { ".jpg", ".png", ".webp", ".gif", ".tiff" } },
            { ".tiff", new List<string> { ".jpg", ".png", ".webp", ".bmp" } },
            { ".tif", new List<string> { ".jpg", ".png", ".webp", ".bmp" } },
            { ".ico", new List<string> { ".jpg", ".png", ".bmp" } }
        };
    }

    public List<FormatOption> GetConvertibleFormats(string inputExtension)
    {
        var ext = inputExtension.ToLowerInvariant();
        if (!ext.StartsWith("."))
        {
            ext = "." + ext;
        }

        if (_conversionMap.TryGetValue(ext, out var formats))
        {
            return formats.Select(f => new FormatOption
            {
                Extension = f,
                DisplayName = f.TrimStart('.').ToUpperInvariant(),
                Description = GetFormatDescription(f)
            }).ToList();
        }

        return GetDefaultFormatsForMediaType(ext);
    }

    private List<FormatOption> GetDefaultFormatsForMediaType(string extension)
    {
        var videoExts = new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".ts", ".mts" };
        var audioExts = new[] { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a", ".wma", ".ape", ".alac" };
        var imageExts = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".webp", ".ico" };

        if (videoExts.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return new List<FormatOption>
            {
                new() { Extension = ".mp4", DisplayName = "MP4", Description = "通用视频格式" },
                new() { Extension = ".avi", DisplayName = "AVI", Description = "传统视频格式" },
                new() { Extension = ".mkv", DisplayName = "MKV", Description = "高清封装格式" },
                new() { Extension = ".webm", DisplayName = "WebM", Description = "网页视频格式" }
            };
        }

        if (audioExts.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return new List<FormatOption>
            {
                new() { Extension = ".mp3", DisplayName = "MP3", Description = "通用音频格式" },
                new() { Extension = ".wav", DisplayName = "WAV", Description = "无损音频格式" },
                new() { Extension = ".aac", DisplayName = "AAC", Description = "高效音频格式" },
                new() { Extension = ".flac", DisplayName = "FLAC", Description = "无损压缩格式" }
            };
        }

        if (imageExts.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return new List<FormatOption>
            {
                new() { Extension = ".jpg", DisplayName = "JPG", Description = "通用图片格式" },
                new() { Extension = ".png", DisplayName = "PNG", Description = "无损图片格式" },
                new() { Extension = ".webp", DisplayName = "WebP", Description = "网页图片格式" },
                new() { Extension = ".gif", DisplayName = "GIF", Description = "动图格式" }
            };
        }

        return new List<FormatOption>();
    }

    private string GetFormatDescription(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".mp4" => "通用视频格式",
            ".avi" => "传统视频格式",
            ".mkv" => "高清封装格式",
            ".webm" => "网页视频格式",
            ".mov" => "苹果视频格式",
            ".wmv" => "Windows视频格式",
            ".flv" => "Flash视频格式",
            ".mp3" => "通用音频格式",
            ".wav" => "无损音频格式",
            ".flac" => "无损压缩格式",
            ".aac" => "高效音频格式",
            ".ogg" => "开源音频格式",
            ".m4a" => "苹果音频格式",
            ".wma" => "Windows音频格式",
            ".jpg" => "通用图片格式",
            ".jpeg" => "通用图片格式",
            ".png" => "无损图片格式",
            ".webp" => "网页图片格式",
            ".gif" => "动图格式",
            ".bmp" => "位图格式",
            ".tiff" => "专业图片格式",
            ".ico" => "图标格式",
            _ => ""
        };
    }
}
