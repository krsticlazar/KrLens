using System.Buffers.Binary;
using KrLensServer.Core.Exceptions;

namespace KrLensServer.Core.Msi;

public readonly record struct MsiHeader(
    uint Width,
    uint Height,
    byte Channels,
    MsiColorSpace ColorSpace,
    MsiCompressionType Compression,
    uint MetaLength,
    uint PixelLength)
{
    public const ushort CurrentVersion = 0x0001;
    public const ushort HeaderLength = 28;
    public static ReadOnlySpan<byte> Magic => "MSI0"u8;

    public void WriteTo(Span<byte> destination)
    {
        if (destination.Length < HeaderLength)
        {
            throw new ArgumentException($"Destination must be at least {HeaderLength} bytes.", nameof(destination));
        }

        Magic.CopyTo(destination);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[4..6], CurrentVersion);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[6..8], HeaderLength);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[8..12], Width);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[12..16], Height);
        destination[16] = Channels;
        destination[17] = (byte)ColorSpace;
        destination[18] = (byte)Compression;
        destination[19] = 0;
        BinaryPrimitives.WriteUInt32LittleEndian(destination[20..24], MetaLength);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[24..28], PixelLength);
    }

    public static MsiHeader Read(ReadOnlySpan<byte> source)
    {
        if (source.Length < HeaderLength)
        {
            throw new MsiCorruptedException("MSI payload is shorter than the fixed header.");
        }

        if (!source[..4].SequenceEqual(Magic))
        {
            throw new MsiCorruptedException("Invalid MSI magic header.");
        }

        var version = BinaryPrimitives.ReadUInt16LittleEndian(source[4..6]);
        if (version != CurrentVersion)
        {
            throw new MsiCorruptedException($"Unsupported MSI version '{version}'.");
        }

        var headerLength = BinaryPrimitives.ReadUInt16LittleEndian(source[6..8]);
        if (headerLength != HeaderLength)
        {
            throw new MsiCorruptedException($"Unsupported MSI header length '{headerLength}'.");
        }

        var width = BinaryPrimitives.ReadUInt32LittleEndian(source[8..12]);
        var height = BinaryPrimitives.ReadUInt32LittleEndian(source[12..16]);
        var channels = source[16];
        if (channels is not (1 or 3))
        {
            throw new MsiCorruptedException($"Invalid MSI channel count '{channels}'.");
        }

        var colorSpace = (MsiColorSpace)source[17];
        var compression = (MsiCompressionType)source[18];
        var metaLength = BinaryPrimitives.ReadUInt32LittleEndian(source[20..24]);
        var pixelLength = BinaryPrimitives.ReadUInt32LittleEndian(source[24..28]);

        return new MsiHeader(width, height, channels, colorSpace, compression, metaLength, pixelLength);
    }
}
