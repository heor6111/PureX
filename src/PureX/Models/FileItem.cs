using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PureX.Core.Models;
using PureX.Core.Services;

namespace PureX.Models;

public class FileItem : INotifyPropertyChanged
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileSizeDisplay => FormatFileSize(FileSize);
    public string MediaType { get; set; } = string.Empty;
    public string SourceFormat { get; set; } = string.Empty;
    
    public ObservableCollection<FormatOption> AvailableFormats { get; } = new();
    
    private FormatOption? _selectedFormat;
    public FormatOption? SelectedFormat
    {
        get => _selectedFormat;
        set
        {
            if (_selectedFormat != null)
            {
                _selectedFormat.IsSelected = false;
            }
            
            _selectedFormat = value;
            
            if (_selectedFormat != null)
            {
                _selectedFormat.IsSelected = true;
                _outputFormat = _selectedFormat.Extension;
            }
            
            OnPropertyChanged();
            OnPropertyChanged(nameof(OutputFormat));
        }
    }
    
    private string _outputFormat = string.Empty;
    public string OutputFormat 
    { 
        get => _outputFormat;
        set
        {
            _outputFormat = value;
            OnPropertyChanged();
        }
    }
    
    private ConversionStatus _status;
    public ConversionStatus Status 
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
        }
    }
    
    public string StatusText => GetStatusText();
    
    private double _progress;
    public double Progress 
    {
        get => _progress;
        set
        {
            _progress = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
        }
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public void InitializeAvailableFormats(FormatConversionService formatService)
    {
        AvailableFormats.Clear();
        
        var formats = formatService.GetConvertibleFormats(SourceFormat);
        foreach (var format in formats)
        {
            AvailableFormats.Add(format);
        }
        
        if (AvailableFormats.Count > 0)
        {
            SelectedFormat = AvailableFormats[0];
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int i = 0;
        double size = bytes;
        while (size >= 1024 && i < suffixes.Length - 1)
        {
            size /= 1024;
            i++;
        }
        return $"{size:F1} {suffixes[i]}";
    }

    private string GetStatusText()
    {
        return Status switch
        {
            ConversionStatus.Pending => "等待转换",
            ConversionStatus.Waiting => "等待中",
            ConversionStatus.Converting => $"转换中 {Progress:F0}%",
            ConversionStatus.Completed => "已完成",
            ConversionStatus.Failed => string.IsNullOrEmpty(ErrorMessage) ? "失败" : "失败",
            _ => "未知"
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum ConversionStatus
{
    Pending,
    Waiting,
    Converting,
    Completed,
    Failed
}
