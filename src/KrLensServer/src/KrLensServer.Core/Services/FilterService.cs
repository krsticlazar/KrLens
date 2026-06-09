using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using KrLensServer.Core.Exceptions;
using KrLensServer.Core.Models;

namespace KrLensServer.Core.Services;

public sealed class FilterService
{
    private static readonly IReadOnlyDictionary<string, double> EmptyParameters = new Dictionary<string, double>();

    public BitmapBuffer Apply(BitmapBuffer source, string filterName, IReadOnlyDictionary<string, double>? parameters)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(filterName);

        var values = parameters ?? EmptyParameters;

        if (filterName.Equals("Grayscale", StringComparison.OrdinalIgnoreCase))
        {
            return Grayscale(source);
        }

        if (filterName.Equals("Invert", StringComparison.OrdinalIgnoreCase))
        {
            return Invert(source);
        }

        if (filterName.Equals("Brightness", StringComparison.OrdinalIgnoreCase))
        {
            var delta = GetRequiredInt(values, "delta", -255, 255);
            return Brightness(source, delta);
        }

        if (filterName.Equals("Contrast", StringComparison.OrdinalIgnoreCase))
        {
            var factor = GetRequiredDouble(values, "factor", 0d, 3d);
            return Contrast(source, factor);
        }

        if (filterName.Equals("Gamma", StringComparison.OrdinalIgnoreCase))
        {
            var gamma = GetRequiredDouble(values, "gamma", 0.1d, 5d);
            return Gamma(source, gamma);
        }

        if (filterName.Equals("Smooth", StringComparison.OrdinalIgnoreCase))
        {
            var radius = GetRequiredInt(values, "radius", 1, 5);
            return Smooth(source, radius);
        }

        if (filterName.Equals("EdgeDetectHV", StringComparison.OrdinalIgnoreCase))
        {
            var direction = GetRequiredInt(values, "direction", 0, 2);
            return EdgeDetect(source, direction);
        }

        if (filterName.Equals("Flip", StringComparison.OrdinalIgnoreCase))
        {
            var axis = GetRequiredInt(values, "axis", 0, 1);
            return Flip(source, horizontal: axis == 0, vertical: axis == 1);
        }

        if (filterName.Equals("Water", StringComparison.OrdinalIgnoreCase))
        {
            var amplitude = GetRequiredDouble(values, "amplitude", 0d, 100d);
            var wavelength = GetRequiredDouble(values, "wavelength", 1d, 1000d);
            return Water(source, amplitude, wavelength);
        }

        if (filterName.Equals("Stucki", StringComparison.OrdinalIgnoreCase))
        {
            return Stucki(source);
        }

        if (filterName.Equals("HistogramEqualizing", StringComparison.OrdinalIgnoreCase))
        {
            return HistogramEqualizing(source);
        }

        throw new FilterParameterException($"Filter '{filterName}' is not registered.");
    }

    public BitmapBuffer ApplyBatch(BitmapBuffer source, IEnumerable<FilterRequest> filters)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(filters);

        var current = source;
        foreach (var request in filters)
        {
            ArgumentNullException.ThrowIfNull(request);
            current = Apply(current, request.Filter, request.Parameters);
        }

        return current;
    }

    private static BitmapBuffer Invert(BitmapBuffer source)
    {
        var result = new BitmapBuffer(source.Width, source.Height, source.Channels);

        Parallel.For(0, source.Height, y =>
        {
            var rowStart = y * source.Stride;
            var rowEnd = rowStart + source.Stride;

            for (var index = rowStart; index < rowEnd; ++index)
            {
                result.Pixels[index] = (byte)(255 - source.Pixels[index]);
            }
        });

        return result;
    }

    private static BitmapBuffer Grayscale(BitmapBuffer source)
    {
        if (source.Channels == 1)
        {
            return source.Clone();
        }

        var result = new BitmapBuffer(source.Width, source.Height, 3);

        Parallel.For(0, source.Height, y =>
        {
            var rowStart = y * source.Stride;
            var rowEnd = rowStart + source.Stride;

            for (var index = rowStart; index < rowEnd; index += 3)
            {
                var gray = ClampToByte((0.299 * source.Pixels[index]) + (0.587 * source.Pixels[index + 1]) + (0.114 * source.Pixels[index + 2]));
                result.Pixels[index] = gray;
                result.Pixels[index + 1] = gray;
                result.Pixels[index + 2] = gray;
            }
        });

        return result;
    }

    private static BitmapBuffer Brightness(BitmapBuffer source, int delta)
    {
        var result = new BitmapBuffer(source.Width, source.Height, source.Channels);

        Parallel.For(0, source.Height, y =>
        {
            var rowStart = y * source.Stride;
            var rowEnd = rowStart + source.Stride;

            for (var index = rowStart; index < rowEnd; ++index)
            {
                result.Pixels[index] = ClampToByte(source.Pixels[index] + delta);
            }
        });

        return result;
    }

    private static BitmapBuffer Contrast(BitmapBuffer source, double factor)
    {
        var result = new BitmapBuffer(source.Width, source.Height, source.Channels);

        Parallel.For(0, source.Height, y =>
        {
            var rowStart = y * source.Stride;
            var rowEnd = rowStart + source.Stride;

            for (var index = rowStart; index < rowEnd; ++index)
            {
                var value = ((((source.Pixels[index] / 255.0) - 0.5) * factor) + 0.5) * 255.0;
                result.Pixels[index] = ClampToByte(value);
            }
        });

        return result;
    }

    private static BitmapBuffer Gamma(BitmapBuffer source, double gamma)
    {
        var result = new BitmapBuffer(source.Width, source.Height, source.Channels);
        var gammaMap = BuildGammaMap(gamma);

        Parallel.For(0, source.Height, y =>
        {
            var rowStart = y * source.Stride;
            var rowEnd = rowStart + source.Stride;

            for (var index = rowStart; index < rowEnd; ++index)
            {
                result.Pixels[index] = gammaMap[source.Pixels[index]];
            }
        });

        return result;
    }

    private static BitmapBuffer Smooth(BitmapBuffer source, int radius)
    {
        var current = source;

        for (var index = 0; index < radius; ++index)
        {
            var matrix = new ConvolutionMatrix();
            matrix.SetAll(1);
            matrix.Factor = 9;
            current = ApplyMatrix(current, matrix, absolute: false);
        }

        return current;
    }

    private static BitmapBuffer EdgeDetect(BitmapBuffer source, int direction)
    {
        return direction switch
        {
            0 => ApplyMatrix(source, ConvolutionMatrix.HorizontalEdge(), absolute: true),
            1 => ApplyMatrix(source, ConvolutionMatrix.VerticalEdge(), absolute: true),
            _ => CombineEdges(
                ApplyMatrix(source, ConvolutionMatrix.HorizontalEdge(), absolute: true),
                ApplyMatrix(source, ConvolutionMatrix.VerticalEdge(), absolute: true)),
        };
    }

    private static BitmapBuffer ApplyMatrix(BitmapBuffer source, ConvolutionMatrix matrix, bool absolute)
    {
        using var sourceBitmap = BitmapBufferCodec.CreateBitmap(source);
        using var destinationBitmap = BitmapBufferCodec.CreateEmpty(sourceBitmap);
        var rect = new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height);
        var sourceData = sourceBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        var destinationData = destinationBitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

        try
        {
            var factor = matrix.Factor == 0 ? 1 : matrix.Factor;

            unsafe
            {
                byte* sourcePointer = (byte*)(void*)sourceData.Scan0;
                byte* destinationPointer = (byte*)(void*)destinationData.Scan0;
                var width = destinationBitmap.Width;
                var height = destinationBitmap.Height;
                var sourceStride = sourceData.Stride;
                var destinationStride = destinationData.Stride;

                Parallel.For(0, height, y =>
                {
                    byte* destinationRow = destinationPointer + (y * destinationStride);

                    for (var x = 0; x < width; ++x)
                    {
                        for (var channel = 0; channel < 3; ++channel)
                        {
                            var pixel =
                                (BitmapBufferCodec.GetChannel(sourcePointer, sourceStride, width, height, x - 1, y - 1, channel) * matrix.TopLeft) +
                                (BitmapBufferCodec.GetChannel(sourcePointer, sourceStride, width, height, x, y - 1, channel) * matrix.TopMid) +
                                (BitmapBufferCodec.GetChannel(sourcePointer, sourceStride, width, height, x + 1, y - 1, channel) * matrix.TopRight) +
                                (BitmapBufferCodec.GetChannel(sourcePointer, sourceStride, width, height, x - 1, y, channel) * matrix.MidLeft) +
                                (BitmapBufferCodec.GetChannel(sourcePointer, sourceStride, width, height, x, y, channel) * matrix.Center) +
                                (BitmapBufferCodec.GetChannel(sourcePointer, sourceStride, width, height, x + 1, y, channel) * matrix.MidRight) +
                                (BitmapBufferCodec.GetChannel(sourcePointer, sourceStride, width, height, x - 1, y + 1, channel) * matrix.BottomLeft) +
                                (BitmapBufferCodec.GetChannel(sourcePointer, sourceStride, width, height, x, y + 1, channel) * matrix.BottomMid) +
                                (BitmapBufferCodec.GetChannel(sourcePointer, sourceStride, width, height, x + 1, y + 1, channel) * matrix.BottomRight);

                            pixel = absolute ? Math.Abs(pixel / factor) + matrix.Offset : (pixel / factor) + matrix.Offset;
                            destinationRow[(x * 3) + channel] = ClampToByte(pixel);
                        }
                    }
                });
            }
        }
        finally
        {
            sourceBitmap.UnlockBits(sourceData);
            destinationBitmap.UnlockBits(destinationData);
        }

        return BitmapBufferCodec.ToBuffer(destinationBitmap);
    }

    private static BitmapBuffer CombineEdges(BitmapBuffer horizontal, BitmapBuffer vertical)
    {
        var destination = new BitmapBuffer(horizontal.Width, horizontal.Height, 3);

        Parallel.For(0, destination.Pixels.Length, index =>
        {
            var value = Math.Sqrt((horizontal.Pixels[index] * horizontal.Pixels[index]) + (vertical.Pixels[index] * vertical.Pixels[index]));
            destination.Pixels[index] = ClampToByte(value);
        });

        return destination;
    }

    private static BitmapBuffer Flip(BitmapBuffer source, bool horizontal, bool vertical)
    {
        var map = new OffsetPoint[source.Width, source.Height];

        Parallel.For(0, source.Height, y =>
        {
            for (var x = 0; x < source.Width; ++x)
            {
                map[x, y] = new OffsetPoint(
                    horizontal ? source.Width - x - 1 : x,
                    vertical ? source.Height - y - 1 : y);
            }
        });

        return ApplyOffset(source, map);
    }

    private static BitmapBuffer Water(BitmapBuffer source, double amplitude, double wavelength)
    {
        var map = new OffsetPoint[source.Width, source.Height];

        Parallel.For(0, source.Height, y =>
        {
            for (var x = 0; x < source.Width; ++x)
            {
                var mappedX = x + (amplitude * Math.Sin((2.0 * Math.PI * y) / wavelength));
                var mappedY = y + (amplitude * Math.Cos((2.0 * Math.PI * x) / wavelength));

                var safeX = mappedX > 0 && mappedX < source.Width ? (int)mappedX : x;
                var safeY = mappedY > 0 && mappedY < source.Height ? (int)mappedY : y;
                map[x, y] = new OffsetPoint(safeX, safeY);
            }
        });

        return ApplyOffset(source, map);
    }

    private static BitmapBuffer ApplyOffset(BitmapBuffer source, OffsetPoint[,] offsetMap)
    {
        using var sourceBitmap = BitmapBufferCodec.CreateBitmap(source);
        using var destinationBitmap = BitmapBufferCodec.CreateEmpty(sourceBitmap);
        var rect = new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height);
        var sourceData = sourceBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        var destinationData = destinationBitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

        try
        {
            unsafe
            {
                byte* sourcePointer = (byte*)(void*)sourceData.Scan0;
                byte* destinationPointer = (byte*)(void*)destinationData.Scan0;
                var width = destinationBitmap.Width;
                var height = destinationBitmap.Height;
                var sourceStride = sourceData.Stride;
                var destinationStride = destinationData.Stride;

                Parallel.For(0, height, y =>
                {
                    byte* destinationRow = destinationPointer + (y * destinationStride);

                    for (var x = 0; x < width; ++x)
                    {
                        var offset = offsetMap[x, y];
                        var sourceIndex = (offset.Y * sourceStride) + (offset.X * 3);
                        var destinationIndex = x * 3;

                        destinationRow[destinationIndex] = sourcePointer[sourceIndex];
                        destinationRow[destinationIndex + 1] = sourcePointer[sourceIndex + 1];
                        destinationRow[destinationIndex + 2] = sourcePointer[sourceIndex + 2];
                    }
                });
            }
        }
        finally
        {
            sourceBitmap.UnlockBits(sourceData);
            destinationBitmap.UnlockBits(destinationData);
        }

        return BitmapBufferCodec.ToBuffer(destinationBitmap);
    }

    private static BitmapBuffer Stucki(BitmapBuffer source)
    {
        using var sourceBitmap = BitmapBufferCodec.CreateBitmap(source);
        using var destinationBitmap = BitmapBufferCodec.CreateEmpty(sourceBitmap);
        var rect = new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height);
        var sourceData = sourceBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        var destinationData = destinationBitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
        var work = new double[sourceBitmap.Width * sourceBitmap.Height];
        var weights = new (int X, int Y, int Weight)[]
        {
            (1, 0, 8), (2, 0, 4),
            (-2, 1, 2), (-1, 1, 4), (0, 1, 8), (1, 1, 4), (2, 1, 2),
            (-2, 2, 1), (-1, 2, 2), (0, 2, 4), (1, 2, 2), (2, 2, 1),
        };

        try
        {
            unsafe
            {
                byte* sourcePointer = (byte*)(void*)sourceData.Scan0;
                var sourceStride = sourceData.Stride;
                var width = sourceBitmap.Width;
                var height = sourceBitmap.Height;

                Parallel.For(0, height, y =>
                {
                    byte* sourceRow = sourcePointer + (y * sourceStride);
                    var workIndex = y * width;

                    for (var x = 0; x < width; ++x)
                    {
                        work[workIndex + x] = (0.114 * sourceRow[0]) + (0.587 * sourceRow[1]) + (0.299 * sourceRow[2]);
                        sourceRow += 3;
                    }
                });

                byte* destinationPointer = (byte*)(void*)destinationData.Scan0;
                var destinationRowPadding = destinationData.Stride - (destinationBitmap.Width * 3);

                for (var y = 0; y < destinationBitmap.Height; ++y)
                {
                    for (var x = 0; x < destinationBitmap.Width; ++x)
                    {
                        var pixelIndex = (y * destinationBitmap.Width) + x;
                        var oldPixel = work[pixelIndex];
                        var newPixel = oldPixel >= 128d ? (byte)255 : (byte)0;
                        var error = oldPixel - newPixel;

                        destinationPointer[0] = newPixel;
                        destinationPointer[1] = newPixel;
                        destinationPointer[2] = newPixel;

                        foreach (var (offsetX, offsetY, weight) in weights)
                        {
                            var targetX = x + offsetX;
                            var targetY = y + offsetY;

                            if (targetX < 0 || targetX >= destinationBitmap.Width || targetY < 0 || targetY >= destinationBitmap.Height)
                            {
                                continue;
                            }

                            var targetIndex = (targetY * destinationBitmap.Width) + targetX;
                            work[targetIndex] = Math.Clamp(work[targetIndex] + ((error * weight) / 42.0), 0.0, 255.0);
                        }

                        destinationPointer += 3;
                    }

                    destinationPointer += destinationRowPadding;
                }
            }
        }
        finally
        {
            sourceBitmap.UnlockBits(sourceData);
            destinationBitmap.UnlockBits(destinationData);
        }

        return BitmapBufferCodec.ToBuffer(destinationBitmap);
    }

    private static BitmapBuffer HistogramEqualizing(BitmapBuffer source)
    {
        using var bitmap = BitmapBufferCodec.CreateBitmap(source);
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
        var histograms = new[] { new int[256], new int[256], new int[256] };
        var maps = new[] { new byte[256], new byte[256], new byte[256] };
        var totalPixels = bitmap.Width * bitmap.Height;

        try
        {
            unsafe
            {
                byte* basePointer = (byte*)(void*)bitmapData.Scan0;
                var width = bitmap.Width;
                var height = bitmap.Height;
                var stride = bitmapData.Stride;
                var histogramLock = new object();

                Parallel.For(
                    0,
                    height,
                    () => new[] { new int[256], new int[256], new int[256] },
                    (y, _, localHistograms) =>
                    {
                        byte* current = basePointer + (y * stride);

                        for (var x = 0; x < width; ++x)
                        {
                            localHistograms[0][current[0]]++;
                            localHistograms[1][current[1]]++;
                            localHistograms[2][current[2]]++;
                            current += 3;
                        }

                        return localHistograms;
                    },
                    localHistograms =>
                    {
                        lock (histogramLock)
                        {
                            for (var channel = 0; channel < 3; ++channel)
                            {
                                for (var value = 0; value < 256; ++value)
                                {
                                    histograms[channel][value] += localHistograms[channel][value];
                                }
                            }
                        }
                    });

                maps[0] = BuildEqualizeMap(histograms[0], totalPixels);
                maps[1] = BuildEqualizeMap(histograms[1], totalPixels);
                maps[2] = BuildEqualizeMap(histograms[2], totalPixels);

                Parallel.For(0, height, y =>
                {
                    byte* current = basePointer + (y * stride);

                    for (var x = 0; x < width; ++x)
                    {
                        current[0] = maps[0][current[0]];
                        current[1] = maps[1][current[1]];
                        current[2] = maps[2][current[2]];
                        current += 3;
                    }
                });
            }
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return BitmapBufferCodec.ToBuffer(bitmap);
    }

    private static byte[] BuildEqualizeMap(int[] histogram, int totalPixels)
    {
        var map = new byte[256];
        var cumulative = 0;
        var cumulativeMinimum = 0;

        for (var index = 0; index < histogram.Length; ++index)
        {
            cumulative += histogram[index];

            if (cumulativeMinimum == 0 && histogram[index] > 0)
            {
                cumulativeMinimum = cumulative;
            }

            if (totalPixels == cumulativeMinimum)
            {
                map[index] = (byte)index;
                continue;
            }

            map[index] = ClampToByte(((cumulative - cumulativeMinimum) / (double)(totalPixels - cumulativeMinimum)) * 255.0);
        }

        return map;
    }

    private static byte[] BuildGammaMap(double gamma)
    {
        var map = new byte[256];

        for (var index = 0; index < map.Length; ++index)
        {
            map[index] = ClampToByte(255.0 * Math.Pow(index / 255.0, 1.0 / gamma));
        }

        return map;
    }

    private static double GetRequiredDouble(IReadOnlyDictionary<string, double> parameters, string name, double min, double max)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        if (!parameters.TryGetValue(name, out var value))
        {
            throw new FilterParameterException($"Missing required parameter '{name}'.");
        }

        if (value < min || value > max)
        {
            throw new FilterParameterException($"Parameter '{name}' must be between {min} and {max}.");
        }

        return value;
    }

    private static int GetRequiredInt(IReadOnlyDictionary<string, double> parameters, string name, int min, int max)
    {
        var value = GetRequiredDouble(parameters, name, min, max);

        if (Math.Abs(value - Math.Round(value)) > 0.0001d)
        {
            throw new FilterParameterException($"Parameter '{name}' must be an integer.");
        }

        return (int)Math.Round(value);
    }

    private static byte ClampToByte(double value)
    {
        return (byte)Math.Clamp((int)Math.Round(value), 0, 255);
    }

    private sealed class ConvolutionMatrix
    {
        public int TopLeft;
        public int TopMid;
        public int TopRight;
        public int MidLeft;
        public int Center = 1;
        public int MidRight;
        public int BottomLeft;
        public int BottomMid;
        public int BottomRight;
        public int Factor = 1;
        public int Offset = 0;

        public void SetAll(int value)
        {
            TopLeft = value;
            TopMid = value;
            TopRight = value;
            MidLeft = value;
            Center = value;
            MidRight = value;
            BottomLeft = value;
            BottomMid = value;
            BottomRight = value;
        }

        public static ConvolutionMatrix HorizontalEdge()
        {
            return new ConvolutionMatrix
            {
                TopLeft = -1,
                TopMid = -2,
                TopRight = -1,
                BottomLeft = 1,
                BottomMid = 2,
                BottomRight = 1,
                Center = 0,
            };
        }

        public static ConvolutionMatrix VerticalEdge()
        {
            return new ConvolutionMatrix
            {
                TopLeft = -1,
                TopRight = 1,
                MidLeft = -2,
                MidRight = 2,
                BottomLeft = -1,
                BottomRight = 1,
                Center = 0,
            };
        }
    }

    private readonly record struct OffsetPoint(int X, int Y);
}
