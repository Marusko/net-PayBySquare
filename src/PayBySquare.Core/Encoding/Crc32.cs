namespace PayBySquare.Core.Encoding;

/// <summary>
/// Standard CRC-32 (ISO-HDLC / "crc32b" in PHP): reflected, polynomial 0xEDB88320,
/// init 0xFFFFFFFF, xor-out 0xFFFFFFFF.
/// </summary>
public static class Crc32
{
    private static readonly uint[] Table = BuildTable();

    private static uint[] BuildTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint c = i;
            for (int k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            table[i] = c;
        }
        return table;
    }

    public static uint Compute(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var b in data)
            crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFF;
    }

    /// <summary>
    /// CRC-32 as 4 little-endian bytes. The PAY by square format prepends these to the
    /// payload (the PHP reference does <c>strrev(hash("crc32b", $d, true))</c>, i.e. the
    /// big-endian digest reversed into little-endian).
    /// </summary>
    public static byte[] ComputeLittleEndian(ReadOnlySpan<byte> data)
    {
        uint crc = Compute(data);
        return new[]
        {
            (byte)(crc & 0xFF),
            (byte)((crc >> 8) & 0xFF),
            (byte)((crc >> 16) & 0xFF),
            (byte)((crc >> 24) & 0xFF),
        };
    }
}
