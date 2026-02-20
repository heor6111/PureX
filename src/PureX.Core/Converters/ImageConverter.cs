using System.Diagnostics;
using PureX.Core.Models;
using ImageMagick;

namespace PureX.Core.Converters;

public class ImageConverter : IConverter
{
    public async Task<ConversionResult> ConvertAsync(string inputPath, string outputPath, 
        ConversionOptions? options = null, 
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            if (!File.Exists(inputPath))
            {
                return ConversionResult.Failed("转换失败: 找不到输入文件");
            }

            options ??= new ConversionOptions();

            if (options.OverwriteExisting && File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            progress?.Report(new ConversionProgress { Progress = 0 });

            await Task.Run(() => ConvertImage(inputPath, outputPath, options, progress), cancellationToken);

            stopwatch.Stop();

            if (File.Exists(outputPath))
            {
                progress?.Report(new ConversionProgress { Progress = 100 });
                var originalSize = new FileInfo(inputPath).Length;
                var outputSize = new FileInfo(outputPath).Length;
                return ConversionResult.Succeeded(outputPath, stopwatch.Elapsed, originalSize, outputSize);
            }
            else
            {
                return ConversionResult.Failed("转换失败: 输出文件未生成");
            }
        }
        catch (OperationCanceledException)
        {
            return ConversionResult.Failed("转换已取消");
        }
        catch (MagickException ex)
        {
            var errorMsg = ParseImageMagickError(ex);
            return ConversionResult.Failed(errorMsg);
        }
        catch (UnauthorizedAccessException)
        {
            return ConversionResult.Failed("转换失败: 没有文件访问权限");
        }
        catch (IOException ex)
        {
            return ConversionResult.Failed($"转换失败: 文件操作错误 - {ex.Message}");
        }
        catch (Exception ex)
        {
            return ConversionResult.Failed($"转换失败: {ex.Message}");
        }
    }

    private string ParseImageMagickError(MagickException ex)
    {
        var message = ex.Message.ToLowerInvariant();

        if (message.Contains("no such file") || message.Contains("not found"))
        {
            return "转换失败: 找不到输入文件";
        }
        if (message.Contains("permission") || message.Contains("access"))
        {
            return "转换失败: 没有文件访问权限";
        }
        if (message.Contains("invalid") || message.Contains("corrupt"))
        {
            return "转换失败: 图片文件已损坏或格式无效";
        }
        if (message.Contains("unsupported") || message.Contains("not supported"))
        {
            return "转换失败: 不支持的图片格式";
        }
        if (message.Contains("memory") || message.Contains("out of"))
        {
            return "转换失败: 内存不足";
        }
        if (message.Contains("disk") || message.Contains("space"))
        {
            return "转换失败: 磁盘空间不足";
        }

        var cleanMsg = ex.Message;
        if (cleanMsg.Length > 100)
        {
            cleanMsg = cleanMsg.Substring(0, 100) + "...";
        }
        
        return $"转换失败: {cleanMsg}";
    }

    private void ConvertImage(string inputPath, string outputPath, ConversionOptions options, 
        IProgress<ConversionProgress>? progress)
    {
        progress?.Report(new ConversionProgress { Progress = 10 });
        
        using var image = new MagickImage(inputPath);
        
        progress?.Report(new ConversionProgress { Progress = 20 });
        
        var outputExt = Path.GetExtension(outputPath).ToLowerInvariant();
        var format = GetMagickFormat(outputExt);
        var imageOpts = options.Image ?? new ImageOptions();
        
        image.Format = format;

        if (imageOpts.Rotation.HasValue && imageOpts.Rotation.Value != 0)
        {
            image.Rotate(imageOpts.Rotation.Value);
        }

        if (imageOpts.FlipHorizontal == true)
        {
            image.Flop();
        }

        if (imageOpts.FlipVertical == true)
        {
            image.Flip();
        }

        progress?.Report(new ConversionProgress { Progress = 30 });

        var hasCrop = imageOpts.CropWidth.HasValue && imageOpts.CropHeight.HasValue;
        if (hasCrop)
        {
            var cropX = imageOpts.CropX ?? 0;
            var cropY = imageOpts.CropY ?? 0;
            var cropWidth = imageOpts.CropWidth!.Value;
            var cropHeight = imageOpts.CropHeight!.Value;
            
            image.Crop(new MagickGeometry(cropX, cropY, (uint)cropWidth, (uint)cropHeight));
        }

        progress?.Report(new ConversionProgress { Progress = 40 });

        var width = imageOpts.Width ?? options.Width;
        var height = imageOpts.Height ?? options.Height;
        
        if (width.HasValue || height.HasValue)
        {
            var keepAspect = imageOpts.KeepAspectRatio ?? true;
            
            if (width.HasValue && height.HasValue)
            {
                if (keepAspect)
                {
                    image.Resize(new MagickGeometry((uint)width.Value, (uint)height.Value)
                    {
                        FillArea = false
                    });
                }
                else
                {
                    image.Resize((uint)width.Value, (uint)height.Value);
                }
            }
            else if (width.HasValue)
            {
                image.Resize((uint)width.Value, 0);
            }
            else if (height.HasValue)
            {
                image.Resize(0, (uint)height.Value);
            }
        }

        progress?.Report(new ConversionProgress { Progress = 60 });

        var quality = imageOpts.Quality ?? options.ImageQuality ?? 90;
        
        switch (format)
        {
            case MagickFormat.Jpeg:
            case MagickFormat.Jpg:
                image.Quality = (uint)Math.Clamp(quality, 1, 100);
                break;

            case MagickFormat.Png:
                image.Quality = 100;
                image.Settings.SetDefine(MagickFormat.Png, "compression-level", "9");
                break;

            case MagickFormat.WebP:
                image.Quality = (uint)Math.Clamp(quality, 1, 100);
                break;

            case MagickFormat.Gif:
                image.Quality = 100;
                break;

            case MagickFormat.Bmp:
                break;

            case MagickFormat.Tiff:
                image.Quality = (uint)Math.Clamp(quality, 1, 100);
                break;
        }

        if (imageOpts.Dpi.HasValue)
        {
            image.Density = new Density(imageOpts.Dpi.Value, imageOpts.Dpi.Value);
        }

        if (!string.IsNullOrEmpty(imageOpts.BackgroundColor))
        {
            try
            {
                var bgColor = new MagickColor(imageOpts.BackgroundColor);
                image.BackgroundColor = bgColor;
            }
            catch
            {
            }
        }

        progress?.Report(new ConversionProgress { Progress = 80 });
        
        image.Write(outputPath);
        
        progress?.Report(new ConversionProgress { Progress = 95 });
    }

    private static MagickFormat GetMagickFormat(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => MagickFormat.Jpeg,
            ".png" => MagickFormat.Png,
            ".webp" => MagickFormat.WebP,
            ".gif" => MagickFormat.Gif,
            ".bmp" => MagickFormat.Bmp,
            ".tiff" or ".tif" => MagickFormat.Tiff,
            ".ico" => MagickFormat.Ico,
            _ => MagickFormat.Unknown
        };
    }

    public IEnumerable<string> GetSupportedInputFormats()
    {
        return new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".tiff", ".tif", ".ico" };
    }

    public IEnumerable<string> GetSupportedOutputFormats()
    {
        return new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".tiff", ".ico" };
    }
}
