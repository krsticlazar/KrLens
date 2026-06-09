using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using KrLensServer.Core.Models;

namespace KrLensServer.Core.Services;

internal static class BitmapBufferCodec
{
    public static Bitmap CreateBitmap(BitmapBuffer source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var bitmap = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var bitmapData = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

        try
        {
            var width = bitmap.Width;
            var height = bitmap.Height;
            var channels = source.Channels;

            unsafe
            {
                byte* start = (byte*)(void*)bitmapData.Scan0;
                var stride = bitmapData.Stride;

                Parallel.For(0, height, y =>
                {
                    byte* current = start + (y * stride);
                    var sourceIndex = y * width * channels;

                    for (var x = 0; x < width; ++x)
                    {
                        if (channels == 1)
                        {
                            var value = source.Pixels[sourceIndex++];
                            current[0] = value;
                            current[1] = value;
                            current[2] = value;
                        }
                        else
                        {
                            current[0] = source.Pixels[sourceIndex + 2];
                            current[1] = source.Pixels[sourceIndex + 1];
                            current[2] = source.Pixels[sourceIndex];
                            sourceIndex += 3;
                        }

                        current += 3;
                    }
                });
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
            var width = normalized.Width;
            var height = normalized.Height;

            unsafe
            {
                byte* start = (byte*)(void*)bitmapData.Scan0;
                var stride = bitmapData.Stride;

                Parallel.For(0, height, y =>
                {
                    byte* current = start + (y * stride);
                    var destinationIndex = y * width * 3;

                    for (var x = 0; x < width; ++x)
                    {
                        buffer.Pixels[destinationIndex] = current[2];
                        buffer.Pixels[destinationIndex + 1] = current[1];
                        buffer.Pixels[destinationIndex + 2] = current[0];
                        destinationIndex += 3;
                        current += 3;
                    }
                });
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
