using System.Buffers.Binary;
using KrLensServer.Core.Exceptions;
using KrLensServer.Core.Models;

namespace KrLensServer.Core.Msi;

public sealed class MsiDecoder
{
    public BitmapBuffer Decode(ReadOnlySpan<byte> data)
    {
        if (data.Length < MsiHeader.HeaderLength + sizeof(uint))
        {
            throw new MsiCorruptedException("MSI payload is too short.");
        }

        var header = MsiHeader.Read(data);
        var totalLength = checked(MsiHeader.HeaderLength + (int)header.MetaLength + (int)header.PixelLength + sizeof(uint));
        if (data.Length != totalLength)
        {
            throw new MsiCorruptedException("MSI payload length does not match the header.");
        }

        var expectedCrc = BinaryPrimitives.ReadUInt32LittleEndian(data[^sizeof(uint)..]);
        var actualCrc = Crc32.Compute(data[..^sizeof(uint)]);
        if (expectedCrc != actualCrc)
        {
            throw new MsiCorruptedException("MSI CRC32 checksum does not match.");
        }

        var metaOffset = MsiHeader.HeaderLength;
        var pixelOffset = metaOffset + (int)header.MetaLength;
        var meta = data.Slice(metaOffset, (int)header.MetaLength);
        var payload = data.Slice(pixelOffset, (int)header.PixelLength);
        var expectedPixelCount = checked((int)header.Width * (int)header.Height * header.Channels);
        var decodedPixels = MsiCompression.Decompress(meta, payload, header.Compression, expectedPixelCount);
        var rgbPixels = MsiColorTransform.ToRgb(decodedPixels, header.Channels, header.ColorSpace);

        return new BitmapBuffer((int)header.Width, (int)header.Height, header.Channels, rgbPixels, takeOwnership: true);
    }
}
