namespace PayBySquare.Core.Encoding;

/// <summary>
/// Base32hex variant used by PAY by square. Bytes are written as a big-endian bit stream
/// and consumed in 5-bit groups, padded with zero bits, mapped through the alphabet
/// <c>0-9 A-V</c>. There is no '=' padding.
/// </summary>
public static class Base32Hex
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUV";

    public static string Encode(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0) return string.Empty;

        // Number of 5-bit groups, rounded up.
        int totalBits = data.Length * 8;
        int groups = (totalBits + 4) / 5;
        var sb = new System.Text.StringBuilder(groups);

        int buffer = 0;
        int bitsInBuffer = 0;
        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsInBuffer += 8;
            while (bitsInBuffer >= 5)
            {
                bitsInBuffer -= 5;
                int index = (buffer >> bitsInBuffer) & 0x1F;
                sb.Append(Alphabet[index]);
            }
        }
        if (bitsInBuffer > 0)
        {
            int index = (buffer << (5 - bitsInBuffer)) & 0x1F;
            sb.Append(Alphabet[index]);
        }
        return sb.ToString();
    }

    public static byte[] Decode(string code)
    {
        if (string.IsNullOrEmpty(code)) return Array.Empty<byte>();

        int buffer = 0;
        int bitsInBuffer = 0;
        var output = new List<byte>(code.Length * 5 / 8 + 1);

        foreach (var rawCh in code)
        {
            char ch = char.ToUpperInvariant(rawCh);
            int value = Alphabet.IndexOf(ch);
            if (value < 0)
                throw new FormatException($"Invalid base32hex character '{rawCh}'.");

            buffer = (buffer << 5) | value;
            bitsInBuffer += 5;
            if (bitsInBuffer >= 8)
            {
                bitsInBuffer -= 8;
                output.Add((byte)((buffer >> bitsInBuffer) & 0xFF));
            }
        }
        return output.ToArray();
    }
}
