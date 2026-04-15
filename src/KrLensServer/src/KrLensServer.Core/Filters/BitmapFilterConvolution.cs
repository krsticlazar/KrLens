using System.Drawing;
using System.Drawing.Imaging;
using KrLensServer.Core.Models;

namespace KrLensServer.Core.Filters;

internal static class BitmapFilterConvolution
{
    internal sealed class ConvMatrix
    {
        public int TopLeft;
        public int TopMid;
        public int TopRight;
        public int MidLeft;
        public int Pixel = 1;
        public int MidRight;
        public int BottomLeft;
        public int BottomMid;
        public int BottomRight;
        public int Factor = 1;
        public int Offset = 0;

        public void SetAll(int value)
        {
            TopLeft = TopMid = TopRight = MidLeft = Pixel = MidRight = BottomLeft = BottomMid = BottomRight = value;
        }
    }

    public static BitmapBuffer Smooth(BitmapBuffer source, int radius)
    {
        var current = source;
        for (var i = 0; i < radius; i++)
        {
            var matrix = new ConvMatrix();
            matrix.SetAll(1);
            matrix.Pixel = 1;
            matrix.Factor = 9;
            current = Conv3x3(current, matrix);
        }

        return current;
    }

    public static BitmapBuffer EdgeDetectHorizontal(BitmapBuffer source)
    {
        var horizontal = new ConvMatrix
        {
            TopLeft = -1,
            TopMid = -2,
            TopRight = -1,
            BottomLeft = 1,
            BottomMid = 2,
            BottomRight = 1,
            Pixel = 0,
            Factor = 1,
        };

        return Conv3x3Absolute(source, horizontal);
    }

    public static BitmapBuffer EdgeDetectVertical(BitmapBuffer source)
    {
        var vertical = new ConvMatrix
        {
            TopLeft = -1,
            TopRight = 1,
            MidLeft = -2,
            MidRight = 2,
            BottomLeft = -1,
            BottomRight = 1,
            Pixel = 0,
            Factor = 1,
        };

        return Conv3x3Absolute(source, vertical);
    }

    public static BitmapBuffer EdgeDetectHV(BitmapBuffer source, int direction)
    {
        return direction switch
        {
            0 => EdgeDetectHorizontal(source),
            1 => EdgeDetectVertical(source),
            _ => CombineEdges(EdgeDetectHorizontal(source), EdgeDetectVertical(source)),
        };
    }

    private static BitmapBuffer Conv3x3(BitmapBuffer source, ConvMatrix matrix)
    {
        return ApplyMatrix(source, matrix, absolute: false);
    }

    private static BitmapBuffer Conv3x3Absolute(BitmapBuffer source, ConvMatrix matrix)
    {
        return ApplyMatrix(source, matrix, absolute: true);
    }

    private static BitmapBuffer ApplyMatrix(BitmapBuffer source, ConvMatrix matrix, bool absolute)
    {
        using var sourceBitmap = BitmapFilterSupport.CreateBitmap(source);
        using var destinationBitmap = BitmapFilterSupport.CreateEmpty(sourceBitmap);
        var rect = new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height);
        var sourceData = sourceBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        var destinationData = destinationBitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

        try
        {
            if (matrix.Factor == 0)
            {
                matrix.Factor = 1;
            }

            unsafe
            {
                byte* pSrcBase = (byte*)(void*)sourceData.Scan0;
                byte* pDst = (byte*)(void*)destinationData.Scan0;
                var strideSrc = sourceData.Stride;
                var strideDst = destinationData.Stride;
                var noffset = strideDst - (destinationBitmap.Width * 3);

                for (var y = 0; y < destinationBitmap.Height; ++y)
                {
                    for (var x = 0; x < destinationBitmap.Width; ++x)
                    {
                        for (var channel = 0; channel < 3; ++channel)
                        {
                            var pixel =
                                GetPixel(pSrcBase, strideSrc, sourceBitmap.Width, sourceBitmap.Height, x - 1, y - 1, channel) * matrix.TopLeft +
                                GetPixel(pSrcBase, strideSrc, sourceBitmap.Width, sourceBitmap.Height, x, y - 1, channel) * matrix.TopMid +
                                GetPixel(pSrcBase, strideSrc, sourceBitmap.Width, sourceBitmap.Height, x + 1, y - 1, channel) * matrix.TopRight +
                                GetPixel(pSrcBase, strideSrc, sourceBitmap.Width, sourceBitmap.Height, x - 1, y, channel) * matrix.MidLeft +
                                GetPixel(pSrcBase, strideSrc, sourceBitmap.Width, sourceBitmap.Height, x, y, channel) * matrix.Pixel +
                                GetPixel(pSrcBase, strideSrc, sourceBitmap.Width, sourceBitmap.Height, x + 1, y, channel) * matrix.MidRight +
                                GetPixel(pSrcBase, strideSrc, sourceBitmap.Width, sourceBitmap.Height, x - 1, y + 1, channel) * matrix.BottomLeft +
                                GetPixel(pSrcBase, strideSrc, sourceBitmap.Width, sourceBitmap.Height, x, y + 1, channel) * matrix.BottomMid +
                                GetPixel(pSrcBase, strideSrc, sourceBitmap.Width, sourceBitmap.Height, x + 1, y + 1, channel) * matrix.BottomRight;

                            pixel = absolute
                                ? Math.Abs(pixel / matrix.Factor) + matrix.Offset
                                : (pixel / matrix.Factor) + matrix.Offset;

                            pDst[channel] = (byte)Math.Clamp(pixel, 0, 255);
                        }

                        pDst += 3;
                    }

                    pDst += noffset;
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

    private static BitmapBuffer CombineEdges(BitmapBuffer horizontal, BitmapBuffer vertical)
    {
        var destination = new BitmapBuffer(horizontal.Width, horizontal.Height, 3);

        for (var i = 0; i < destination.Pixels.Length; ++i)
        {
            var pixel = Math.Sqrt((horizontal.Pixels[i] * horizontal.Pixels[i]) + (vertical.Pixels[i] * vertical.Pixels[i]));
            destination.Pixels[i] = (byte)Math.Clamp((int)Math.Round(pixel), 0, 255);
        }

        return destination;
    }

    private static unsafe byte GetPixel(byte* scan0, int stride, int width, int height, int x, int y, int channel)
    {
        return BitmapFilterSupport.GetChannel(scan0, stride, width, height, x, y, channel);
    }
}
