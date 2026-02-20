using PureX.Core.Models;

namespace PureX.Core.Converters;

public interface IConverter
{
    Task<ConversionResult> ConvertAsync(string inputPath, string outputPath, 
        ConversionOptions? options = null, 
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default);
    
    IEnumerable<string> GetSupportedInputFormats();
    IEnumerable<string> GetSupportedOutputFormats();
}
