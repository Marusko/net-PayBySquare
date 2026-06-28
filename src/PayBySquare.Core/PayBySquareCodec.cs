using System.Text;
using PayBySquare.Core.Encoding;
using PayBySquare.Core.Models;

namespace PayBySquare.Core;

/// <summary>Options controlling how a payment is encoded.</summary>
public sealed class EncodeOptions
{
    /// <summary>Strip diacritics from text fields (note, beneficiary). Recommended for bank compatibility.</summary>
    public bool StripDiacritics { get; set; } = true;

    public static readonly EncodeOptions Default = new();
}

/// <summary>Result of decoding a PAY by square string.</summary>
public sealed class DecodeResult
{
    public required PaymentRequest Payment { get; init; }
    public required string RawData { get; init; }
    public required bool CrcValid { get; init; }
}

/// <summary>
/// Encodes and decodes the PAY by square string (the textual payload placed inside the QR code).
///
/// Pipeline: tab-delimited data → prepend CRC32 (little-endian) → raw LZMA1 compress →
/// prepend 2 header bytes + 2-byte little-endian decompressed length → base32hex.
/// </summary>
public static class PayBySquareCodec
{
    public static string Encode(PaymentRequest payment, EncodeOptions? options = null)
    {
        options ??= EncodeOptions.Default;

        var data = DataModelSerializer.Serialize(payment, options.StripDiacritics);
        var dataBytes = System.Text.Encoding.UTF8.GetBytes(data);

        // CRC32 (little-endian) prepended to the data.
        var crc = Crc32.ComputeLittleEndian(dataBytes);
        var payload = new byte[crc.Length + dataBytes.Length];
        Buffer.BlockCopy(crc, 0, payload, 0, crc.Length);
        Buffer.BlockCopy(dataBytes, 0, payload, crc.Length, dataBytes.Length);

        var compressed = Lzma1.Compress(payload);

        // Header: 0x00 0x00, then the decompressed length as a 2-byte little-endian value.
        var output = new byte[4 + compressed.Length];
        output[0] = 0x00;
        output[1] = 0x00;
        output[2] = (byte)(payload.Length & 0xFF);
        output[3] = (byte)((payload.Length >> 8) & 0xFF);
        Buffer.BlockCopy(compressed, 0, output, 4, compressed.Length);

        return Base32Hex.Encode(output);
    }

    public static DecodeResult Decode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is empty.", nameof(code));

        var bytes = Base32Hex.Decode(code.Trim());
        if (bytes.Length < 4)
            throw new FormatException("Code is too short to contain a valid header.");

        int decompressedLength = bytes[2] | (bytes[3] << 8);
        var compressed = new byte[bytes.Length - 4];
        Buffer.BlockCopy(bytes, 4, compressed, 0, compressed.Length);

        var payload = Lzma1.Decompress(compressed, decompressedLength);
        if (payload.Length < 4)
            throw new FormatException("Decompressed payload is too short.");

        var data = payload.AsSpan(4).ToArray();
        var dataString = System.Text.Encoding.UTF8.GetString(data);

        uint expectedCrc = (uint)(payload[0] | (payload[1] << 8) | (payload[2] << 16) | (payload[3] << 24));
        bool crcValid = Crc32.Compute(data) == expectedCrc;

        return new DecodeResult
        {
            Payment = DataModelSerializer.Deserialize(dataString),
            RawData = dataString,
            CrcValid = crcValid,
        };
    }
}
