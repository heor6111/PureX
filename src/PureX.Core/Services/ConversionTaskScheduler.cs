using System.Collections.Concurrent;
using System.Diagnostics;

namespace PureX.Core.Services;

public class ConversionTaskScheduler : IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentQueue<ConversionTaskItem> _waitingQueue;
    private readonly ConcurrentDictionary<string, ConversionTaskItem> _runningTasks;
    private readonly int _maxConcurrency;
    private readonly int _cpuAffinityMask;
    private bool _isDisposed;

    public int MaxConcurrency => _maxConcurrency;
    public int RunningCount => _runningTasks.Count;
    public int WaitingCount => _waitingQueue.Count;
    public bool HasAvailableSlots => _runningTasks.Count < _maxConcurrency;

    public event EventHandler<ConversionTaskEventArgs>? TaskStarted;
    public event EventHandler<ConversionTaskEventArgs>? TaskCompleted;
    public event EventHandler<ConversionTaskEventArgs>? TaskQueued;

    public ConversionTaskScheduler(int maxConcurrency = 3)
    {
        _maxConcurrency = Math.Clamp(maxConcurrency, 1, 5);
        _semaphore = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);
        _waitingQueue = new ConcurrentQueue<ConversionTaskItem>();
        _runningTasks = new ConcurrentDictionary<string, ConversionTaskItem>();
        _cpuAffinityMask = CalculateCpuAffinity();
    }

    private int CalculateCpuAffinity()
    {
        int totalCores = Environment.ProcessorCount;
        int coresToUse = Math.Max(1, totalCores / 2);
        int mask = 0;
        for (int i = 0; i < coresToUse; i++)
        {
            mask |= (1 << i);
        }
        return mask;
    }

    public string EnqueueTask(
        string taskId,
        Func<CancellationToken, Task> taskAction,
        string fileName)
    {
        var taskItem = new ConversionTaskItem
        {
            TaskId = taskId,
            TaskAction = taskAction,
            FileName = fileName,
            Status = ConversionTaskStatus.Waiting,
            EnqueueTime = DateTime.Now
        };

        _waitingQueue.Enqueue(taskItem);
        TaskQueued?.Invoke(this, new ConversionTaskEventArgs(taskItem));

        _ = ProcessQueueAsync();

        return taskId;
    }

    private async Task ProcessQueueAsync()
    {
        while (_waitingQueue.TryDequeue(out var taskItem) && !_isDisposed)
        {
            await _semaphore.WaitAsync();

            if (_isDisposed)
            {
                _semaphore.Release();
                break;
            }

            _ = ExecuteTaskAsync(taskItem);
        }
    }

    private async Task ExecuteTaskAsync(ConversionTaskItem taskItem)
    {
        taskItem.Status = ConversionTaskStatus.Running;
        taskItem.StartTime = DateTime.Now;
        _runningTasks[taskItem.TaskId] = taskItem;

        TaskStarted?.Invoke(this, new ConversionTaskEventArgs(taskItem));

        try
        {
            await taskItem.TaskAction(taskItem.CancellationToken);
            taskItem.Status = ConversionTaskStatus.Completed;
        }
        catch (OperationCanceledException)
        {
            taskItem.Status = ConversionTaskStatus.Cancelled;
            taskItem.ErrorMessage = "转换已取消";
        }
        catch (Exception ex)
        {
            taskItem.Status = ConversionTaskStatus.Failed;
            taskItem.ErrorMessage = ex.Message;
        }
        finally
        {
            taskItem.EndTime = DateTime.Now;
            _runningTasks.TryRemove(taskItem.TaskId, out _);
            _semaphore.Release();

            TaskCompleted?.Invoke(this, new ConversionTaskEventArgs(taskItem));

            if (_waitingQueue.Count > 0)
            {
                _ = ProcessQueueAsync();
            }
        }
    }

    public bool CancelTask(string taskId)
    {
        if (_runningTasks.TryGetValue(taskId, out var taskItem))
        {
            taskItem.CancellationTokenSource.Cancel();
            return true;
        }
        return false;
    }

    public void CancelAllTasks()
    {
        foreach (var task in _runningTasks.Values)
        {
            task.CancellationTokenSource.Cancel();
        }
    }

    public Process? ConfigureProcessPriority(Process? process)
    {
        if (process == null) return null;

        try
        {
            process.PriorityClass = ProcessPriorityClass.BelowNormal;

            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
            {
                process.ProcessorAffinity = (IntPtr)_cpuAffinityMask;
            }
        }
        catch
        {
        }

        return process;
    }

    public IEnumerable<ConversionTaskItem> GetWaitingTasks()
    {
        return _waitingQueue.ToArray();
    }

    public IEnumerable<ConversionTaskItem> GetRunningTasks()
    {
        return _runningTasks.Values.ToArray();
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;
        CancelAllTasks();
        _semaphore.Dispose();

        GC.SuppressFinalize(this);
    }
}

public class ConversionTaskItem
{
    public string TaskId { get; set; } = string.Empty;
    public Func<CancellationToken, Task> TaskAction { get; set; } = _ => Task.CompletedTask;
    public string FileName { get; set; } = string.Empty;
    public ConversionTaskStatus Status { get; set; }
    public DateTime EnqueueTime { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? ErrorMessage { get; set; }
    public CancellationTokenSource CancellationTokenSource { get; } = new();
    public CancellationToken CancellationToken => CancellationTokenSource.Token;
}

public enum ConversionTaskStatus
{
    Waiting,
    Running,
    Completed,
    Failed,
    Cancelled
}

public class ConversionTaskEventArgs : EventArgs
{
    public ConversionTaskItem Task { get; }

    public ConversionTaskEventArgs(ConversionTaskItem task)
    {
        Task = task;
    }
}
