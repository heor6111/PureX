namespace PureX.Core.Models;

public class ConversionProgress
{
    public double Progress { get; set; }
    public string? CurrentFile { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public double SpeedMBps { get; set; }
    public int CurrentIndex { get; set; }
    public int TotalFiles { get; set; }
}

public delegate void ProgressChangedEventHandler(object sender, ConversionProgress progress);
