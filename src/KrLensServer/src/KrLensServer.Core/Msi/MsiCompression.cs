using System.Buffers.Binary;
using KrLensServer.Core.Exceptions;

namespace KrLensServer.Core.Msi;

internal static class MsiCompression
{
    public static (byte[] Meta, byte[] Payload) Compress(ReadOnlySpan<byte> pixels, MsiCompressionType compression)
    {
        return compression switch
        {
            MsiCompressionType.None => (Array.Empty<byte>(), pixels.ToArray()),
            MsiCompressionType.Huffman => CompressHuffman(pixels),
            _ => throw new UnsupportedImageFormatException($"Compression '{compression}' is not implemented."),
        };
    }

    public static byte[] Decompress(
        ReadOnlySpan<byte> meta,
        ReadOnlySpan<byte> payload,
        MsiCompressionType compression,
        int expectedLength)
    {
        return compression switch
        {
            MsiCompressionType.None => payload.Length == expectedLength
                ? payload.ToArray()
                : throw new MsiCorruptedException("Raw pixel payload length does not match header."),
            MsiCompressionType.Huffman => DecompressHuffman(meta, payload, expectedLength),
            _ => throw new MsiCorruptedException($"Unsupported compression '{compression}'."),
        };
    }

    private static (byte[] Meta, byte[] Payload) CompressHuffman(ReadOnlySpan<byte> pixels)
    {
        var frequencies = BuildFrequencies(pixels);
        var root = BuildTree(frequencies);
        var codes = new Dictionary<byte, bool[]>();
        BuildCodes(root, new List<bool>(), codes);

        var writer = new BitWriter();
        foreach (var pixel in pixels)
        {
            writer.Write(codes[pixel]);
        }

        return (SerializeFrequencies(frequencies), writer.ToArray());
    }

    private static byte[] DecompressHuffman(ReadOnlySpan<byte> meta, ReadOnlySpan<byte> payload, int expectedLength)
    {
        var frequencies = DeserializeFrequencies(meta);
        var root = BuildTree(frequencies);
        if (root.IsLeaf)
        {
            return Enumerable.Repeat(root.Symbol!.Value, expectedLength).ToArray();
        }

        var reader = new BitReader(payload);
        var result = new byte[expectedLength];

        for (var i = 0; i < expectedLength; i++)
        {
            var current = root;
            while (!current.IsLeaf)
            {
                if (!reader.TryReadBit(out var bit))
                {
                    throw new MsiCorruptedException("Huffman payload ended before the expected number of pixels was decoded.");
                }

                current = bit ? current.Right! : current.Left!;
            }

            result[i] = current.Symbol!.Value;
        }

        return result;
    }

    private static int[] BuildFrequencies(ReadOnlySpan<byte> pixels)
    {
        var frequencies = new int[256];
        foreach (var pixel in pixels)
        {
            frequencies[pixel]++;
        }

        return frequencies;
    }

    private static byte[] SerializeFrequencies(IReadOnlyList<int> frequencies)
    {
        var activeEntries = frequencies
            .Select((frequency, symbol) => new { frequency, symbol })
            .Where(entry => entry.frequency > 0)
            .ToArray();

        var meta = new byte[2 + (activeEntries.Length * 5)];
        BinaryPrimitives.WriteUInt16LittleEndian(meta.AsSpan(0, 2), (ushort)activeEntries.Length);
        var offset = 2;
        foreach (var entry in activeEntries)
        {
            meta[offset] = (byte)entry.symbol;
            BinaryPrimitives.WriteInt32LittleEndian(meta.AsSpan(offset + 1, 4), entry.frequency);
            offset += 5;
        }

        return meta;
    }

    private static int[] DeserializeFrequencies(ReadOnlySpan<byte> meta)
    {
        if (meta.Length < 2)
        {
            throw new MsiCorruptedException("Huffman metadata is missing the symbol table header.");
        }

        var entryCount = BinaryPrimitives.ReadUInt16LittleEndian(meta[..2]);
        var expectedLength = 2 + (entryCount * 5);
        if (meta.Length != expectedLength)
        {
            throw new MsiCorruptedException("Huffman metadata length is invalid.");
        }

        var frequencies = new int[256];
        var offset = 2;
        for (var i = 0; i < entryCount; i++)
        {
            var symbol = meta[offset];
            var frequency = BinaryPrimitives.ReadInt32LittleEndian(meta.Slice(offset + 1, 4));
            if (frequency <= 0)
            {
                throw new MsiCorruptedException("Huffman metadata contains a non-positive symbol frequency.");
            }

            frequencies[symbol] = frequency;
            offset += 5;
        }

        return frequencies;
    }

    private static HuffmanNode BuildTree(IReadOnlyList<int> frequencies)
    {
        var queue = new PriorityQueue<HuffmanNode, int>();
        for (var i = 0; i < frequencies.Count; i++)
        {
            if (frequencies[i] <= 0)
            {
                continue;
            }

            queue.Enqueue(new HuffmanNode((byte)i, frequencies[i]), frequencies[i]);
        }

        if (queue.Count == 0)
        {
            throw new MsiCorruptedException("Huffman tree cannot be built from an empty payload.");
        }

        while (queue.Count > 1)
        {
            queue.TryDequeue(out var left, out var leftPriority);
            queue.TryDequeue(out var right, out var rightPriority);
            var parent = new HuffmanNode(left!, right!);
            queue.Enqueue(parent, leftPriority + rightPriority);
        }

        queue.TryDequeue(out var root, out _);
        return root!;
    }

    private static void BuildCodes(HuffmanNode node, List<bool> path, IDictionary<byte, bool[]> codes)
    {
        if (node.IsLeaf)
        {
            codes[node.Symbol!.Value] = path.Count == 0 ? new[] { false } : path.ToArray();
            return;
        }

        path.Add(false);
        BuildCodes(node.Left!, path, codes);
        path.RemoveAt(path.Count - 1);

        path.Add(true);
        BuildCodes(node.Right!, path, codes);
        path.RemoveAt(path.Count - 1);
    }

    private sealed class HuffmanNode
    {
        public HuffmanNode(byte symbol, int frequency)
        {
            Symbol = symbol;
            Frequency = frequency;
        }

        public HuffmanNode(HuffmanNode left, HuffmanNode right)
        {
            Left = left;
            Right = right;
            Frequency = left.Frequency + right.Frequency;
        }

        public byte? Symbol { get; }

        public int Frequency { get; }

        public HuffmanNode? Left { get; }

        public HuffmanNode? Right { get; }

        public bool IsLeaf => Symbol.HasValue;
    }

    private sealed class BitWriter
    {
        private readonly List<byte> _bytes = new();
        private int _bitIndex;
        private byte _current;

        public void Write(ReadOnlySpan<bool> bits)
        {
            foreach (var bit in bits)
            {
                if (bit)
                {
                    _current |= (byte)(1 << (7 - _bitIndex));
                }

                _bitIndex++;
                if (_bitIndex == 8)
                {
                    _bytes.Add(_current);
                    _current = 0;
                    _bitIndex = 0;
                }
            }
        }

        public byte[] ToArray()
        {
            if (_bitIndex > 0)
            {
                _bytes.Add(_current);
                _current = 0;
                _bitIndex = 0;
            }

            return _bytes.ToArray();
        }
    }

    private sealed class BitReader
    {
        private readonly ReadOnlyMemory<byte> _bytes;
        private int _byteIndex;
        private int _bitIndex;

        public BitReader(ReadOnlySpan<byte> bytes)
        {
            _bytes = bytes.ToArray();
        }

        public bool TryReadBit(out bool bit)
        {
            if (_byteIndex >= _bytes.Length)
            {
                bit = false;
                return false;
            }

            bit = (_bytes.Span[_byteIndex] & (1 << (7 - _bitIndex))) != 0;
            _bitIndex++;
            if (_bitIndex == 8)
            {
                _bitIndex = 0;
                _byteIndex++;
            }

            return true;
        }
    }
}
