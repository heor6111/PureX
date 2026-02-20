using System.Reflection;

namespace PureX.Core.Services;

public class FFmpegResourceManager : IDisposable
{
    private static FFmpegResourceManager? _instance;
    private static readonly object _lock = new();
    
    private readonly string _extractDirectory;
    private string? _ffmpegPath;
    private string? _ffprobePath;
    private bool _isExtracted;
    private bool _isDisposed;

    public static FFmpegResourceManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new FFmpegResourceManager();
                }
            }
            return _instance;
        }
    }

    private FFmpegResourceManager()
    {
        _extractDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PureX",
            "ffmpeg"
        );
    }

    public string FFmpegPath
    {
        get
        {
            EnsureAvailable();
            return _ffmpegPath!;
        }
    }

    public string FFprobePath
    {
        get
        {
            EnsureAvailable();
            return _ffprobePath!;
        }
    }

    public string FFmpegDirectory
    {
        get
        {
            EnsureAvailable();
            return _extractDirectory;
        }
    }

    public bool IsFFmpegAvailable()
    {
        return !string.IsNullOrEmpty(_ffmpegPath) && File.Exists(_ffmpegPath) 
            && !string.IsNullOrEmpty(_ffprobePath) && File.Exists(_ffprobePath);
    }

    private void EnsureAvailable()
    {
        if (_isExtracted && IsFFmpegAvailable()) return;

        lock (_lock)
        {
            if (_isExtracted && IsFFmpegAvailable()) return;

            _ffmpegPath = Path.Combine(_extractDirectory, "ffmpeg.exe");
            _ffprobePath = Path.Combine(_extractDirectory, "ffprobe.exe");

            if (!IsFFmpegAvailable())
            {
                ExtractFromEmbeddedResources();
            }

            if (!IsFFmpegAvailable())
            {
                var foundPath = FindFFmpegInCommonLocations();
                if (!string.IsNullOrEmpty(foundPath))
                {
                    _ffmpegPath = Path.Combine(foundPath, "ffmpeg.exe");
                    _ffprobePath = Path.Combine(foundPath, "ffprobe.exe");
                }
            }

            if (!IsFFmpegAvailable())
            {
                throw new InvalidOperationException(
                    "FFmpeg未找到。程序已内置FFmpeg，如仍无法运行，请检查程序完整性。\n" +
                    "您也可以从 https://ffmpeg.org/download.html 下载FFmpeg并放置到程序目录。");
            }

            _isExtracted = true;
        }
    }

    private void ExtractFromEmbeddedResources()
    {
        try
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();

            var ffmpegResource = resourceNames.FirstOrDefault(n => n.EndsWith("ffmpeg.exe"));
            var ffprobeResource = resourceNames.FirstOrDefault(n => n.EndsWith("ffprobe.exe"));

            if (ffmpegResource == null && ffprobeResource == null)
            {
                return;
            }

            if (!Directory.Exists(_extractDirectory))
            {
                Directory.CreateDirectory(_extractDirectory);
            }

            if (ffmpegResource != null && !File.Exists(_ffmpegPath))
            {
                ExtractResource(assembly, ffmpegResource, _ffmpegPath!);
            }

            if (ffprobeResource != null && !File.Exists(_ffprobePath))
            {
                ExtractResource(assembly, ffprobeResource, _ffprobePath!);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"提取FFmpeg资源失败: {ex.Message}");
        }
    }

    private void ExtractResource(Assembly assembly, string resourceName, string targetPath)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return;

        var tempPath = targetPath + ".tmp";
        using (var fileStream = File.Create(tempPath))
        {
            stream.CopyTo(fileStream);
        }

        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }
        
        File.Move(tempPath, targetPath);
    }

    public static string? FindFFmpegInCommonLocations()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        
        var locations = new[]
        {
            Path.Combine(baseDir, "ffmpeg", "bin"),
            Path.Combine(baseDir, "..", "..", "..", "..", "tools", "ffmpeg", "bin"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PureX", "ffmpeg"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "ffmpeg", "bin"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ffmpeg", "bin"),
        };

        foreach (var location in locations)
        {
            try
            {
                var fullPath = Path.GetFullPath(location);
                var ffmpegExe = Path.Combine(fullPath, "ffmpeg.exe");
                if (File.Exists(ffmpegExe))
                {
                    return fullPath;
                }
            }
            catch
            {
            }
        }

        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            foreach (var path in pathEnv.Split(Path.PathSeparator))
            {
                try
                {
                    var ffmpegExe = Path.Combine(path.Trim(), "ffmpeg.exe");
                    if (File.Exists(ffmpegExe))
                    {
                        return path.Trim();
                    }
                }
                catch
                {
                }
            }
        }

        return null;
    }

    public void Cleanup()
    {
        if (_isDisposed) return;

        try
        {
            if (Directory.Exists(_extractDirectory))
            {
                Directory.Delete(_extractDirectory, recursive: true);
            }
        }
        catch
        {
        }

        _isDisposed = true;
    }

    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }

    ~FFmpegResourceManager()
    {
        Cleanup();
    }
}
