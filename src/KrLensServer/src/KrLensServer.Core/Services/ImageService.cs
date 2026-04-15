using System.Drawing;
using System.Drawing.Imaging;
using KrLensServer.Core.Filters;
using KrLensServer.Core.Exceptions;
using KrLensServer.Core.Msi;
using KrLensServer.Core.Models;

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
            using var rawMemory = new MemoryStream();
            await stream.CopyToAsync(rawMemory, cancellationToken);
            return _msiDecoder.Decode(rawMemory.ToArray());
        }

        await using var memory = new MemoryStream();
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        await stream.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;

        using var bitmap = new Bitmap(memory);
        return BitmapFilterSupport.ToBuffer(bitmap);
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

        cancellationToken.ThrowIfCancellationRequested();
        using var bitmap = BitmapFilterSupport.CreateBitmap(buffer);
        using var memory = new MemoryStream();
        bitmap.Save(memory, GetImageFormat(normalized));
        await memory.FlushAsync(cancellationToken);
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

    private static ImageFormat GetImageFormat(string format)
    {
        return format switch
        {
            "png" => ImageFormat.Png,
            "jpeg" => ImageFormat.Jpeg,
            "bmp" => ImageFormat.Bmp,
            "gif" => ImageFormat.Gif,
            _ => throw new UnsupportedImageFormatException(format),
        };
    }
}
