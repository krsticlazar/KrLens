using KrLensServer.Core.Models;

namespace KrLensServer.Core.Filters;

internal static class BitmapFilterDisplacement
{
    internal struct FloatPoint
    {
        public double X;
        public double Y;
    }

    internal struct Point
    {
        public int X;
        public int Y;
    }

    public static BitmapBuffer Flip(BitmapBuffer source, bool bHorz, bool bVert)
    {
        var ptFlip = new Point[source.Width, source.Height];
        var nWidth = source.Width;
        var nHeight = source.Height;

        for (var x = 0; x < nWidth; ++x)
        {
            for (var y = 0; y < nHeight; ++y)
            {
                ptFlip[x, y].X = bHorz ? nWidth - (x + 1) : x;
                ptFlip[x, y].Y = bVert ? nHeight - (y + 1) : y;
            }
        }

        return OffsetFilterAbs(source, ptFlip);
    }

    public static BitmapBuffer Water(BitmapBuffer source, double amplitude, double wavelength)
    {
        var nWidth = source.Width;
        var nHeight = source.Height;
        var fp = new FloatPoint[nWidth, nHeight];
        var pt = new Point[nWidth, nHeight];

        double newX;
        double newY;
        double xo;
        double yo;

        for (var x = 0; x < nWidth; ++x)
        {
            for (var y = 0; y < nHeight; ++y)
            {
                xo = amplitude * Math.Sin((2.0 * Math.PI * y) / wavelength);
                yo = amplitude * Math.Cos((2.0 * Math.PI * x) / wavelength);

                newX = x + xo;
                newY = y + yo;

                if (newX > 0 && newX < nWidth)
                {
                    fp[x, y].X = newX;
                    pt[x, y].X = (int)newX;
                }
                else
                {
                    fp[x, y].X = x;
                    pt[x, y].X = x;
                }

                if (newY > 0 && newY < nHeight)
                {
                    fp[x, y].Y = newY;
                    pt[x, y].Y = (int)newY;
                }
                else
                {
                    fp[x, y].Y = y;
                    pt[x, y].Y = y;
                }
            }
        }

        return OffsetFilterAbs(source, pt);
    }

    public static unsafe BitmapBuffer OffsetFilterAbs(BitmapBuffer source, Point[,] offset)
    {
        var destination = new BitmapBuffer(source.Width, source.Height, source.Channels);
        var width = source.Width;
        var height = source.Height;
        var channels = source.Channels;

        fixed (byte* pSrcBase = source.Pixels)
        fixed (byte* pDstBase = destination.Pixels)
        {
            byte* pDst = pDstBase;

            for (var y = 0; y < height; ++y)
            {
                for (var x = 0; x < width; ++x)
                {
                    var xOffset = offset[x, y].X;
                    var yOffset = offset[x, y].Y;

                    if (yOffset >= 0 && yOffset < height && xOffset >= 0 && xOffset < width)
                    {
                        var sourceIndex = ((yOffset * width) + xOffset) * channels;
                        for (var channel = 0; channel < channels; ++channel)
                        {
                            pDst[((y * width) + x) * channels + channel] = pSrcBase[sourceIndex + channel];
                        }
                    }
                }
            }
        }

        return destination;
    }
}
