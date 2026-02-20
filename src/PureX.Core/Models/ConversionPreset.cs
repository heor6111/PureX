namespace PureX.Core.Models;

public class ConversionPreset
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public ConversionOptions Options { get; set; } = new();
    public string? Icon { get; set; }
    public bool IsBuiltIn { get; set; }
}

public static class BuiltInPresets
{
    public static List<ConversionPreset> VideoPresets { get; } = new()
    {
        new ConversionPreset
        {
            Name = "高质量MP4",
            Description = "H.264编码，高质量输出，适合大多数场景",
            Category = "视频",
            Icon = "🎬",
            IsBuiltIn = true,
            Options = new ConversionOptions
            {
                Video = new VideoOptions
                {
                    Codec = "libx264",
                    Preset = "slow",
                    Quality = 18
                },
                Audio = new AudioOptions
                {
                    Codec = "aac",
                    Bitrate = 192
                }
            }
        },
        new ConversionPreset
        {
            Name = "压缩MP4",
            Description = "较小文件大小，适合网络分享",
            Category = "视频",
            Icon = "📦",
            IsBuiltIn = true,
            Options = new ConversionOptions
            {
                Video = new VideoOptions
                {
                    Codec = "libx264",
                    Preset = "fast",
                    Quality = 28
                },
                Audio = new AudioOptions
                {
                    Codec = "aac",
                    Bitrate = 128
                }
            }
        },
        new ConversionPreset
        {
            Name = "1080p高清",
            Description = "1920x1080分辨率，高清输出",
            Category = "视频",
            Icon = "📺",
            IsBuiltIn = true,
            Options = new ConversionOptions
            {
                Video = new VideoOptions
                {
                    Codec = "libx264",
                    Preset = "medium",
                    Quality = 23,
                    Width = 1920,
                    Height = 1080
                }
            }
        },
        new ConversionPreset
        {
            Name = "720p标清",
            Description = "1280x720分辨率，适合移动设备",
            Category = "视频",
            Icon = "📱",
            IsBuiltIn = true,
            Options = new ConversionOptions
            {
                Video = new VideoOptions
                {
                    Codec = "libx264",
                    Preset = "medium",
                    Quality = 23,
                    Width = 1280,
                    Height = 720
                }
            }
        },
        new ConversionPreset
        {
            Name = "WebM网页",
            Description = "VP9编码，适合网页嵌入",
            Category = "视频",
            Icon = "🌐",
            IsBuiltIn = true,
            Options = new ConversionOptions
            {
                Video = new VideoOptions
                {
                    Codec = "libvpx-vp9",
                    Quality = 30
                },
                Audio = new AudioOptions
                {
                    Codec = "libopus",
                    Bitrate = 128
                }
            }
        }
    };

    public static List<ConversionPreset> AudioPresets { get; } = new()
    {
        new ConversionPreset
        {
            Name = "高质量MP3",
            Description = "320kbps，最佳音质",
            Category = "音频",
            Icon = "🎵",
            IsBuiltIn = true,
            Options = new ConversionOptions
            {
                Audio = new AudioOptions
                {
                    Codec = "libmp3lame",
                    Bitrate = 320,
                    SampleRate = 44100
                }
            }
        },
        new ConversionPreset
        {
            Name = "标准MP3",
            Description = "192kbps，平衡音质与大小",
            Category = "音频",
            Icon = "🎶",
            IsBuiltIn = true,
            Options = new ConversionOptions
            {
                Audio = new AudioOptions
                {
                    Codec = "libmp3lame",
                    Bitrate = 192,
                    SampleRate = 44100
                }
            }
        },
        new ConversionPreset
        {
            Name = "无损FLAC",
            Description = "无损压缩，保留原始音质",
            Category = "音频",
            Icon = "💿",
            IsBuiltIn = true,
            Options = new ConversionOptions
            {
                Audio = new AudioOptions
                {
                    Codec = "flac"
                }
            }
        },
        new ConversionPreset
        {
            Name = "AAC高效",
            Description = "AAC编码，高压缩效率",
            Category = "音频",
            Icon = "🔊",
            IsBuiltIn = true,
            Options = new ConversionOptions
            {
                Audio = new AudioOptions
                {
                    Codec = "aac",
                    Bitrate = 192
                }
            }
        },
        new ConversionPreset
        {
            Name = "语音优化",
            Description = "适合语音录制，小文件",
            Category = "音频",
            Icon = "🎤",
            IsBuiltIn = true,
            Options = new ConversionOptions
            {
                Audio = new AudioOptions
                {
                    Codec = "libmp3lame",
                    Bitrate = 64,
                    SampleRate = 22050,
                    Channels = 1
                }
            }
        }
    };

    public static List<ConversionPreset> ImagePresets { get; } = new()
    {
        new ConversionPreset
        {
            Name = "高质量JPG",
            Description = "95%质量，适合照片",
            Category = "图片",
            Icon = "🖼️",
            IsBuiltIn = true,
            Options = new ConversionOptions
            {
                Image = new ImageOptions
                {
                    Quality = 95
                }
            }
        },
        new ConversionPreset
        {
            Name = "网页优化JPG",
            Description = "80%质量，适合网页",
            Category = "图片",
            Icon = "🌐",
            IsBuiltIn = true,
            Options = new ConversionOptions
            {
                Image = new ImageOptions
                {
                    Quality = 80
                }
            }
        },
        new ConversionPreset
        {
            Name = "无损PNG",
            Description = "无损压缩，适合图标和截图",
            Category = "图片",
            Icon = "📊",
            IsBuiltIn = true,
            Options = new ConversionOptions
            {
                Image = new ImageOptions
                {
                    Quality = 100
                }
            }
        },
        new ConversionPreset
        {
            Name = "WebP高效",
            Description = "现代格式，高压缩比",
            Category = "图片",
            Icon = "⚡",
            IsBuiltIn = true,
            Options = new ConversionOptions
            {
                Image = new ImageOptions
                {
                    Quality = 85
                }
            }
        },
        new ConversionPreset
        {
            Name = "缩略图",
            Description = "200x200，适合预览图",
            Category = "图片",
            Icon = "🔍",
            IsBuiltIn = true,
            Options = new ConversionOptions
            {
                Image = new ImageOptions
                {
                    Width = 200,
                    Height = 200,
                    Quality = 75,
                    KeepAspectRatio = true
                }
            }
        }
    };

    public static List<ConversionPreset> GetAllPresets()
    {
        var presets = new List<ConversionPreset>();
        presets.AddRange(VideoPresets);
        presets.AddRange(AudioPresets);
        presets.AddRange(ImagePresets);
        return presets;
    }
}
