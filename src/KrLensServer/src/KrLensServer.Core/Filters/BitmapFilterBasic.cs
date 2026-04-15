using System.Drawing;
using System.Drawing.Imaging;
using KrLensServer.Core.Models;

namespace KrLensServer.Core.Filters;

internal static class BitmapFilterBasic
{
    public static BitmapBuffer Invert(BitmapBuffer source)
    {
        using var bitmap = BitmapFilterSupport.CreateBitmap(source);
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

        try
        {
            var scan0 = bitmapData.Scan0;
            var stride = bitmapData.Stride;

            unsafe
            {
                byte* p = (byte*)(void*)scan0;
                var noffset = stride - (bitmap.Width * 3);
                var nWidth = bitmap.Width * 3;

                for (var y = 0; y < bitmap.Height; ++y)
                {
                    for (var x = 0; x < nWidth; ++x)
                    {
                        p[0] = (byte)(255 - p[0]);
                        ++p;
                    }

                    p += noffset;
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return BitmapFilterSupport.ToBuffer(bitmap);
    }

    public static BitmapBuffer GrayScale(BitmapBuffer source)
    {
        using var bitmap = BitmapFilterSupport.CreateBitmap(source);
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

        try
        {
            var scan0 = bitmapData.Scan0;
            var stride = bitmapData.Stride;

            unsafe
            {
                byte* p = (byte*)(void*)scan0;
                var noffset = stride - (bitmap.Width * 3);

                for (var y = 0; y < bitmap.Height; ++y)
                {
                    for (var x = 0; x < bitmap.Width; ++x)
                    {
                        var gray = (byte)Math.Clamp((int)Math.Round((0.114 * p[0]) + (0.587 * p[1]) + (0.299 * p[2])), 0, 255);
                        p[0] = gray;
                        p[1] = gray;
                        p[2] = gray;
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

        return BitmapFilterSupport.ToBuffer(bitmap);
    }

    public static BitmapBuffer Brightness(BitmapBuffer source, int brightness)
    {
        using var bitmap = BitmapFilterSupport.CreateBitmap(source);
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

        try
        {
            var scan0 = bitmapData.Scan0;
            var stride = bitmapData.Stride;

            unsafe
            {
                byte* p = (byte*)(void*)scan0;
                var noffset = stride - (bitmap.Width * 3);
                var nWidth = bitmap.Width * 3;

                for (var y = 0; y < bitmap.Height; ++y)
                {
                    for (var x = 0; x < nWidth; ++x)
                    {
                        var value = p[0] + brightness;
                        p[0] = (byte)Math.Clamp(value, 0, 255);
                        ++p;
                    }

                    p += noffset;
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return BitmapFilterSupport.ToBuffer(bitmap);
    }

    public static BitmapBuffer Contrast(BitmapBuffer source, double factor)
    {
        using var bitmap = BitmapFilterSupport.CreateBitmap(source);
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

        try
        {
            var scan0 = bitmapData.Scan0;
            var stride = bitmapData.Stride;

            unsafe
            {
                byte* p = (byte*)(void*)scan0;
                var noffset = stride - (bitmap.Width * 3);
                var nWidth = bitmap.Width * 3;

                for (var y = 0; y < bitmap.Height; ++y)
                {
                    for (var x = 0; x < nWidth; ++x)
                    {
                        var pixel = p[0] / 255.0;
                        pixel -= 0.5;
                        pixel *= factor;
                        pixel += 0.5;
                        pixel *= 255.0;

                        p[0] = (byte)Math.Clamp((int)Math.Round(pixel), 0, 255);
                        ++p;
                    }

                    p += noffset;
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return BitmapFilterSupport.ToBuffer(bitmap);
    }

    public static BitmapBuffer Gamma(BitmapBuffer source, double gamma)
    {
        using var bitmap = BitmapFilterSupport.CreateBitmap(source);
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
        var gammaArray = new byte[256];

        for (var i = 0; i < gammaArray.Length; ++i)
        {
            gammaArray[i] = (byte)Math.Min(255, (int)((255.0 * Math.Pow(i / 255.0, 1.0 / gamma)) + 0.5));
        }

        try
        {
            var scan0 = bitmapData.Scan0;
            var stride = bitmapData.Stride;

            unsafe
            {
                byte* p = (byte*)(void*)scan0;
                var noffset = stride - (bitmap.Width * 3);
                var nWidth = bitmap.Width * 3;

                for (var y = 0; y < bitmap.Height; ++y)
                {
                    for (var x = 0; x < nWidth; ++x)
                    {
                        p[0] = gammaArray[p[0]];
                        ++p;
                    }

                    p += noffset;
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return BitmapFilterSupport.ToBuffer(bitmap);
    }
}
