using SevenZip;
using LzmaEncoder = SevenZip.Compression.LZMA.Encoder;
using LzmaDecoder = SevenZip.Compression.LZMA.Decoder;

namespace PayBySquare.Core.Encoding;

/// <summary>
/// Raw LZMA1 codec configured exactly like the PAY by square reference pipeline
/// (<c>xz --format=raw --lzma1=lc=3,lp=0,pb=2,dict=128KiB</c>). The 7-Zip managed encoder
/// with these properties and an end marker produces byte-identical output to that pipeline,
/// so no native <c>xz</c> binary is required.
/// </summary>
public static class Lzma1
{
    private const int LiteralContextBits = 3; // lc
    private const int LiteralPosBits = 0;     // lp
    private const int PosStateBits = 2;       // pb
    private const int DictionarySize = 1 << 17; // 128 KiB

    public static byte[] Compress(byte[] data)
    {
        var encoder = new LzmaEncoder();
        encoder.SetCoderProperties(
            new[]
            {
                CoderPropID.DictionarySize,
                CoderPropID.PosStateBits,
                CoderPropID.LitContextBits,
                CoderPropID.LitPosBits,
                CoderPropID.Algorithm,
                CoderPropID.NumFastBytes,
                CoderPropID.MatchFinder,
                CoderPropID.EndMarker,
            },
            new object[] { DictionarySize, PosStateBits, LiteralContextBits, LiteralPosBits, 2, 273, "BT4", true });

        using var input = new MemoryStream(data);
        using var output = new MemoryStream();
        // Raw stream: we deliberately do NOT write the 5-byte properties header.
        encoder.Code(input, output, data.Length, -1, null);
        return output.ToArray();
    }

    public static byte[] Decompress(byte[] compressed, int decompressedLength)
    {
        // Synthesize the 5-byte properties header the decoder expects:
        //   props byte = (pb * 5 + lp) * 9 + lc, followed by the 4-byte little-endian dict size.
        byte propsByte = (byte)((PosStateBits * 5 + LiteralPosBits) * 9 + LiteralContextBits);
        var props = new byte[5];
        props[0] = propsByte;
        props[1] = (byte)(DictionarySize & 0xFF);
        props[2] = (byte)((DictionarySize >> 8) & 0xFF);
        props[3] = (byte)((DictionarySize >> 16) & 0xFF);
        props[4] = (byte)((DictionarySize >> 24) & 0xFF);

        var decoder = new LzmaDecoder();
        decoder.SetDecoderProperties(props);

        using var input = new MemoryStream(compressed);
        using var output = new MemoryStream(decompressedLength);
        decoder.Code(input, output, compressed.Length, decompressedLength, null);
        return output.ToArray();
    }
}
