using System.Drawing;
using System.Drawing.Imaging;
using KrLensServer.Core.Models;

namespace KrLensServer.Core.Filters;

internal static class BitmapFilterSupport
{
    public static Bitmap CreateBitmap(BitmapBuffer source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var bitmap = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var bitmapData = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

        try
        {
            var scan0 = bitmapData.Scan0;
            var stride = bitmapData.Stride;

            unsafe
            {
                byte* p = (byte*)(void*)scan0;
                var noffset = stride - (bitmap.Width * 3);
                var sourceIndex = 0;

                for (var y = 0; y < bitmap.Height; ++y)
                {
                    for (var x = 0; x < bitmap.Width; ++x)
                    {
                        if (source.Channels == 1)
                        {
                            var value = source.Pixels[sourceIndex];
                            p[0] = value;
                            p[1] = value;
                            p[2] = value;
                            ++sourceIndex;
                        }
                        else
                        {
                            p[0] = source.Pixels[sourceIndex + 2];
                            p[1] = source.Pixels[sourceIndex + 1];
                            p[2] = source.Pixels[sourceIndex];
                            sourceIndex += 3;
                        }

                        p += 3;
                    }

                    p += noffset;
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return bitmap;
    }

    public static BitmapBuffer ToBuffer(Bitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        using var normalized = NormalizeBitmap(bitmap);
        var buffer = new BitmapBuffer(normalized.Width, normalized.Height, 3);
        var rect = new Rectangle(0, 0, normalized.Width, normalized.Height);
        var bitmapData = normalized.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

        try
        {
            var scan0 = bitmapData.Scan0;
            var stride = bitmapData.Stride;
            var destinationIndex = 0;

            unsafe
            {
                byte* p = (byte*)(void*)scan0;
                var noffset = stride - (normalized.Width * 3);

                for (var y = 0; y < normalized.Height; ++y)
                {
                    for (var x = 0; x < normalized.Width; ++x)
                    {
                        buffer.Pixels[destinationIndex] = p[2];
                        buffer.Pixels[destinationIndex + 1] = p[1];
                        buffer.Pixels[destinationIndex + 2] = p[0];
                        destinationIndex += 3;
                        p += 3;
                    }

                    p += noffset;
                }
            }
        }
        finally
        {
            normalized.UnlockBits(bitmapData);
        }

        return buffer;
    }

    public static Bitmap CreateEmpty(Bitmap source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
    }

    public static Bitmap NormalizeBitmap(Bitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        if (bitmap.PixelFormat == PixelFormat.Format24bppRgb)
        {
            return (Bitmap)bitmap.Clone();
        }

        var normalized = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(normalized);
        graphics.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
        return normalized;
    }

    public static unsafe byte GetChannel(byte* scan0, int stride, int width, int height, int x, int y, int channel)
    {
        if (x < 0)
        {
            x = 0;
        }
        else if (x >= width)
        {
            x = width - 1;
        }

        if (y < 0)
        {
            y = 0;
        }
        else if (y >= height)
        {
            y = height - 1;
        }

        return scan0[(y * stride) + (x * 3) + channel];
    }
}
