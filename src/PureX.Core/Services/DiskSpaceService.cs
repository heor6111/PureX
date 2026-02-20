using System.Runtime.InteropServices;

namespace PureX.Core.Services;

public class DiskSpaceCheckResult
{
    public bool HasEnoughSpace { get; set; }
    public long RequiredBytes { get; set; }
    public long AvailableBytes { get; set; }
    public string RequiredFormatted => FormatSize(RequiredBytes);
    public string AvailableFormatted => FormatSize(AvailableBytes);
    public string? ErrorMessage { get; set; }

    private static string FormatSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        int unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }
        return $"{size:F2} {units[unitIndex]}";
    }
}

public class DiskSpaceService
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetDiskFreeSpaceEx(
        string lpDirectoryName,
        out ulong lpFreeBytesAvailable,
        out ulong lpTotalNumberOfBytes,
        out ulong lpTotalNumberOfFreeBytes);

    public DiskSpaceCheckResult CheckDiskSpace(string path, long requiredBytes)
    {
        var result = new DiskSpaceCheckResult
        {
            RequiredBytes = requiredBytes
        };

        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                result.HasEnoughSpace = false;
                result.ErrorMessage = "输出目录未设置";
                return result;
            }

            string checkPath = path;
            while (!string.IsNullOrEmpty(checkPath) && !Directory.Exists(checkPath))
            {
                checkPath = Path.GetDirectoryName(checkPath) ?? string.Empty;
            }

            if (string.IsNullOrEmpty(checkPath))
            {
                checkPath = Path.GetPathRoot(path) ?? "C:\\";
            }

            if (IsNetworkPath(path))
            {
                result.AvailableBytes = GetNetworkDriveAvailableSpace(path);
            }
            else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                if (!GetDiskFreeSpaceEx(checkPath, out ulong freeBytes, out _, out _))
                {
                    result.HasEnoughSpace = false;
                    result.ErrorMessage = "无法获取磁盘空间信息";
                    return result;
                }
                result.AvailableBytes = (long)freeBytes;
            }
            else
            {
                var driveInfo = new DriveInfo(checkPath);
                result.AvailableBytes = driveInfo.AvailableFreeSpace;
            }

            result.HasEnoughSpace = result.AvailableBytes >= requiredBytes;
            
            if (!result.HasEnoughSpace)
            {
                result.ErrorMessage = $"磁盘空间不足！\n" +
                    $"预估需要: {result.RequiredFormatted}\n" +
                    $"剩余空间: {result.AvailableFormatted}";
            }
        }
        catch (Exception ex)
        {
            result.HasEnoughSpace = false;
            result.ErrorMessage = $"检查磁盘空间时出错: {ex.Message}";
        }

        return result;
    }

    private bool IsNetworkPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        return path.StartsWith(@"\\") || path.StartsWith("//");
    }

    private long GetNetworkDriveAvailableSpace(string path)
    {
        try
        {
            string rootPath = path;
            while (!string.IsNullOrEmpty(rootPath) && !Directory.Exists(rootPath))
            {
                rootPath = Path.GetDirectoryName(rootPath) ?? string.Empty;
            }

            if (string.IsNullOrEmpty(rootPath))
            {
                return long.MaxValue / 2;
            }

            var driveInfo = new DriveInfo(rootPath);
            return driveInfo.AvailableFreeSpace;
        }
        catch
        {
            return long.MaxValue / 2;
        }
    }

    public bool TryCreateDirectory(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}
