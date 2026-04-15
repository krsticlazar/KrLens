namespace KrLensServer.Core.Msi;

public enum MsiCompressionType : byte
{
    None = 0,
    ShannonFano = 1,
    Huffman = 2,
    DownsamplingMpeg1 = 3,
    DownsamplingMpeg2 = 4,
}
