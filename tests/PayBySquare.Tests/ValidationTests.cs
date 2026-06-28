using PayBySquare.Core.Encoding;
using PayBySquare.Core.Validation;
using Xunit;

namespace PayBySquare.Tests;

public class ValidationTests
{
    [Theory]
    [InlineData("SK7283300000009111111118")]   // Slovak
    [InlineData("SK31 1200 0000 1987 4263 7541")] // Slovak with spaces
    [InlineData("CZ6508000000192000145399")]    // Czech (issue #1)
    [InlineData("DE89370400440532013000")]      // German
    public void IbanValidator_AcceptsValidIbans(string iban) =>
        Assert.True(IbanValidator.IsValid(iban));

    [Theory]
    [InlineData("SK7283300000009111111119")]   // bad check digit
    [InlineData("SK721")]                        // too short
    [InlineData("XX00000000000000000000")]      // bad check digit
    [InlineData("")]                             // empty
    public void IbanValidator_RejectsInvalidIbans(string iban) =>
        Assert.False(IbanValidator.IsValid(iban));

    [Fact]
    public void Crc32_KnownValue()
    {
        // CRC32 of "123456789" is 0xCBF43926 (standard test vector).
        var bytes = System.Text.Encoding.ASCII.GetBytes("123456789");
        Assert.Equal(0xCBF43926u, Crc32.Compute(bytes));
    }

    [Fact]
    public void Base32Hex_RoundTrip()
    {
        var data = new byte[] { 0x00, 0x00, 0x4B, 0x00, 0xFF, 0x12, 0x34, 0x56, 0x78, 0x9A };
        var encoded = Base32Hex.Encode(data);
        Assert.Equal(data, Base32Hex.Decode(encoded));
    }

    [Theory]
    [InlineData("Príliš žltý kôň", "Prilis zlty kon")]
    [InlineData("Měšťan", "Mestan")]
    [InlineData("Łódź", "Lodz")]
    public void Diacritics_AreStripped(string input, string expected) =>
        Assert.Equal(expected, Diacritics.Remove(input));
}
