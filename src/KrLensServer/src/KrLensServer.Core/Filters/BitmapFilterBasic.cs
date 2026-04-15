using KrLensServer.Core.Models;

namespace KrLensServer.Core.Filters;

internal static class BitmapFilterBasic
{
    public static unsafe BitmapBuffer Invert(BitmapBuffer source)
    {
        var destination = new BitmapBuffer(source.Width, source.Height, source.Channels);
        var stride = source.Stride;
        var nWidth = source.Width * source.Channels;
        var nOffset = stride - nWidth;

        fixed (byte* p = source.Pixels)
        fixed (byte* pDstBase = destination.Pixels)
        {
            byte* pSrc = p;
            byte* pDst = pDstBase;

            for (var y = 0; y < source.Height; ++y)
            {
                for (var x = 0; x < nWidth; ++x)
                {
                    pDst[0] = (byte)(255 - pSrc[0]);
                    ++pSrc;
                    ++pDst;
                }

                pSrc += nOffset;
                pDst += nOffset;
            }
        }

        return destination;
    }

    public static unsafe BitmapBuffer GrayScale(BitmapBuffer source)
    {
        if (source.Channels == 1)
        {
            return source.Clone();
        }

        var destination = new BitmapBuffer(source.Width, source.Height, 1);

        fixed (byte* pSrcBase = source.Pixels)
        fixed (byte* pDstBase = destination.Pixels)
        {
            byte* pSrc = pSrcBase;
            byte* pDst = pDstBase;

            for (var y = 0; y < source.Height; ++y)
            {
                for (var x = 0; x < source.Width; ++x)
                {
                    var blue = pSrc[2];
                    var green = pSrc[1];
                    var red = pSrc[0];

                    pDst[0] = (byte)(0.299 * red + 0.587 * green + 0.114 * blue);

                    pSrc += 3;
                    ++pDst;
                }
            }
        }

        return destination;
    }

    public static unsafe BitmapBuffer Brightness(BitmapBuffer source, int nBrightness)
    {
        var destination = new BitmapBuffer(source.Width, source.Height, source.Channels);
        var stride = source.Stride;
        var nWidth = source.Width * source.Channels;
        var nOffset = stride - nWidth;

        fixed (byte* pSrcBase = source.Pixels)
        fixed (byte* pDstBase = destination.Pixels)
        {
            byte* pSrc = pSrcBase;
            byte* pDst = pDstBase;
            int nVal;

            for (var y = 0; y < source.Height; ++y)
            {
                for (var x = 0; x < nWidth; ++x)
                {
                    nVal = pSrc[0] + nBrightness;

                    if (nVal < 0) nVal = 0;
                    if (nVal > 255) nVal = 255;

                    pDst[0] = (byte)nVal;

                    ++pSrc;
                    ++pDst;
                }

                pSrc += nOffset;
                pDst += nOffset;
            }
        }

        return destination;
    }

    public static unsafe BitmapBuffer Contrast(BitmapBuffer source, double factor)
    {
        var destination = new BitmapBuffer(source.Width, source.Height, source.Channels);
        var stride = source.Stride;
        var nWidth = source.Width * source.Channels;
        var nOffset = stride - nWidth;

        fixed (byte* pSrcBase = source.Pixels)
        fixed (byte* pDstBase = destination.Pixels)
        {
            byte* pSrc = pSrcBase;
            byte* pDst = pDstBase;
            double pixel;

            for (var y = 0; y < source.Height; ++y)
            {
                for (var x = 0; x < nWidth; ++x)
                {
                    pixel = pSrc[0] / 255.0;
                    pixel -= 0.5;
                    pixel *= factor;
                    pixel += 0.5;
                    pixel *= 255.0;

                    if (pixel < 0) pixel = 0;
                    if (pixel > 255) pixel = 255;

                    pDst[0] = (byte)pixel;

                    ++pSrc;
                    ++pDst;
                }

                pSrc += nOffset;
                pDst += nOffset;
            }
        }

        return destination;
    }

    public static unsafe BitmapBuffer Gamma(BitmapBuffer source, double gamma)
    {
        var destination = new BitmapBuffer(source.Width, source.Height, source.Channels);
        var gammaArray = new byte[256];

        for (var i = 0; i < 256; ++i)
        {
            gammaArray[i] = (byte)Math.Min(255, (int)((255.0 * Math.Pow(i / 255.0, 1.0 / gamma)) + 0.5));
        }

        var stride = source.Stride;
        var nWidth = source.Width * source.Channels;
        var nOffset = stride - nWidth;

        fixed (byte* pSrcBase = source.Pixels)
        fixed (byte* pDstBase = destination.Pixels)
        fixed (byte* pGamma = gammaArray)
        {
            byte* pSrc = pSrcBase;
            byte* pDst = pDstBase;

            for (var y = 0; y < source.Height; ++y)
            {
                for (var x = 0; x < nWidth; ++x)
                {
                    pDst[0] = pGamma[pSrc[0]];
                    ++pSrc;
                    ++pDst;
                }

                pSrc += nOffset;
                pDst += nOffset;
            }
        }

        return destination;
    }
}
