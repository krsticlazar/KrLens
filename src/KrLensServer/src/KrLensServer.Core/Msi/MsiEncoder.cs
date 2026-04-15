using System.Buffers.Binary;
using KrLensServer.Core.Models;

namespace KrLensServer.Core.Msi;

public sealed class MsiEncoder
{
    public byte[] Encode(BitmapBuffer source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var colorSpace = source.Channels == 1 ? MsiColorSpace.Linear : MsiColorSpace.Rgb;
        var compression = MsiCompressionType.Huffman;
        var transformedPixels = MsiColorTransform.FromRgb(source.Pixels, (byte)source.Channels, colorSpace);
        var (meta, payload) = MsiCompression.Compress(transformedPixels, compression);

        var header = new MsiHeader(
            (uint)source.Width,
            (uint)source.Height,
            (byte)source.Channels,
            colorSpace,
            compression,
            (uint)meta.Length,
            (uint)payload.Length);

        var totalLength = MsiHeader.HeaderLength + meta.Length + payload.Length + sizeof(uint);
        var result = new byte[totalLength];
        header.WriteTo(result.AsSpan(0, MsiHeader.HeaderLength));
        meta.CopyTo(result, MsiHeader.HeaderLength);
        payload.CopyTo(result, MsiHeader.HeaderLength + meta.Length);

        var crcOffset = totalLength - sizeof(uint);
        var crc = Crc32.Compute(result.AsSpan(0, crcOffset));
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(crcOffset, sizeof(uint)), crc);

        return result;
    }
}
