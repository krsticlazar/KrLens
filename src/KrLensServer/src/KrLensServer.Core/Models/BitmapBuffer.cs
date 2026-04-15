using System.Diagnostics;

namespace KrLensServer.Core.Models;

[DebuggerDisplay("{Width}x{Height}x{Channels}")]
public sealed class BitmapBuffer
{
    public BitmapBuffer(int width, int height, int channels)
        : this(width, height, channels, new byte[GetPixelArrayLength(width, height, channels)], takeOwnership: true)
    {
    }

    public BitmapBuffer(int width, int height, int channels, byte[] pixels, bool takeOwnership = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);

        if (channels is not (1 or 3))
        {
            throw new ArgumentOutOfRangeException(nameof(channels), "Channels must be 1 or 3.");
        }

        ArgumentNullException.ThrowIfNull(pixels);

        var expectedLength = GetPixelArrayLength(width, height, channels);
        if (pixels.Length != expectedLength)
        {
            throw new ArgumentException($"Pixel buffer length must be {expectedLength}.", nameof(pixels));
        }

        Width = width;
        Height = height;
        Channels = channels;
        Pixels = takeOwnership ? pixels : pixels.ToArray();
    }

    public int Width { get; }

    public int Height { get; }

    public int Channels { get; }

    public byte[] Pixels { get; }

    public int Stride => Width * Channels;

    public int PixelCount => Width * Height;

    public int GetOffset(int x, int y)
    {
        if (x < 0 || x >= Width)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        if (y < 0 || y >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(y));
        }

        return ((y * Width) + x) * Channels;
    }

    public BitmapBuffer Clone() => new(Width, Height, Channels, Pixels);

    public static int GetPixelArrayLength(int width, int height, int channels)
    {
        checked
        {
            return width * height * channels;
        }
    }
}
