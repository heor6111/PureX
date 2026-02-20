using System.Collections.Concurrent;
using System.Diagnostics;
using PureX.Core.Converters;
using PureX.Core.Models;

namespace PureX.Core.Services;

public class BatchConversionService
{
    private readonly int _maxDegreeOfParallelism;

    public BatchConversionService(string ffmpegPath, int maxDegreeOfParallelism = 2)
    {
        _maxDegreeOfParallelism = Math.Max(1, Math.Min(maxDegreeOfParallelism, Environment.ProcessorCount));
        ConverterFactory.Initialize(ffmpegPath);
    }

    public async Task<BatchConversionResult> ConvertAsync(
        IEnumerable<ConversionTask> tasks,
        IProgress<BatchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var taskList = tasks.ToList();
        var results = new ConcurrentBag<ConversionResult>();
        var completedCount = 0;
        var failedCount = 0;
        var totalFiles = taskList.Count;
        var totalBytes = taskList.Sum(t => new FileInfo(t.InputPath).Length);

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = _maxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        var progressLock = new object();

        try
        {
            await Parallel.ForEachAsync(taskList, options, async (task, ct) =>
            {
                var result = await ConvertSingleFileAsync(task, ct);
                results.Add(result);

                lock (progressLock)
                {
                    if (result.Success)
                        completedCount++;
                    else
                        failedCount++;

                    var processedBytes = results
                        .Where(r => r.Success)
                        .Sum(r => r.OriginalSizeBytes);

                    progress?.Report(new BatchProgress
                    {
                        TotalFiles = totalFiles,
                        CompletedFiles = completedCount,
                        FailedFiles = failedCount,
                        CurrentFile = Path.GetFileName(task.InputPath),
                        TotalBytes = totalBytes,
                        ProcessedBytes = processedBytes,
                        ElapsedTime = stopwatch.Elapsed
                    });
                }
            });
        }
        catch (OperationCanceledException)
        {
        }

        stopwatch.Stop();

        return new BatchConversionResult
        {
            TotalFiles = totalFiles,
            CompletedFiles = completedCount,
            FailedFiles = failedCount,
            TotalTime = stopwatch.Elapsed,
            Results = results.ToList()
        };
    }

    private async Task<ConversionResult> ConvertSingleFileAsync(ConversionTask task, CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(task.InputPath))
            {
                return ConversionResult.Failed("转换失败: 找不到输入文件");
            }

            if (!ConverterFactory.CanConvert(task.InputPath, task.OutputPath))
            {
                return ConversionResult.Failed($"转换失败: 不支持的格式转换 {Path.GetExtension(task.InputPath)} → {Path.GetExtension(task.OutputPath)}");
            }

            var converter = ConverterFactory.CreateConverter(task.InputPath, task.OutputPath);
            if (converter == null)
            {
                return ConversionResult.Failed("转换失败: 找不到合适的转换器");
            }

            var outputDir = Path.GetDirectoryName(task.OutputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            return await converter.ConvertAsync(
                task.InputPath,
                task.OutputPath,
                task.Options,
                progress: null,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return ConversionResult.Failed("转换已取消");
        }
        catch (Exception ex)
        {
            return ConversionResult.Failed($"转换失败: {ex.Message}");
        }
    }
}

public class ConversionTask
{
    public string InputPath { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public ConversionOptions? Options { get; set; }
}

public class BatchProgress
{
    public int TotalFiles { get; set; }
    public int CompletedFiles { get; set; }
    public int FailedFiles { get; set; }
    public string? CurrentFile { get; set; }
    public long TotalBytes { get; set; }
    public long ProcessedBytes { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public double Progress => TotalFiles > 0 ? (double)(CompletedFiles + FailedFiles) / TotalFiles * 100 : 0;
    public double SpeedMBps => ElapsedTime.TotalSeconds > 0 ? ProcessedBytes / 1024.0 / 1024.0 / ElapsedTime.TotalSeconds : 0;
}

public class BatchConversionResult
{
    public int TotalFiles { get; set; }
    public int CompletedFiles { get; set; }
    public int FailedFiles { get; set; }
    public TimeSpan TotalTime { get; set; }
    public List<ConversionResult> Results { get; set; } = new();
    public bool AllSuccess => FailedFiles == 0 && CompletedFiles == TotalFiles;
}
