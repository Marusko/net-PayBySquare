using System.Buffers.Binary;
using System.IO.Compression;
using PayBySquare.Core.Encoding;

namespace PayBySquare.Core.Qr;

/// <summary>
/// Minimal PNG encoder for 8-bit RGB images. Uses only the framework's <see cref="DeflateStream"/>
/// (no third-party imaging library). Re-uses the project's standard CRC-32 for chunk checksums.
/// </summary>
public static class PngEncoder
{
    private static readonly byte[] Signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    /// <param name="rgb">Row-major RGB bytes, length = width * height * 3.</param>
    public static byte[] Encode(int width, int height, byte[] rgb)
    {
        using var output = new MemoryStream();
        output.Write(Signature);

        Span<byte> ihdr = stackalloc byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(ihdr[..4], (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(ihdr.Slice(4, 4), (uint)height);
        ihdr[8] = 8;  // bit depth
        ihdr[9] = 2;  // colour type: truecolour (RGB)
        ihdr[10] = 0; // compression
        ihdr[11] = 0; // filter
        ihdr[12] = 0; // interlace
        WriteChunk(output, "IHDR", ihdr);

        WriteChunk(output, "IDAT", Zlib(BuildRawScanlines(width, height, rgb)));
        WriteChunk(output, "IEND", ReadOnlySpan<byte>.Empty);
        return output.ToArray();
    }

    private static byte[] BuildRawScanlines(int width, int height, byte[] rgb)
    {
        int rowBytes = width * 3;
        var raw = new byte[height * (rowBytes + 1)];
        for (int y = 0; y < height; y++)
        {
            int dst = y * (rowBytes + 1);
            raw[dst] = 0; // filter type: none
            Buffer.BlockCopy(rgb, y * rowBytes, raw, dst + 1, rowBytes);
        }
        return raw;
    }

    private static byte[] Zlib(byte[] data)
    {
        using var ms = new MemoryStream();
        ms.WriteByte(0x78); // zlib header (CMF)
        ms.WriteByte(0x9C); // FLG (default compression)
        using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true))
            deflate.Write(data, 0, data.Length);
        Span<byte> adler = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(adler, Adler32(data));
        ms.Write(adler);
        return ms.ToArray();
    }

    private static uint Adler32(byte[] data)
    {
        const uint mod = 65521;
        uint a = 1, b = 0;
        foreach (var x in data)
        {
            a = (a + x) % mod;
            b = (b + a) % mod;
        }
        return (b << 16) | a;
    }

    private static void WriteChunk(Stream output, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> len = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(len, (uint)data.Length);
        output.Write(len);

        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        output.Write(typeBytes);
        output.Write(data);

        var crcInput = new byte[typeBytes.Length + data.Length];
        Buffer.BlockCopy(typeBytes, 0, crcInput, 0, typeBytes.Length);
        data.CopyTo(crcInput.AsSpan(typeBytes.Length));
        Span<byte> crc = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32.Compute(crcInput));
        output.Write(crc);
    }
}
