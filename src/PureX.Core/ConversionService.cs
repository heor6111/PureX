using PureX.Core.Converters;
using PureX.Core.Models;
using PureX.Core.Services;

namespace PureX.Core;

public class ConversionService
{
    private readonly string _ffmpegPath;

    public ConversionService(string? ffmpegPath = null)
    {
        if (string.IsNullOrEmpty(ffmpegPath))
        {
            ffmpegPath = FFmpegResourceManager.FindFFmpegInCommonLocations();
        }
        
        if (string.IsNullOrEmpty(ffmpegPath))
        {
            try
            {
                ffmpegPath = FFmpegResourceManager.Instance.FFmpegDirectory;
            }
            catch
            {
                ffmpegPath = string.Empty;
            }
        }
        
        _ffmpegPath = ffmpegPath ?? string.Empty;
        
        if (!string.IsNullOrEmpty(_ffmpegPath))
        {
            ConverterFactory.Initialize(_ffmpegPath);
        }
    }

    public async Task<ConversionResult> ConvertAsync(string inputPath, string outputPath, 
        ConversionOptions? options = null, 
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(inputPath))
        {
            return ConversionResult.Failed($"输入文件不存在: {inputPath}");
        }

        if (!ConverterFactory.CanConvert(inputPath, outputPath))
        {
            var inputExt = Path.GetExtension(inputPath);
            var outputExt = Path.GetExtension(outputPath);
            return ConversionResult.Failed($"不支持从 {inputExt} 转换到 {outputExt}");
        }

        var converter = ConverterFactory.CreateConverter(inputPath, outputPath);
        if (converter == null)
        {
            return ConversionResult.Failed("未找到适合的转换器");
        }

        return await converter.ConvertAsync(inputPath, outputPath, options, progress, cancellationToken);
    }

    public IEnumerable<string> GetSupportedInputFormats()
    {
        var formats = new List<string>();
        
        var imageConverter = new ImageConverter();
        formats.AddRange(imageConverter.GetSupportedInputFormats());
        
        if (!string.IsNullOrEmpty(_ffmpegPath) && Directory.Exists(_ffmpegPath))
        {
            try
            {
                var ffmpegConverter = new FFmpegConverter(_ffmpegPath);
                formats.AddRange(ffmpegConverter.GetSupportedInputFormats());
            }
            catch
            {
            }
        }
        
        return formats.Distinct();
    }

    public IEnumerable<string> GetSupportedOutputFormats(string inputPath)
    {
        var inputType = MediaTypeHelper.GetMediaType(inputPath);
        
        return inputType switch
        {
            MediaType.Image => new ImageConverter().GetSupportedOutputFormats(),
            MediaType.Video or MediaType.Audio when !string.IsNullOrEmpty(_ffmpegPath) 
                => new FFmpegConverter(_ffmpegPath).GetSupportedOutputFormats(),
            _ => Enumerable.Empty<string>()
        };
    }
}
