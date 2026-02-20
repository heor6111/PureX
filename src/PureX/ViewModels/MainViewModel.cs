using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Windows.Input;
using PureX.Models;
using PureX.Core.Models;
using PureX.Core.Services;

namespace PureX.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly Core.ConversionService _conversionService;
    private readonly DiskSpaceService _diskSpaceService;
    private readonly OutputSizeEstimator _sizeEstimator;
    private readonly FormatConversionService _formatService;
    private ConversionTaskScheduler? _taskScheduler;
    private CancellationTokenSource? _cancellationTokenSource;
    
    public ObservableCollection<FileItem> Files { get; } = new();
    
    private FileItem? _selectedFile;
    public FileItem? SelectedFile
    {
        get => _selectedFile;
        set
        {
            _selectedFile = value;
            OnPropertyChanged();
        }
    }

    private bool _isConverting;
    public bool IsConverting
    {
        get => _isConverting;
        set
        {
            _isConverting = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotConverting));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool IsNotConverting => !IsConverting;

    private bool _isPaused;
    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            _isPaused = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanPause));
            OnPropertyChanged(nameof(CanResume));
        }
    }

    public bool CanPause => IsConverting && !IsPaused;
    public bool CanResume => IsConverting && IsPaused;

    private string _statusMessage = "请拖拽文件到此处或点击选择文件";
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    private string _outputDirectory = string.Empty;
    public string OutputDirectory
    {
        get => _outputDirectory;
        set
        {
            _outputDirectory = value;
            OnPropertyChanged();
        }
    }

    private double _totalProgress;
    public double TotalProgress
    {
        get => _totalProgress;
        set
        {
            _totalProgress = value;
            OnPropertyChanged();
        }
    }

    private string _progressText = string.Empty;
    public string ProgressText
    {
        get => _progressText;
        set
        {
            _progressText = value;
            OnPropertyChanged();
        }
    }

    private string _speedText = string.Empty;
    public string SpeedText
    {
        get => _speedText;
        set
        {
            _speedText = value;
            OnPropertyChanged();
        }
    }

    private string _timeRemainingText = string.Empty;
    public string TimeRemainingText
    {
        get => _timeRemainingText;
        set
        {
            _timeRemainingText = value;
            OnPropertyChanged();
        }
    }

    public ICommand AddFilesCommand { get; }
    public ICommand RemoveFileCommand { get; }
    public ICommand ClearFilesCommand { get; }
    public ICommand ConvertCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand ResumeCommand { get; }
    public ICommand RetryCommand { get; }
    public ICommand RetryAllCommand { get; }
    public ICommand SelectOutputDirectoryCommand { get; }
    public ICommand OpenOutputFolderCommand { get; }
    public ICommand AdvancedSettingsCommand { get; }

    private ConversionOptions _currentOptions = new();
    public ConversionOptions CurrentOptions
    {
        get => _currentOptions;
        set
        {
            _currentOptions = value;
            OnPropertyChanged();
        }
    }

    public MainViewModel()
    {
        _conversionService = new Core.ConversionService();
        _diskSpaceService = new DiskSpaceService();
        _sizeEstimator = new OutputSizeEstimator();
        _formatService = new FormatConversionService();
        
        OutputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "FileConverter输出");
        
        AddFilesCommand = new RelayCommand(AddFiles);
        RemoveFileCommand = new RelayCommand<FileItem>(RemoveFile);
        ClearFilesCommand = new RelayCommand(ClearFiles, () => Files.Count > 0 && !IsConverting);
        ConvertCommand = new AsyncRelayCommand(ConvertFiles, () => Files.Count > 0 && !IsConverting);
        CancelCommand = new RelayCommand(CancelConversion, () => IsConverting);
        PauseCommand = new RelayCommand(PauseConversion, () => CanPause);
        ResumeCommand = new RelayCommand(ResumeConversion, () => CanResume);
        RetryCommand = new RelayCommand<FileItem>(RetryFile, f => f != null && f.Status == ConversionStatus.Failed && !IsConverting);
        RetryAllCommand = new RelayCommand(RetryAllFailed, () => Files.Any(f => f.Status == ConversionStatus.Failed) && !IsConverting);
        SelectOutputDirectoryCommand = new RelayCommand(SelectOutputDirectory);
        OpenOutputFolderCommand = new RelayCommand(OpenOutputFolder, () => Directory.Exists(OutputDirectory));
        AdvancedSettingsCommand = new RelayCommand(OpenAdvancedSettings, () => !IsConverting);
    }

    public void AddDroppedFiles(string[] filePaths)
    {
        foreach (var filePath in filePaths)
        {
            AddFileIfValid(filePath);
        }
        UpdateStatus();
    }

    private void AddFiles()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Multiselect = true,
            Filter = "所有支持的文件|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm;*.mp3;*.wav;*.flac;*.aac;*.ogg;*.m4a;*.jpg;*.jpeg;*.png;*.webp;*.gif;*.bmp;*.tiff|" +
                     "视频文件|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm|" +
                     "音频文件|*.mp3;*.wav;*.flac;*.aac;*.ogg;*.m4a|" +
                     "图片文件|*.jpg;*.jpeg;*.png;*.webp;*.gif;*.bmp;*.tiff",
            Title = "选择要转换的文件"
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var filePath in dialog.FileNames)
            {
                AddFileIfValid(filePath);
            }
            UpdateStatus();
        }
    }

    private void AddFileIfValid(string filePath)
    {
        if (!File.Exists(filePath)) return;
        if (Files.Any(f => f.FilePath == filePath)) return;

        var mediaType = MediaTypeHelper.GetMediaType(filePath);
        if (mediaType == null) return;

        var fileInfo = new FileInfo(filePath);
        var sourceFormat = Path.GetExtension(filePath);
        
        var fileItem = new FileItem
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            FileType = sourceFormat.ToUpperInvariant(),
            FileSize = fileInfo.Length,
            MediaType = mediaType.Value.ToString(),
            SourceFormat = sourceFormat,
            Status = ConversionStatus.Pending,
            ErrorMessage = string.Empty
        };
        
        fileItem.InitializeAvailableFormats(_formatService);

        Files.Add(fileItem);
    }

    private void RemoveFile(FileItem? file)
    {
        if (file != null && !IsConverting)
        {
            Files.Remove(file);
            UpdateStatus();
        }
    }

    private void ClearFiles()
    {
        if (!IsConverting)
        {
            Files.Clear();
            UpdateStatus();
        }
    }

    private void UpdateStatus()
    {
        if (Files.Count == 0)
        {
            StatusMessage = "请拖拽文件到此处或点击选择文件";
        }
        else
        {
            var completed = Files.Count(f => f.Status == ConversionStatus.Completed);
            var failed = Files.Count(f => f.Status == ConversionStatus.Failed);
            
            StatusMessage = $"已添加 {Files.Count} 个文件";
            if (completed > 0 || failed > 0)
            {
                StatusMessage += $" | 完成: {completed}";
                if (failed > 0) StatusMessage += $" | 失败: {failed}";
            }
        }
    }

    private void SelectOutputDirectory()
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            OutputDirectory = dialog.SelectedPath;
        }
    }

    private void OpenOutputFolder()
    {
        if (Directory.Exists(OutputDirectory))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = OutputDirectory,
                UseShellExecute = true,
                Verb = "open"
            });
        }
    }

    private void OpenAdvancedSettings()
    {
        var mediaType = "Video";
        if (Files.Count > 0)
        {
            mediaType = Files[0].MediaType;
        }
        
        var window = new Views.AdvancedSettingsWindow(CurrentOptions, mediaType)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        
        if (window.ShowDialog() == true)
        {
            CurrentOptions = window.Result;
        }
    }

    private async Task ConvertFiles()
    {
        if (string.IsNullOrEmpty(OutputDirectory))
        {
            System.Windows.MessageBox.Show("请选择输出目录", "提示", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (!Directory.Exists(OutputDirectory))
        {
            try
            {
                Directory.CreateDirectory(OutputDirectory);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"无法创建输出目录: {ex.Message}", "错误", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }
        }

        var filesToConvert = Files.Where(f => f.Status != ConversionStatus.Completed).ToList();
        if (filesToConvert.Count == 0)
        {
            System.Windows.MessageBox.Show("没有需要转换的文件", "提示", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        var spaceCheckResult = CheckDiskSpaceBeforeConversion(filesToConvert);
        if (!spaceCheckResult.HasEnoughSpace)
        {
            var result = System.Windows.MessageBox.Show(
                $"磁盘空间不足！\n\n" +
                $"预估需要: {spaceCheckResult.RequiredFormatted}\n" +
                $"剩余空间: {spaceCheckResult.AvailableFormatted}\n\n" +
                $"是否仍要继续转换？",
                "磁盘空间不足",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);
            
            if (result != System.Windows.MessageBoxResult.Yes)
            {
                return;
            }
        }

        IsConverting = true;
        IsPaused = false;
        _cancellationTokenSource = new CancellationTokenSource();
        
        var maxConcurrency = CurrentOptions.MaxConcurrency;
        _taskScheduler = new ConversionTaskScheduler(maxConcurrency);
        
        var totalFiles = filesToConvert.Count;
        var completedFiles = 0;

        StatusMessage = $"正在转换... (并发数: {maxConcurrency})";

        try
        {
            var conversionTasks = new List<Task>();
            
            foreach (var file in filesToConvert)
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                    break;

                var outputFormat = file.OutputFormat;
                if (string.IsNullOrEmpty(outputFormat))
                {
                    outputFormat = file.MediaType switch
                    {
                        "Video" => ".mp4",
                        "Audio" => ".mp3",
                        "Image" => ".jpg",
                        _ => ".mp4"
                    };
                }

                var outputPath = Path.Combine(OutputDirectory, 
                    Path.GetFileNameWithoutExtension(file.FileName) + outputFormat);

                var fileItem = file;
                var currentIndex = filesToConvert.IndexOf(file);
                var progress = new Progress<ConversionProgress>(p =>
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        fileItem.Progress = p.Progress;
                        UpdateProgressDisplay(completedFiles, totalFiles, p);
                    });
                });

                var taskId = file.FilePath;
                
                _taskScheduler.TaskStarted += (s, e) =>
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (e.Task.TaskId == fileItem.FilePath)
                        {
                            fileItem.Status = ConversionStatus.Converting;
                            fileItem.Progress = 0;
                        }
                    });
                };

                _taskScheduler.TaskCompleted += (s, e) =>
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (e.Task.TaskId == fileItem.FilePath)
                        {
                            if (e.Task.Status == ConversionTaskStatus.Completed)
                            {
                                fileItem.Status = ConversionStatus.Completed;
                                fileItem.Progress = 100;
                            }
                            else if (e.Task.Status == ConversionTaskStatus.Failed)
                            {
                                fileItem.Status = ConversionStatus.Failed;
                                fileItem.ErrorMessage = e.Task.ErrorMessage ?? "转换失败";
                            }
                            else if (e.Task.Status == ConversionTaskStatus.Cancelled)
                            {
                                fileItem.Status = ConversionStatus.Pending;
                            }
                            
                            completedFiles++;
                            TotalProgress = (double)completedFiles / totalFiles * 100;
                            
                            StatusMessage = $"正在转换... ({completedFiles}/{totalFiles})";
                        }
                    });
                };

                _taskScheduler.TaskQueued += (s, e) =>
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (e.Task.TaskId == fileItem.FilePath)
                        {
                            fileItem.Status = ConversionStatus.Waiting;
                        }
                    });
                };

                var conversionTask = _taskScheduler.EnqueueTask(
                    taskId,
                    async (ct) =>
                    {
                        var result = await _conversionService.ConvertAsync(
                            fileItem.FilePath, 
                            outputPath, 
                            options: CurrentOptions,
                            progress: progress,
                            cancellationToken: ct);
                        
                        if (!result.Success)
                        {
                            fileItem.ErrorMessage = result.ErrorMessage ?? "转换失败";
                            throw new Exception(result.ErrorMessage);
                        }
                    },
                    file.FileName
                );
            }

            while (completedFiles < totalFiles && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(100, _cancellationTokenSource.Token);
            }

            if (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                _taskScheduler.CancelAllTasks();
                StatusMessage = "转换已取消";
            }
            else
            {
                var failedCount = Files.Count(f => f.Status == ConversionStatus.Failed);
                var completedCount = Files.Count(f => f.Status == ConversionStatus.Completed);
                
                if (failedCount > 0)
                {
                    StatusMessage = $"转换完成 | 成功: {completedCount} | 失败: {failedCount}";
                    var result = System.Windows.MessageBox.Show(
                        $"转换完成！\n成功: {completedCount}\n失败: {failedCount}\n\n是否打开输出目录？",
                        "转换完成", 
                        System.Windows.MessageBoxButton.YesNo, 
                        System.Windows.MessageBoxImage.Information);
                    
                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        OpenOutputFolder();
                    }
                }
                else
                {
                    StatusMessage = $"转换完成！共 {completedCount} 个文件";
                    var result = System.Windows.MessageBox.Show(
                        $"文件转换完成！共 {completedCount} 个文件\n\n是否打开输出目录？",
                        "完成", 
                        System.Windows.MessageBoxButton.YesNo, 
                        System.Windows.MessageBoxImage.Information);
                    
                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        OpenOutputFolder();
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "转换已取消";
        }
        catch (Exception ex)
        {
            StatusMessage = $"转换失败：{ex.Message}";
            System.Windows.MessageBox.Show($"转换失败：{ex.Message}", "错误", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsConverting = false;
            IsPaused = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _taskScheduler?.Dispose();
            _taskScheduler = null;
            UpdateStatus();
            ProgressText = string.Empty;
            SpeedText = string.Empty;
            TimeRemainingText = string.Empty;
        }
    }

    private DiskSpaceCheckResult CheckDiskSpaceBeforeConversion(List<FileItem> filesToConvert)
    {
        long totalEstimatedSize = 0;

        foreach (var file in filesToConvert)
        {
            var outputFormat = file.OutputFormat;
            if (string.IsNullOrEmpty(outputFormat))
            {
                outputFormat = file.MediaType switch
                {
                    "Video" => ".mp4",
                    "Audio" => ".mp3",
                    "Image" => ".jpg",
                    _ => ".mp4"
                };
            }

            var estimatedSize = _sizeEstimator.EstimateOutputSize(file.FilePath, outputFormat, CurrentOptions);
            totalEstimatedSize += estimatedSize;
        }

        totalEstimatedSize = (long)(totalEstimatedSize * 1.1);

        return _diskSpaceService.CheckDiskSpace(OutputDirectory, totalEstimatedSize);
    }

    private void UpdateProgressDisplay(int completedFiles, int totalFiles, ConversionProgress currentProgress)
    {
        ProgressText = $"{completedFiles + 1}/{totalFiles} - {currentProgress.Progress:F0}%";
        
        if (currentProgress.SpeedMBps > 0)
        {
            SpeedText = $"速度: {currentProgress.SpeedMBps:F1} MB/s";
        }
        
        if (currentProgress.EstimatedTimeRemaining.HasValue && currentProgress.EstimatedTimeRemaining.Value.TotalSeconds > 0)
        {
            var remaining = currentProgress.EstimatedTimeRemaining.Value;
            if (remaining.TotalMinutes >= 1)
            {
                TimeRemainingText = $"剩余: {remaining.Minutes}分{remaining.Seconds}秒";
            }
            else
            {
                TimeRemainingText = $"剩余: {remaining.Seconds}秒";
            }
        }
    }

    private void CancelConversion()
    {
        _cancellationTokenSource?.Cancel();
        _taskScheduler?.CancelAllTasks();
        StatusMessage = "正在取消...";
    }

    private void PauseConversion()
    {
        IsPaused = true;
        StatusMessage = "已暂停";
    }

    private void ResumeConversion()
    {
        IsPaused = false;
        StatusMessage = "正在转换...";
    }

    private async void RetryFile(FileItem? file)
    {
        if (file == null || file.Status != ConversionStatus.Failed) return;

        if (string.IsNullOrEmpty(OutputDirectory) || !Directory.Exists(OutputDirectory))
        {
            System.Windows.MessageBox.Show("请选择有效的输出目录", "提示", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        file.Status = ConversionStatus.Pending;
        file.Progress = 0;
        file.ErrorMessage = string.Empty;

        var outputFormat = file.OutputFormat;
        if (string.IsNullOrEmpty(outputFormat))
        {
            outputFormat = file.MediaType switch
            {
                "Video" => ".mp4",
                "Audio" => ".mp3",
                "Image" => ".jpg",
                _ => ".mp4"
            };
        }

        var outputPath = Path.Combine(OutputDirectory, 
            Path.GetFileNameWithoutExtension(file.FileName) + outputFormat);

        file.Status = ConversionStatus.Converting;

        var progress = new Progress<ConversionProgress>(p =>
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                file.Progress = p.Progress;
            });
        });

        var result = await _conversionService.ConvertAsync(
            file.FilePath, 
            outputPath, 
            progress: progress);

        if (result.Success)
        {
            file.Status = ConversionStatus.Completed;
            file.Progress = 100;
        }
        else
        {
            file.Status = ConversionStatus.Failed;
            file.ErrorMessage = result.ErrorMessage ?? "转换失败";
        }

        UpdateStatus();
    }

    private async void RetryAllFailed()
    {
        var failedFiles = Files.Where(f => f.Status == ConversionStatus.Failed).ToList();
        if (failedFiles.Count == 0) return;

        IsConverting = true;
        StatusMessage = $"正在重试 {failedFiles.Count} 个失败文件...";

        foreach (var file in failedFiles)
        {
            file.Status = ConversionStatus.Pending;
            file.Progress = 0;
            file.ErrorMessage = string.Empty;
        }

        await ConvertFiles();
    }
}
