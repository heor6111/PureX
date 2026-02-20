namespace PureX.Core.Models;

public class ConversionOptions
{
    public int? VideoBitrate { get; set; }
    public int? AudioBitrate { get; set; }
    public int? VideoQuality { get; set; }
    public int? ImageQuality { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public bool PreserveMetadata { get; set; } = true;
    public bool OverwriteExisting { get; set; } = true;
    public string? CustomFFmpegArgs { get; set; }
    
    public int MaxConcurrency { get; set; } = 3;

    public VideoOptions? Video { get; set; }
    public AudioOptions? Audio { get; set; }
    public ImageOptions? Image { get; set; }
    public TimeRange? TimeRange { get; set; }
}

public class VideoOptions
{
    public string? Codec { get; set; }
    public string? Preset { get; set; }
    public int? Framerate { get; set; }
    public int? Bitrate { get; set; }
    public int? Quality { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public bool? KeepAspectRatio { get; set; } = true;
    public string? AspectRatio { get; set; }
    public int? Rotation { get; set; }
    public bool? FlipHorizontal { get; set; }
    public bool? FlipVertical { get; set; }
    public string? PixelFormat { get; set; }
    public int? GopSize { get; set; }
    public string? Profile { get; set; }
    public string? Level { get; set; }
}

public class AudioOptions
{
    public string? Codec { get; set; }
    public int? SampleRate { get; set; }
    public int? Bitrate { get; set; }
    public int? Channels { get; set; }
    public double? Volume { get; set; }
    public string? ChannelLayout { get; set; }
    public int? Quality { get; set; }
}

public class ImageOptions
{
    public int? Quality { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public bool? KeepAspectRatio { get; set; } = true;
    public int? Rotation { get; set; }
    public bool? FlipHorizontal { get; set; }
    public bool? FlipVertical { get; set; }
    public int? CropX { get; set; }
    public int? CropY { get; set; }
    public int? CropWidth { get; set; }
    public int? CropHeight { get; set; }
    public int? Dpi { get; set; }
    public string? BackgroundColor { get; set; }
    public string? ResizeMode { get; set; }
}

public class TimeRange
{
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public TimeSpan? Duration { get; set; }
}
