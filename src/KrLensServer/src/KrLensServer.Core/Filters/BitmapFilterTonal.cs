using KrLensServer.Core.Models;

namespace KrLensServer.Core.Filters;

internal static class BitmapFilterTonal
{
    public static BitmapBuffer Stucki(BitmapBuffer source)
    {
        var grayscale = source.Channels == 1 ? source.Clone() : BitmapFilterBasic.GrayScale(source);
        var output = new BitmapBuffer(grayscale.Width, grayscale.Height, 1);
        var work = new double[grayscale.Pixels.Length];

        for (var i = 0; i < grayscale.Pixels.Length; i++)
        {
            work[i] = grayscale.Pixels[i];
        }

        var weights = new (int X, int Y, int Weight)[]
        {
            (1, 0, 8), (2, 0, 4),
            (-2, 1, 2), (-1, 1, 4), (0, 1, 8), (1, 1, 4), (2, 1, 2),
            (-2, 2, 1), (-1, 2, 2), (0, 2, 4), (1, 2, 2), (2, 2, 1),
        };

        for (var y = 0; y < grayscale.Height; y++)
        {
            for (var x = 0; x < grayscale.Width; x++)
            {
                var index = (y * grayscale.Width) + x;
                var oldPixel = work[index];
                var newPixel = oldPixel >= 128.0 ? 255.0 : 0.0;
                var error = oldPixel - newPixel;

                output.Pixels[index] = (byte)newPixel;

                foreach (var (xOffset, yOffset, weight) in weights)
                {
                    var sx = x + xOffset;
                    var sy = y + yOffset;

                    if (sx < 0 || sx >= grayscale.Width || sy < 0 || sy >= grayscale.Height)
                    {
                        continue;
                    }

                    var targetIndex = (sy * grayscale.Width) + sx;
                    work[targetIndex] += (error * weight) / 42.0;

                    if (work[targetIndex] < 0) work[targetIndex] = 0;
                    if (work[targetIndex] > 255) work[targetIndex] = 255;
                }
            }
        }

        return output;
    }

    public static BitmapBuffer HistogramEqualizing(BitmapBuffer source)
    {
        var destination = new BitmapBuffer(source.Width, source.Height, source.Channels);
        var histograms = new int[source.Channels][];
        var maps = new byte[source.Channels][];

        for (var channel = 0; channel < source.Channels; channel++)
        {
            histograms[channel] = new int[256];
        }

        for (var i = 0; i < source.Pixels.Length; i++)
        {
            histograms[i % source.Channels][source.Pixels[i]]++;
        }

        for (var channel = 0; channel < source.Channels; channel++)
        {
            maps[channel] = BuildEqualizeMap(histograms[channel], source.PixelCount);
        }

        for (var i = 0; i < source.Pixels.Length; i++)
        {
            var channel = i % source.Channels;
            destination.Pixels[i] = maps[channel][source.Pixels[i]];
        }

        return destination;
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
