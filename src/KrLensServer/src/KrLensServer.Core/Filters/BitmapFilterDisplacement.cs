using System.Drawing;
using System.Drawing.Imaging;
using KrLensServer.Core.Models;

namespace KrLensServer.Core.Filters;

internal static class BitmapFilterDisplacement
{
    internal struct FloatPoint
    {
        public double X;
        public double Y;
    }

    internal struct OffsetPoint
    {
        public int X;
        public int Y;
    }

    public static BitmapBuffer Flip(BitmapBuffer source, bool horizontal, bool vertical)
    {
        var map = new OffsetPoint[source.Width, source.Height];
        var width = source.Width;
        var height = source.Height;

        for (var x = 0; x < width; ++x)
        {
            for (var y = 0; y < height; ++y)
            {
                map[x, y].X = horizontal ? width - (x + 1) : x;
                map[x, y].Y = vertical ? height - (y + 1) : y;
            }
        }

        return OffsetFilterAbs(source, map);
    }

    public static BitmapBuffer Water(BitmapBuffer source, double amplitude, double wavelength)
    {
        var width = source.Width;
        var height = source.Height;
        var floatMap = new FloatPoint[width, height];
        var map = new OffsetPoint[width, height];

        double newX;
        double newY;
        double xo;
        double yo;

        for (var x = 0; x < width; ++x)
        {
            for (var y = 0; y < height; ++y)
            {
                xo = amplitude * Math.Sin((2.0 * Math.PI * y) / wavelength);
                yo = amplitude * Math.Cos((2.0 * Math.PI * x) / wavelength);

                newX = x + xo;
                newY = y + yo;

                if (newX > 0 && newX < width)
                {
                    floatMap[x, y].X = newX;
                    map[x, y].X = (int)newX;
                }
                else
                {
                    floatMap[x, y].X = x;
                    map[x, y].X = x;
                }

                if (newY > 0 && newY < height)
                {
                    floatMap[x, y].Y = newY;
                    map[x, y].Y = (int)newY;
                }
                else
                {
                    floatMap[x, y].Y = y;
                    map[x, y].Y = y;
                }
            }
        }

        return OffsetFilterAbs(source, map);
    }

    public static BitmapBuffer OffsetFilterAbs(BitmapBuffer source, OffsetPoint[,] offset)
    {
        using var sourceBitmap = BitmapFilterSupport.CreateBitmap(source);
        using var destinationBitmap = BitmapFilterSupport.CreateEmpty(sourceBitmap);
        var rect = new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height);
        var sourceData = sourceBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        var destinationData = destinationBitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

        try
        {
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
                        var xOffset = offset[x, y].X;
                        var yOffset = offset[x, y].Y;

                        if (yOffset >= 0 && yOffset < destinationBitmap.Height && xOffset >= 0 && xOffset < destinationBitmap.Width)
                        {
                            var sourceIndex = (yOffset * strideSrc) + (xOffset * 3);
                            pDst[0] = pSrcBase[sourceIndex];
                            pDst[1] = pSrcBase[sourceIndex + 1];
                            pDst[2] = pSrcBase[sourceIndex + 2];
                        }
                        else
                        {
                            pDst[0] = 0;
                            pDst[1] = 0;
                            pDst[2] = 0;
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
}
