using System.Drawing;
using System.Drawing.Imaging;
using KrLensServer.Core.Models;

namespace KrLensServer.Core.Filters;

internal static class BitmapFilterTonal
{
    public static BitmapBuffer Stucki(BitmapBuffer source)
    {
        using var sourceBitmap = BitmapFilterSupport.CreateBitmap(source);
        using var destinationBitmap = BitmapFilterSupport.CreateEmpty(sourceBitmap);
        var rect = new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height);
        var sourceData = sourceBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        var destinationData = destinationBitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
        var work = new double[sourceBitmap.Width * sourceBitmap.Height];

        try
        {
            unsafe
            {
                byte* pSrc = (byte*)(void*)sourceData.Scan0;
                var noffsetSrc = sourceData.Stride - (sourceBitmap.Width * 3);
                var index = 0;

                for (var y = 0; y < sourceBitmap.Height; ++y)
                {
                    for (var x = 0; x < sourceBitmap.Width; ++x)
                    {
                        work[index++] = (0.114 * pSrc[0]) + (0.587 * pSrc[1]) + (0.299 * pSrc[2]);
                        pSrc += 3;
                    }

                    pSrc += noffsetSrc;
                }

                var weights = new (int X, int Y, int Weight)[]
                {
                    (1, 0, 8), (2, 0, 4),
                    (-2, 1, 2), (-1, 1, 4), (0, 1, 8), (1, 1, 4), (2, 1, 2),
                    (-2, 2, 1), (-1, 2, 2), (0, 2, 4), (1, 2, 2), (2, 2, 1),
                };

                byte* pDst = (byte*)(void*)destinationData.Scan0;
                var noffsetDst = destinationData.Stride - (destinationBitmap.Width * 3);

                for (var y = 0; y < destinationBitmap.Height; ++y)
                {
                    for (var x = 0; x < destinationBitmap.Width; ++x)
                    {
                        var pixelIndex = (y * destinationBitmap.Width) + x;
                        var oldPixel = work[pixelIndex];
                        var newPixel = oldPixel >= 128.0 ? (byte)255 : (byte)0;
                        var error = oldPixel - newPixel;

                        pDst[0] = newPixel;
                        pDst[1] = newPixel;
                        pDst[2] = newPixel;

                        foreach (var (xOffset, yOffset, weight) in weights)
                        {
                            var sx = x + xOffset;
                            var sy = y + yOffset;

                            if (sx < 0 || sx >= destinationBitmap.Width || sy < 0 || sy >= destinationBitmap.Height)
                            {
                                continue;
                            }

                            var targetIndex = (sy * destinationBitmap.Width) + sx;
                            work[targetIndex] += (error * weight) / 42.0;
                            work[targetIndex] = Math.Clamp(work[targetIndex], 0.0, 255.0);
                        }

                        pDst += 3;
                    }

                    pDst += noffsetDst;
                }
            }
        }
        finally
        {
            sourceBitmap.UnlockBits(sourceData);
            destinationBitmap.UnlockBits(destinationData);
        }

        return BitmapFilterSupport.ToBuffer(destinationBitmap);
    }

    public static BitmapBuffer HistogramEqualizing(BitmapBuffer source)
    {
        using var bitmap = BitmapFilterSupport.CreateBitmap(source);
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
        var histograms = new[] { new int[256], new int[256], new int[256] };
        var maps = new[] { new byte[256], new byte[256], new byte[256] };
        var totalPixels = bitmap.Width * bitmap.Height;

        try
        {
            unsafe
            {
                byte* pBase = (byte*)(void*)bitmapData.Scan0;
                byte* p = pBase;
                var noffset = bitmapData.Stride - (bitmap.Width * 3);

                for (var y = 0; y < bitmap.Height; ++y)
                {
                    for (var x = 0; x < bitmap.Width; ++x)
                    {
                        histograms[0][p[0]]++;
                        histograms[1][p[1]]++;
                        histograms[2][p[2]]++;
                        p += 3;
                    }

                    p += noffset;
                }

                maps[0] = BuildEqualizeMap(histograms[0], totalPixels);
                maps[1] = BuildEqualizeMap(histograms[1], totalPixels);
                maps[2] = BuildEqualizeMap(histograms[2], totalPixels);

                p = pBase;

                for (var y = 0; y < bitmap.Height; ++y)
                {
                    for (var x = 0; x < bitmap.Width; ++x)
                    {
                        p[0] = maps[0][p[0]];
                        p[1] = maps[1][p[1]];
                        p[2] = maps[2][p[2]];
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

    private static byte[] BuildEqualizeMap(int[] histogram, int totalPixels)
    {
        var map = new byte[256];
        var cdf = 0;
        var cdfMin = 0;

        for (var i = 0; i < histogram.Length; i++)
        {
            cdf += histogram[i];
            if (cdfMin == 0 && histogram[i] > 0)
            {
                cdfMin = cdf;
            }

            if (totalPixels == cdfMin)
            {
                map[i] = (byte)i;
            }
            else
            {
                map[i] = (byte)Math.Clamp((int)Math.Round(((cdf - cdfMin) / (double)(totalPixels - cdfMin)) * 255.0), 0, 255);
            }
        }

        return map;
    }
}
