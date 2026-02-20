namespace PureX.Core.Models;

public class ConversionResult
{
    public bool Success { get; set; }
    public string? OutputPath { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public long OriginalSizeBytes { get; set; }
    public long OutputSizeBytes { get; set; }

    public static ConversionResult Succeeded(string outputPath, TimeSpan processingTime, long originalSize, long outputSize)
    {
        return new ConversionResult
        {
            Success = true,
            OutputPath = outputPath,
            ProcessingTime = processingTime,
            OriginalSizeBytes = originalSize,
            OutputSizeBytes = outputSize
        };
    }

    public static ConversionResult Failed(string errorMessage)
    {
        return new ConversionResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
