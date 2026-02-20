using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using PureX.Core.Models;
using PureX.Core.Services;

namespace PureX.ViewModels;

public class AdvancedSettingsViewModel : ViewModelBase, INotifyPropertyChanged
{
    private ConversionOptions _options;
    private string _mediaType;

    public AdvancedSettingsViewModel(ConversionOptions options, string mediaType)
    {
        _options = options ?? new ConversionOptions();
        _mediaType = mediaType ?? "Video";
        
        InitializeOptions();
        LoadPresets();
        
        ApplyPresetCommand = new RelayCommand<ConversionPreset>(ApplyPreset);
        ResetCommand = new RelayCommand(ResetToDefaults);
    }

    public bool IsVideo => _mediaType == "Video";
    public bool IsAudio => _mediaType == "Audio";
    public bool IsImage => _mediaType == "Image";

    #region Concurrency Settings

    private int _maxConcurrency = 3;
    public int MaxConcurrency
    {
        get => _maxConcurrency;
        set 
        { 
            _maxConcurrency = Math.Clamp(value, 1, 5); 
            OnPropertyChanged(); 
        }
    }

    public List<int> ConcurrencyOptions { get; } = new() { 1, 2, 3, 4, 5 };

    #endregion

    #region Video Options

    private int _videoWidth;
    public int VideoWidth
    {
        get => _videoWidth;
        set { _videoWidth = value; OnPropertyChanged(); }
    }

    private int _videoHeight;
    public int VideoHeight
    {
        get => _videoHeight;
        set { _videoHeight = value; OnPropertyChanged(); }
    }

    private bool _keepAspectRatio = true;
    public bool KeepAspectRatio
    {
        get => _keepAspectRatio;
        set { _keepAspectRatio = value; OnPropertyChanged(); }
    }

    private int _videoBitrate;
    public int VideoBitrate
    {
        get => _videoBitrate;
        set { _videoBitrate = value; OnPropertyChanged(); }
    }

    private int _videoQuality = 23;
    public int VideoQuality
    {
        get => _videoQuality;
        set { _videoQuality = value; OnPropertyChanged(); }
    }

    private int _framerate;
    public int Framerate
    {
        get => _framerate;
        set { _framerate = value; OnPropertyChanged(); }
    }

    private string _videoCodec = "libx264";
    public string VideoCodec
    {
        get => _videoCodec;
        set { _videoCodec = value; OnPropertyChanged(); }
    }

    private string _preset = "medium";
    public string Preset
    {
        get => _preset;
        set { _preset = value; OnPropertyChanged(); }
    }

    private int _rotation;
    public int Rotation
    {
        get => _rotation;
        set { _rotation = value; OnPropertyChanged(); }
    }

    private bool _flipHorizontal;
    public bool FlipHorizontal
    {
        get => _flipHorizontal;
        set { _flipHorizontal = value; OnPropertyChanged(); }
    }

    private bool _flipVertical;
    public bool FlipVertical
    {
        get => _flipVertical;
        set { _flipVertical = value; OnPropertyChanged(); }
    }

    #endregion

    #region Audio Options

    private int _audioBitrate = 192;
    public int AudioBitrate
    {
        get => _audioBitrate;
        set { _audioBitrate = value; OnPropertyChanged(); }
    }

    private int _sampleRate = 44100;
    public int SampleRate
    {
        get => _sampleRate;
        set { _sampleRate = value; OnPropertyChanged(); }
    }

    private int _channels = 2;
    public int Channels
    {
        get => _channels;
        set { _channels = value; OnPropertyChanged(); }
    }

    private double _volume = 1.0;
    public double Volume
    {
        get => _volume;
        set { _volume = value; OnPropertyChanged(); }
    }

    private string _audioCodec = "aac";
    public string AudioCodec
    {
        get => _audioCodec;
        set { _audioCodec = value; OnPropertyChanged(); }
    }

    #endregion

    #region Image Options

    private int _imageQuality = 90;
    public int ImageQuality
    {
        get => _imageQuality;
        set { _imageQuality = value; OnPropertyChanged(); }
    }

    private int _imageWidth;
    public int ImageWidth
    {
        get => _imageWidth;
        set { _imageWidth = value; OnPropertyChanged(); }
    }

    private int _imageHeight;
    public int ImageHeight
    {
        get => _imageHeight;
        set { _imageHeight = value; OnPropertyChanged(); }
    }

    private int _imageRotation;
    public int ImageRotation
    {
        get => _imageRotation;
        set { _imageRotation = value; OnPropertyChanged(); }
    }

    private int _cropX;
    public int CropX
    {
        get => _cropX;
        set { _cropX = value; OnPropertyChanged(); }
    }

    private int _cropY;
    public int CropY
    {
        get => _cropY;
        set { _cropY = value; OnPropertyChanged(); }
    }

    private int _cropWidth;
    public int CropWidth
    {
        get => _cropWidth;
        set { _cropWidth = value; OnPropertyChanged(); }
    }

    private int _cropHeight;
    public int CropHeight
    {
        get => _cropHeight;
        set { _cropHeight = value; OnPropertyChanged(); }
    }

    private int _dpi;
    public int Dpi
    {
        get => _dpi;
        set { _dpi = value; OnPropertyChanged(); }
    }

    private string _backgroundColor = "";
    public string BackgroundColor
    {
        get => _backgroundColor;
        set { _backgroundColor = value; OnPropertyChanged(); }
    }

    #endregion

    #region Time Range

    private TimeSpan _startTime;
    public TimeSpan StartTime
    {
        get => _startTime;
        set { _startTime = value; OnPropertyChanged(); }
    }

    private TimeSpan _endTime;
    public TimeSpan EndTime
    {
        get => _endTime;
        set { _endTime = value; OnPropertyChanged(); }
    }

    private bool _useTimeRange;
    public bool UseTimeRange
    {
        get => _useTimeRange;
        set { _useTimeRange = value; OnPropertyChanged(); }
    }

    #endregion

    #region Presets

    public ObservableCollection<ConversionPreset> Presets { get; } = new();
    
    private ConversionPreset? _selectedPreset;
    public ConversionPreset? SelectedPreset
    {
        get => _selectedPreset;
        set { _selectedPreset = value; OnPropertyChanged(); }
    }

    #endregion

    public ICommand ApplyPresetCommand { get; }
    public ICommand ResetCommand { get; }

    public List<string> VideoCodecs { get; } = new() { "libx264", "libx265", "libvpx-vp9", "mpeg4" };
    public List<string> AudioCodecs { get; } = new() { "aac", "libmp3lame", "flac", "libopus", "pcm_s16le" };
    public List<string> Presets_list { get; } = new() { "ultrafast", "superfast", "veryfast", "faster", "fast", "medium", "slow", "slower", "veryslow" };
    public List<int> SampleRates { get; } = new() { 8000, 16000, 22050, 44100, 48000, 96000 };
    public List<int> ChannelOptions { get; } = new() { 1, 2, 6 };
    public List<int> RotationOptions { get; } = new() { 0, 90, 180, 270 };

    private void InitializeOptions()
    {
        MaxConcurrency = UserSettingsService.Instance.MaxConcurrency;

        if (_options.Video != null)
        {
            VideoWidth = _options.Video.Width ?? 0;
            VideoHeight = _options.Video.Height ?? 0;
            KeepAspectRatio = _options.Video.KeepAspectRatio ?? true;
            VideoBitrate = _options.Video.Bitrate ?? 0;
            VideoQuality = _options.Video.Quality ?? 23;
            Framerate = _options.Video.Framerate ?? 0;
            VideoCodec = _options.Video.Codec ?? "libx264";
            Preset = _options.Video.Preset ?? "medium";
            Rotation = _options.Video.Rotation ?? 0;
            FlipHorizontal = _options.Video.FlipHorizontal ?? false;
            FlipVertical = _options.Video.FlipVertical ?? false;
        }

        if (_options.Audio != null)
        {
            AudioBitrate = _options.Audio.Bitrate ?? 192;
            SampleRate = _options.Audio.SampleRate ?? 44100;
            Channels = _options.Audio.Channels ?? 2;
            Volume = _options.Audio.Volume ?? 1.0;
            AudioCodec = _options.Audio.Codec ?? "aac";
        }

        if (_options.Image != null)
        {
            ImageQuality = _options.Image.Quality ?? 90;
            ImageWidth = _options.Image.Width ?? 0;
            ImageHeight = _options.Image.Height ?? 0;
            ImageRotation = _options.Image.Rotation ?? 0;
            CropX = _options.Image.CropX ?? 0;
            CropY = _options.Image.CropY ?? 0;
            CropWidth = _options.Image.CropWidth ?? 0;
            CropHeight = _options.Image.CropHeight ?? 0;
            Dpi = _options.Image.Dpi ?? 0;
            BackgroundColor = _options.Image.BackgroundColor ?? "";
        }

        if (_options.TimeRange != null)
        {
            StartTime = _options.TimeRange.StartTime ?? TimeSpan.Zero;
            EndTime = _options.TimeRange.EndTime ?? TimeSpan.Zero;
            UseTimeRange = true;
        }
    }

    private void LoadPresets()
    {
        var presetList = _mediaType switch
        {
            "Video" => BuiltInPresets.VideoPresets,
            "Audio" => BuiltInPresets.AudioPresets,
            "Image" => BuiltInPresets.ImagePresets,
            _ => new List<ConversionPreset>()
        };

        foreach (var preset in presetList)
        {
            Presets.Add(preset);
        }
    }

    private void ApplyPreset(ConversionPreset? preset)
    {
        if (preset == null) return;
        
        _options = preset.Options;
        InitializeOptions();
    }

    private void ResetToDefaults()
    {
        _options = new ConversionOptions();
        InitializeOptions();
    }

    public ConversionOptions GetOptions()
    {
        var options = new ConversionOptions();

        if (IsVideo || IsAudio)
        {
            options.Video = new VideoOptions
            {
                Width = VideoWidth > 0 ? VideoWidth : null,
                Height = VideoHeight > 0 ? VideoHeight : null,
                KeepAspectRatio = KeepAspectRatio,
                Bitrate = VideoBitrate > 0 ? VideoBitrate : null,
                Quality = VideoQuality,
                Framerate = Framerate > 0 ? Framerate : null,
                Codec = VideoCodec,
                Preset = Preset,
                Rotation = Rotation,
                FlipHorizontal = FlipHorizontal,
                FlipVertical = FlipVertical
            };

            options.Audio = new AudioOptions
            {
                Bitrate = AudioBitrate,
                SampleRate = SampleRate,
                Channels = Channels,
                Volume = Volume,
                Codec = AudioCodec
            };

            if (UseTimeRange)
            {
                options.TimeRange = new TimeRange
                {
                    StartTime = StartTime,
                    EndTime = EndTime
                };
            }
        }

        if (IsImage)
        {
            options.Image = new ImageOptions
            {
                Quality = ImageQuality,
                Width = ImageWidth > 0 ? ImageWidth : null,
                Height = ImageHeight > 0 ? ImageHeight : null,
                Rotation = ImageRotation,
                CropX = CropX,
                CropY = CropY,
                CropWidth = CropWidth > 0 ? CropWidth : null,
                CropHeight = CropHeight > 0 ? CropHeight : null,
                Dpi = Dpi > 0 ? Dpi : null,
                BackgroundColor = BackgroundColor
            };
        }

        options.MaxConcurrency = MaxConcurrency;
        UserSettingsService.Instance.MaxConcurrency = MaxConcurrency;

        return options;
    }

    public new event PropertyChangedEventHandler? PropertyChanged;
    protected new virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
