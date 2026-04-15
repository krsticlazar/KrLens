using KrLensServer.Core.Exceptions;
using KrLensServer.Core.Msi;
using KrLensServer.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace KrLensServer.Core.Services;

public sealed class ImageService
{
    private readonly MsiEncoder _msiEncoder;
    private readonly MsiDecoder _msiDecoder;

    public ImageService(MsiEncoder msiEncoder, MsiDecoder msiDecoder)
    {
        _msiEncoder = msiEncoder;
        _msiDecoder = msiDecoder;
    }

    public async Task<BitmapBuffer> LoadAsync(Stream stream, string fileNameOrExtension, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileNameOrExtension);

        var format = NormalizeFormat(fileNameOrExtension);
        if (format == "msi")
        {
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, cancellationToken);
            return _msiDecoder.Decode(memory.ToArray());
        }

        stream.Position = 0;
        using var image = await Image.LoadAsync<Rgba32>(stream, cancellationToken);
        var pixels = new byte[BitmapBuffer.GetPixelArrayLength(image.Width, image.Height, 3)];

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    var offset = ((y * image.Width) + x) * 3;
                    pixels[offset] = row[x].R;
                    pixels[offset + 1] = row[x].G;
                    pixels[offset + 2] = row[x].B;
                }
            }
        });

        return new BitmapBuffer(image.Width, image.Height, 3, pixels, takeOwnership: true);
    }

    public async Task<byte[]> EncodeAsync(BitmapBuffer buffer, string format, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        var normalized = NormalizeFormat(format);
        if (normalized == "msi")
        {
            return _msiEncoder.Encode(buffer);
        }

        using var memory = new MemoryStream();
        if (buffer.Channels == 1)
        {
            using var image = new Image<L8>(buffer.Width, buffer.Height);
            image.ProcessPixelRows(accessor =>
            {
                for (var y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (var x = 0; x < row.Length; x++)
                    {
                        row[x] = new L8(buffer.Pixels[(y * buffer.Width) + x]);
                    }
                }
            });

            await SaveStandardAsync(image, normalized, memory, cancellationToken);
        }
        else
        {
            using var image = new Image<Rgb24>(buffer.Width, buffer.Height);
            image.ProcessPixelRows(accessor =>
            {
                for (var y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (var x = 0; x < row.Length; x++)
                    {
                        var offset = ((y * buffer.Width) + x) * 3;
                        row[x] = new Rgb24(buffer.Pixels[offset], buffer.Pixels[offset + 1], buffer.Pixels[offset + 2]);
                    }
                }
            });

            await SaveStandardAsync(image, normalized, memory, cancellationToken);
        }

        return memory.ToArray();
    }

    public string GetContentType(string format)
    {
        return NormalizeFormat(format) switch
        {
            "png" => "image/png",
            "jpeg" => "image/jpeg",
            "bmp" => "image/bmp",
            "gif" => "image/gif",
            "msi" => "application/octet-stream",
            var unsupported => throw new UnsupportedImageFormatException(unsupported),
        };
    }

    private static string NormalizeFormat(string format)
    {
        var candidate = format.Trim();
        if (candidate.Contains('.') || candidate.Contains(Path.DirectorySeparatorChar) || candidate.Contains(Path.AltDirectorySeparatorChar))
        {
            var extension = Path.GetExtension(candidate);
            if (!string.IsNullOrWhiteSpace(extension))
            {
                candidate = extension;
            }
        }

        var normalized = candidate.Trim().TrimStart('.').ToLowerInvariant();
        return normalized switch
        {
            "jpg" => "jpeg",
            "jpeg" => "jpeg",
            "png" => "png",
            "bmp" => "bmp",
            "gif" => "gif",
            "msi" => "msi",
            _ => throw new UnsupportedImageFormatException(format),
        };
    }

    private static Task SaveStandardAsync<TPixel>(
        Image<TPixel> image,
        string format,
        Stream output,
        CancellationToken cancellationToken)
        where TPixel : unmanaged, IPixel<TPixel>
    {
        return format switch
        {
            "png" => image.SaveAsPngAsync(output, cancellationToken),
            "jpeg" => image.SaveAsJpegAsync(output, cancellationToken),
            "bmp" => image.SaveAsBmpAsync(output, cancellationToken),
            "gif" => image.SaveAsGifAsync(output, cancellationToken),
            _ => throw new UnsupportedImageFormatException(format),
        };
    }
}
