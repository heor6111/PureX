using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using PureX.Core.Services;

namespace PureX.ViewModels;

public class ToolboxViewModel : ViewModelBase
{
    private readonly ToolboxService _toolboxService;
    private int _selectedTabIndex;
    private bool _isProcessing;
    private string _statusMessage = string.Empty;
    private double _progress;

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set { _selectedTabIndex = value; OnPropertyChanged(); }
    }

    public bool IsProcessing
    {
        get => _isProcessing;
        set { _isProcessing = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public double Progress
    {
        get => _progress;
        set { _progress = value; OnPropertyChanged(); }
    }

    #region Audio Extract

    private string _audioExtractInput = string.Empty;
    public string AudioExtractInput
    {
        get => _audioExtractInput;
        set { _audioExtractInput = value; OnPropertyChanged(); }
    }

    private string _audioExtractOutput = string.Empty;
    public string AudioExtractOutput
    {
        get => _audioExtractOutput;
        set { _audioExtractOutput = value; OnPropertyChanged(); }
    }

    private string _selectedAudioFormat = "mp3";
    public string SelectedAudioFormat
    {
        get => _selectedAudioFormat;
        set { _selectedAudioFormat = value; OnPropertyChanged(); }
    }

    public List<string> AudioFormats { get; } = new() { "mp3", "wav", "aac", "flac", "ogg" };

    private int _audioBitrate = 192;
    public int AudioBitrate
    {
        get => _audioBitrate;
        set { _audioBitrate = value; OnPropertyChanged(); }
    }

    public List<int> AudioBitrates { get; } = new() { 128, 192, 256, 320 };

    #endregion

    #region Audio Merge

    public ObservableCollection<string> AudioMergeFiles { get; } = new();
    
    private string _audioMergeOutput = string.Empty;
    public string AudioMergeOutput
    {
        get => _audioMergeOutput;
        set { _audioMergeOutput = value; OnPropertyChanged(); }
    }

    #endregion

    #region Format Repair

    private string _repairInput = string.Empty;
    public string RepairInput
    {
        get => _repairInput;
        set { _repairInput = value; OnPropertyChanged(); }
    }

    private string _repairOutput = string.Empty;
    public string RepairOutput
    {
        get => _repairOutput;
        set { _repairOutput = value; OnPropertyChanged(); }
    }

    #endregion

    #region Media Trim

    private string _trimInput = string.Empty;
    public string TrimInput
    {
        get => _trimInput;
        set { _trimInput = value; OnPropertyChanged(); }
    }

    private string _trimOutput = string.Empty;
    public string TrimOutput
    {
        get => _trimOutput;
        set { _trimOutput = value; OnPropertyChanged(); }
    }

    private TimeSpan _trimStartTime;
    public TimeSpan TrimStartTime
    {
        get => _trimStartTime;
        set { _trimStartTime = value; OnPropertyChanged(); }
    }

    private TimeSpan _trimEndTime = TimeSpan.FromMinutes(1);
    public TimeSpan TrimEndTime
    {
        get => _trimEndTime;
        set { _trimEndTime = value; OnPropertyChanged(); }
    }

    private string _trimDuration = "00:01:00";
    public string TrimDuration
    {
        get => _trimDuration;
        set { _trimDuration = value; OnPropertyChanged(); }
    }

    #endregion

    #region Video Crop

    private string _cropInput = string.Empty;
    public string CropInput
    {
        get => _cropInput;
        set { _cropInput = value; OnPropertyChanged(); }
    }

    private string _cropOutput = string.Empty;
    public string CropOutput
    {
        get => _cropOutput;
        set { _cropOutput = value; OnPropertyChanged(); }
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

    private int _cropWidth = 1920;
    public int CropWidth
    {
        get => _cropWidth;
        set { _cropWidth = value; OnPropertyChanged(); }
    }

    private int _cropHeight = 1080;
    public int CropHeight
    {
        get => _cropHeight;
        set { _cropHeight = value; OnPropertyChanged(); }
    }

    #endregion

    #region Media Join

    public ObservableCollection<string> MediaJoinFiles { get; } = new();

    private string _mediaJoinOutput = string.Empty;
    public string MediaJoinOutput
    {
        get => _mediaJoinOutput;
        set { _mediaJoinOutput = value; OnPropertyChanged(); }
    }

    #endregion

    public ICommand SelectAudioExtractInputCommand { get; }
    public ICommand SelectAudioExtractOutputCommand { get; }
    public ICommand ExtractAudioCommand { get; }

    public ICommand AddAudioMergeFileCommand { get; }
    public ICommand RemoveAudioMergeFileCommand { get; }
    public ICommand SelectAudioMergeOutputCommand { get; }
    public ICommand MergeAudioCommand { get; }

    public ICommand SelectRepairInputCommand { get; }
    public ICommand SelectRepairOutputCommand { get; }
    public ICommand RepairFileCommand { get; }

    public ICommand SelectTrimInputCommand { get; }
    public ICommand SelectTrimOutputCommand { get; }
    public ICommand TrimMediaCommand { get; }

    public ICommand SelectCropInputCommand { get; }
    public ICommand SelectCropOutputCommand { get; }
    public ICommand CropVideoCommand { get; }

    public ICommand AddMediaJoinFileCommand { get; }
    public ICommand RemoveMediaJoinFileCommand { get; }
    public ICommand SelectMediaJoinOutputCommand { get; }
    public ICommand JoinMediaCommand { get; }

    public ToolboxViewModel()
    {
        _toolboxService = new ToolboxService();

        SelectAudioExtractInputCommand = new RelayCommand(SelectAudioExtractInput);
        SelectAudioExtractOutputCommand = new RelayCommand(SelectAudioExtractOutput);
        ExtractAudioCommand = new AsyncRelayCommand(ExtractAudio, () => !IsProcessing && !string.IsNullOrEmpty(AudioExtractInput));

        AddAudioMergeFileCommand = new RelayCommand(AddAudioMergeFile);
        RemoveAudioMergeFileCommand = new RelayCommand<string>(RemoveAudioMergeFile);
        SelectAudioMergeOutputCommand = new RelayCommand(SelectAudioMergeOutput);
        MergeAudioCommand = new AsyncRelayCommand(MergeAudio, () => !IsProcessing && AudioMergeFiles.Count > 0);

        SelectRepairInputCommand = new RelayCommand(SelectRepairInput);
        SelectRepairOutputCommand = new RelayCommand(SelectRepairOutput);
        RepairFileCommand = new AsyncRelayCommand(RepairFile, () => !IsProcessing && !string.IsNullOrEmpty(RepairInput));

        SelectTrimInputCommand = new RelayCommand(SelectTrimInput);
        SelectTrimOutputCommand = new RelayCommand(SelectTrimOutput);
        TrimMediaCommand = new AsyncRelayCommand(TrimMedia, () => !IsProcessing && !string.IsNullOrEmpty(TrimInput));

        SelectCropInputCommand = new RelayCommand(SelectCropInput);
        SelectCropOutputCommand = new RelayCommand(SelectCropOutput);
        CropVideoCommand = new AsyncRelayCommand(CropVideo, () => !IsProcessing && !string.IsNullOrEmpty(CropInput));

        AddMediaJoinFileCommand = new RelayCommand(AddMediaJoinFile);
        RemoveMediaJoinFileCommand = new RelayCommand<string>(RemoveMediaJoinFile);
        SelectMediaJoinOutputCommand = new RelayCommand(SelectMediaJoinOutput);
        JoinMediaCommand = new AsyncRelayCommand(JoinMedia, () => !IsProcessing && MediaJoinFiles.Count > 0);
    }

    private void SelectAudioExtractInput()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "视频文件|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv|所有文件|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            AudioExtractInput = dialog.FileName;
            AudioExtractOutput = Path.ChangeExtension(dialog.FileName, SelectedAudioFormat);
        }
    }

    private void SelectAudioExtractOutput()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "音频文件|*.mp3;*.wav;*.aac;*.flac;*.ogg|所有文件|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            AudioExtractOutput = dialog.FileName;
        }
    }

    private async Task ExtractAudio()
    {
        IsProcessing = true;
        Progress = 0;
        StatusMessage = "正在提取音频...";

        try
        {
            var progress = new Progress<double>(p => Progress = p * 100);
            var result = await _toolboxService.ExtractAudioAsync(
                AudioExtractInput, AudioExtractOutput, SelectedAudioFormat, AudioBitrate, progress);

            StatusMessage = result.Success ? "音频提取成功！" : result.Message;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private void AddAudioMergeFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "音频文件|*.mp3;*.wav;*.aac;*.flac;*.ogg|所有文件|*.*",
            Multiselect = true
        };
        if (dialog.ShowDialog() == true)
        {
            foreach (var file in dialog.FileNames)
            {
                if (!AudioMergeFiles.Contains(file))
                {
                    AudioMergeFiles.Add(file);
                }
            }
        }
    }

    private void RemoveAudioMergeFile(string? file)
    {
        if (!string.IsNullOrEmpty(file))
        {
            AudioMergeFiles.Remove(file);
        }
    }

    private void SelectAudioMergeOutput()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "音频文件|*.mp3;*.wav;*.aac;*.flac|所有文件|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            AudioMergeOutput = dialog.FileName;
        }
    }

    private async Task MergeAudio()
    {
        if (string.IsNullOrEmpty(AudioMergeOutput))
        {
            StatusMessage = "请选择输出文件";
            return;
        }

        IsProcessing = true;
        Progress = 0;
        StatusMessage = "正在合并音频...";

        try
        {
            var progress = new Progress<double>(p => Progress = p * 100);
            var result = await _toolboxService.MergeAudioAsync(
                AudioMergeFiles.ToList(), AudioMergeOutput, progress);

            StatusMessage = result.Success ? "音频合并成功！" : result.Message;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private void SelectRepairInput()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "媒体文件|*.mp4;*.avi;*.mkv;*.mov;*.mp3;*.wav|所有文件|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            RepairInput = dialog.FileName;
            RepairOutput = Path.Combine(
                Path.GetDirectoryName(dialog.FileName) ?? "",
                Path.GetFileNameWithoutExtension(dialog.FileName) + "_repaired" + Path.GetExtension(dialog.FileName));
        }
    }

    private void SelectRepairOutput()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "媒体文件|*.mp4;*.avi;*.mkv;*.mov;*.mp3;*.wav|所有文件|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            RepairOutput = dialog.FileName;
        }
    }

    private async Task RepairFile()
    {
        IsProcessing = true;
        Progress = 0;
        StatusMessage = "正在修复文件...";

        try
        {
            var progress = new Progress<double>(p => Progress = p * 100);
            var result = await _toolboxService.RepairFileAsync(RepairInput, RepairOutput, progress);

            StatusMessage = result.Success ? "文件修复成功！" : result.Message;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private void SelectTrimInput()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "媒体文件|*.mp4;*.avi;*.mkv;*.mov;*.mp3;*.wav|所有文件|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            TrimInput = dialog.FileName;
            TrimOutput = Path.Combine(
                Path.GetDirectoryName(dialog.FileName) ?? "",
                Path.GetFileNameWithoutExtension(dialog.FileName) + "_trimmed" + Path.GetExtension(dialog.FileName));
        }
    }

    private void SelectTrimOutput()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "媒体文件|*.mp4;*.avi;*.mkv;*.mov;*.mp3;*.wav|所有文件|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            TrimOutput = dialog.FileName;
        }
    }

    private async Task TrimMedia()
    {
        IsProcessing = true;
        Progress = 0;
        StatusMessage = "正在裁剪媒体...";

        try
        {
            var progress = new Progress<double>(p => Progress = p * 100);
            var result = await _toolboxService.TrimMediaAsync(
                TrimInput, TrimOutput, TrimStartTime, TrimEndTime, progress);

            StatusMessage = result.Success ? "媒体裁剪成功！" : result.Message;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private void SelectCropInput()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "视频文件|*.mp4;*.avi;*.mkv;*.mov|所有文件|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            CropInput = dialog.FileName;
            CropOutput = Path.Combine(
                Path.GetDirectoryName(dialog.FileName) ?? "",
                Path.GetFileNameWithoutExtension(dialog.FileName) + "_cropped" + Path.GetExtension(dialog.FileName));
        }
    }

    private void SelectCropOutput()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "视频文件|*.mp4;*.avi;*.mkv;*.mov|所有文件|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            CropOutput = dialog.FileName;
        }
    }

    private async Task CropVideo()
    {
        IsProcessing = true;
        Progress = 0;
        StatusMessage = "正在裁剪视频...";

        try
        {
            var progress = new Progress<double>(p => Progress = p * 100);
            var result = await _toolboxService.CropVideoAsync(
                CropInput, CropOutput, CropX, CropY, CropWidth, CropHeight, progress);

            StatusMessage = result.Success ? "视频裁剪成功！" : result.Message;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private void AddMediaJoinFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "媒体文件|*.mp4;*.avi;*.mkv;*.mov;*.mp3;*.wav|所有文件|*.*",
            Multiselect = true
        };
        if (dialog.ShowDialog() == true)
        {
            foreach (var file in dialog.FileNames)
            {
                if (!MediaJoinFiles.Contains(file))
                {
                    MediaJoinFiles.Add(file);
                }
            }
        }
    }

    private void RemoveMediaJoinFile(string? file)
    {
        if (!string.IsNullOrEmpty(file))
        {
            MediaJoinFiles.Remove(file);
        }
    }

    private void SelectMediaJoinOutput()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "媒体文件|*.mp4;*.avi;*.mkv;*.mov;*.mp3;*.wav|所有文件|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            MediaJoinOutput = dialog.FileName;
        }
    }

    private async Task JoinMedia()
    {
        if (string.IsNullOrEmpty(MediaJoinOutput))
        {
            StatusMessage = "请选择输出文件";
            return;
        }

        IsProcessing = true;
        Progress = 0;
        StatusMessage = "正在合并媒体...";

        try
        {
            var progress = new Progress<double>(p => Progress = p * 100);
            var result = await _toolboxService.JoinMediaAsync(
                MediaJoinFiles.ToList(), MediaJoinOutput, progress);

            StatusMessage = result.Success ? "媒体合并成功！" : result.Message;
        }
        finally
        {
            IsProcessing = false;
        }
    }
}
