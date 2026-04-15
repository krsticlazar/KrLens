using KrLensServer.Core.Exceptions;

namespace KrLensServer.Core.Msi;

internal static class MsiColorTransform
{
    public static byte[] FromRgb(byte[] rgbPixels, byte channels, MsiColorSpace colorSpace)
    {
        ArgumentNullException.ThrowIfNull(rgbPixels);

        if (channels == 1 || colorSpace is MsiColorSpace.Linear or MsiColorSpace.Rgb)
        {
            return rgbPixels.ToArray();
        }

        return colorSpace switch
        {
            MsiColorSpace.Hsv => ConvertRgbToHsv(rgbPixels),
            MsiColorSpace.YCbCr => ConvertRgbToYCbCr(rgbPixels),
            _ => throw new UnsupportedImageFormatException($"Unsupported MSI colorspace '{colorSpace}'."),
        };
    }

    public static byte[] ToRgb(byte[] storedPixels, byte channels, MsiColorSpace colorSpace)
    {
        ArgumentNullException.ThrowIfNull(storedPixels);

        if (channels == 1 || colorSpace is MsiColorSpace.Linear or MsiColorSpace.Rgb)
        {
            return storedPixels.ToArray();
        }

        return colorSpace switch
        {
            MsiColorSpace.Hsv => ConvertHsvToRgb(storedPixels),
            MsiColorSpace.YCbCr => ConvertYCbCrToRgb(storedPixels),
            _ => throw new MsiCorruptedException($"Unsupported MSI colorspace '{colorSpace}'."),
        };
    }

    private static byte[] ConvertRgbToHsv(byte[] rgbPixels)
    {
        var hsvPixels = new byte[rgbPixels.Length];
        for (var i = 0; i < rgbPixels.Length; i += 3)
        {
            var r = rgbPixels[i] / 255d;
            var g = rgbPixels[i + 1] / 255d;
            var b = rgbPixels[i + 2] / 255d;
            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));
            var delta = max - min;

            double hue;
            if (delta == 0)
            {
                hue = 0;
            }
            else if (max == r)
            {
                hue = 60d * (((g - b) / delta) % 6d);
            }
            else if (max == g)
            {
                hue = 60d * (((b - r) / delta) + 2d);
            }
            else
            {
                hue = 60d * (((r - g) / delta) + 4d);
            }

            if (hue < 0)
            {
                hue += 360d;
            }

            var saturation = max == 0 ? 0d : delta / max;

            hsvPixels[i] = (byte)Math.Round((hue / 360d) * 255d);
            hsvPixels[i + 1] = (byte)Math.Round(saturation * 255d);
            hsvPixels[i + 2] = (byte)Math.Round(max * 255d);
        }

        return hsvPixels;
    }

    private static byte[] ConvertHsvToRgb(byte[] hsvPixels)
    {
        var rgbPixels = new byte[hsvPixels.Length];
        for (var i = 0; i < hsvPixels.Length; i += 3)
        {
            var h = (hsvPixels[i] / 255d) * 360d;
            var s = hsvPixels[i + 1] / 255d;
            var v = hsvPixels[i + 2] / 255d;
            var c = v * s;
            var x = c * (1d - Math.Abs(((h / 60d) % 2d) - 1d));
            var m = v - c;

            (double rPrime, double gPrime, double bPrime) = h switch
            {
                < 60d => (c, x, 0d),
                < 120d => (x, c, 0d),
                < 180d => (0d, c, x),
                < 240d => (0d, x, c),
                < 300d => (x, 0d, c),
                _ => (c, 0d, x),
            };

            rgbPixels[i] = ClampToByte((rPrime + m) * 255d);
            rgbPixels[i + 1] = ClampToByte((gPrime + m) * 255d);
            rgbPixels[i + 2] = ClampToByte((bPrime + m) * 255d);
        }

        return rgbPixels;
    }

    private static byte[] ConvertRgbToYCbCr(byte[] rgbPixels)
    {
        var output = new byte[rgbPixels.Length];
        for (var i = 0; i < rgbPixels.Length; i += 3)
        {
            var r = rgbPixels[i];
            var g = rgbPixels[i + 1];
            var b = rgbPixels[i + 2];

            var y = 0.299d * r + 0.587d * g + 0.114d * b;
            var cb = 128d - (0.168736d * r) - (0.331264d * g) + (0.5d * b);
            var cr = 128d + (0.5d * r) - (0.418688d * g) - (0.081312d * b);

            output[i] = ClampToByte(y);
            output[i + 1] = ClampToByte(cb);
            output[i + 2] = ClampToByte(cr);
        }

        return output;
    }

    private static byte[] ConvertYCbCrToRgb(byte[] input)
    {
        var output = new byte[input.Length];
        for (var i = 0; i < input.Length; i += 3)
        {
            var y = input[i];
            var cb = input[i + 1] - 128d;
            var cr = input[i + 2] - 128d;

            var r = y + (1.402d * cr);
            var g = y - (0.344136d * cb) - (0.714136d * cr);
            var b = y + (1.772d * cb);

            output[i] = ClampToByte(r);
            output[i + 1] = ClampToByte(g);
            output[i + 2] = ClampToByte(b);
        }

        return output;
    }

    private static byte ClampToByte(double value) => (byte)Math.Clamp((int)Math.Round(value), 0, 255);
}
