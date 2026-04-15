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
        public int Offset;

        public void SetAll(int nVal)
        {
            TopLeft = TopMid = TopRight = MidLeft = Pixel = MidRight = BottomLeft = BottomMid = BottomRight = nVal;
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
            Offset = 0,
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
            Offset = 0,
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

    private static unsafe BitmapBuffer Conv3x3(BitmapBuffer source, ConvMatrix matrix)
    {
        var destination = new BitmapBuffer(source.Width, source.Height, source.Channels);
        var width = source.Width;
        var height = source.Height;
        var channels = source.Channels;

        if (matrix.Factor == 0)
        {
            matrix.Factor = 1;
        }

        fixed (byte* pSrcBase = source.Pixels)
        fixed (byte* pDstBase = destination.Pixels)
        {
            byte* pSrc = pSrcBase;
            byte* pDst = pDstBase;

            for (var y = 0; y < height; ++y)
            {
                for (var x = 0; x < width; ++x)
                {
                    for (var channel = 0; channel < channels; ++channel)
                    {
                        var nPixel =
                            GetPixel(source, pSrcBase, x - 1, y - 1, channel) * matrix.TopLeft +
                            GetPixel(source, pSrcBase, x, y - 1, channel) * matrix.TopMid +
                            GetPixel(source, pSrcBase, x + 1, y - 1, channel) * matrix.TopRight +
                            GetPixel(source, pSrcBase, x - 1, y, channel) * matrix.MidLeft +
                            GetPixel(source, pSrcBase, x, y, channel) * matrix.Pixel +
                            GetPixel(source, pSrcBase, x + 1, y, channel) * matrix.MidRight +
                            GetPixel(source, pSrcBase, x - 1, y + 1, channel) * matrix.BottomLeft +
                            GetPixel(source, pSrcBase, x, y + 1, channel) * matrix.BottomMid +
                            GetPixel(source, pSrcBase, x + 1, y + 1, channel) * matrix.BottomRight;

                        nPixel = (nPixel / matrix.Factor) + matrix.Offset;

                        if (nPixel < 0) nPixel = 0;
                        if (nPixel > 255) nPixel = 255;

                        pDst[((y * width) + x) * channels + channel] = (byte)nPixel;
                    }
                }
            }
        }

        return destination;
    }

    private static unsafe BitmapBuffer Conv3x3Absolute(BitmapBuffer source, ConvMatrix matrix)
    {
        var destination = new BitmapBuffer(source.Width, source.Height, source.Channels);
        var width = source.Width;
        var height = source.Height;
        var channels = source.Channels;

        if (matrix.Factor == 0)
        {
            matrix.Factor = 1;
        }

        fixed (byte* pSrcBase = source.Pixels)
        fixed (byte* pDstBase = destination.Pixels)
        {
            for (var y = 0; y < height; ++y)
            {
                for (var x = 0; x < width; ++x)
                {
                    for (var channel = 0; channel < channels; ++channel)
                    {
                        var nPixel =
                            GetPixel(source, pSrcBase, x - 1, y - 1, channel) * matrix.TopLeft +
                            GetPixel(source, pSrcBase, x, y - 1, channel) * matrix.TopMid +
                            GetPixel(source, pSrcBase, x + 1, y - 1, channel) * matrix.TopRight +
                            GetPixel(source, pSrcBase, x - 1, y, channel) * matrix.MidLeft +
                            GetPixel(source, pSrcBase, x, y, channel) * matrix.Pixel +
                            GetPixel(source, pSrcBase, x + 1, y, channel) * matrix.MidRight +
                            GetPixel(source, pSrcBase, x - 1, y + 1, channel) * matrix.BottomLeft +
                            GetPixel(source, pSrcBase, x, y + 1, channel) * matrix.BottomMid +
                            GetPixel(source, pSrcBase, x + 1, y + 1, channel) * matrix.BottomRight;

                        nPixel = Math.Abs(nPixel / matrix.Factor) + matrix.Offset;

                        if (nPixel < 0) nPixel = 0;
                        if (nPixel > 255) nPixel = 255;

                        pDstBase[((y * width) + x) * channels + channel] = (byte)nPixel;
                    }
                }
            }
        }

        return destination;
    }

    private static BitmapBuffer CombineEdges(BitmapBuffer horizontal, BitmapBuffer vertical)
    {
        var destination = new BitmapBuffer(horizontal.Width, horizontal.Height, horizontal.Channels);
        for (var i = 0; i < destination.Pixels.Length; i++)
        {
            var nPixel = Math.Sqrt((horizontal.Pixels[i] * horizontal.Pixels[i]) + (vertical.Pixels[i] * vertical.Pixels[i]));
            if (nPixel > 255) nPixel = 255;
            destination.Pixels[i] = (byte)nPixel;
        }

        return destination;
    }

    private static unsafe byte GetPixel(BitmapBuffer source, byte* pSrcBase, int x, int y, int channel)
    {
        if (x < 0) x = 0;
        if (y < 0) y = 0;
        if (x >= source.Width) x = source.Width - 1;
        if (y >= source.Height) y = source.Height - 1;

        return pSrcBase[((y * source.Width) + x) * source.Channels + channel];
    }
}
