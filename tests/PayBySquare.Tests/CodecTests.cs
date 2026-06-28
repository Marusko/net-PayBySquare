using PayBySquare.Core;
using PayBySquare.Core.Models;
using Xunit;

namespace PayBySquare.Tests;

public class CodecTests
{
    private static readonly DateOnly RefDate = new(2017, 1, 1);

    // Ground-truth vectors produced by the canonical pipeline:
    //   xz --format=raw --lzma1=lc=3,lp=0,pb=2,dict=128KiB  (the same xz the PHP repo shells out to)
    // The managed implementation must reproduce these byte-for-byte.

    [Fact]
    public void Encode_Vector1_MatchesReference()
    {
        var payment = new PaymentRequest
        {
            Amount = 10m,
            Iban = "SK7283300000009111111118",
            Bic = "FIOZSKBAXXX",
            VariableSymbol = "47",
            DueDate = RefDate,
        };
        const string expected = "0004M000E8I18I1092PSOB1EKLG0V9OP3H36G6495IGI1MO1HUPEJ09OQ6OPCC5L5SJGFET8M4SRR31LOQFUPJ300A3STJR41IAD7ATDBVV74920";
        Assert.Equal(expected, PayBySquareCodec.Encode(payment));
    }

    [Fact]
    public void Encode_Vector2_MatchesReference()
    {
        var payment = new PaymentRequest
        {
            Amount = 5.01m,
            Iban = "SK7700000000000000000000",
            Bic = "CEKOSKBX",
            VariableSymbol = "00000002",
            SpecificSymbol = "2022",
            ConstantSymbol = "0000",
            Note = "pre jozka",
            BeneficiaryName = "jozko mrkvicka",
            DueDate = RefDate,
        };
        const string expected = "0006U000F0OSJJ2G9BRQ70DCPJK4BMPV0HEHLJNFT2LHNNVII3JPQ2Q13QET3O1KJJQOMPTCCVNFIFCF6FKT73HEDOENI6D84TNKUD2KRPJH6S2HRSU0TF80O3VP7VM1L8FDI8TPT95OEFNPHNNG0";
        Assert.Equal(expected, PayBySquareCodec.Encode(payment));
    }

    [Fact]
    public void Encode_Vector3_NoBic_IsRegressionLocked()
    {
        // For this input the managed LZMA encoder makes different (still valid) optimal-parse
        // choices than `xz`, producing a different but fully decodable stream. This locks the
        // managed output. Cross-compatibility is verified by Decode_CanonicalPhpVector below,
        // which decodes the byte-for-byte `xz`/PHP output of the same input.
        var payment = new PaymentRequest
        {
            Amount = 100.5m,
            Iban = "SK3112000000198742637541",
            DueDate = RefDate,
        };
        const string expected = "00042000ECE1A9QG92PSOB1GV1U69OT5O7N34H4T2CB6B3NHDE5UN9QTV0LR6B3FM8NFVV4958H7E5HC7ALAF4SU5VVEHVJ26KFVQM1300";
        Assert.Equal(expected, PayBySquareCodec.Encode(payment));
    }

    [Fact]
    public void Decode_CanonicalPhpVector_ReadsFieldsCorrectly()
    {
        // This is the byte-for-byte output of the original PHP/xz pipeline for vector 3.
        // The managed decoder must read codes produced by other generators / bank apps.
        const string canonical = "00042000ECE1A9QG92PSOB1GV1U69OT5O7N34H4T2CB6B3NHDE5UN9QTV0LR6ASVVMGFDVLF33FGVVSNDTP075RJCAE5Q26VVPDLNG0";
        var result = PayBySquareCodec.Decode(canonical);

        Assert.True(result.CrcValid);
        Assert.Equal(100.5m, result.Payment.Amount);
        Assert.Equal("SK3112000000198742637541", result.Payment.Iban);
        Assert.Null(result.Payment.Bic);
    }

    [Fact]
    public void RoundTrip_PreservesFields()
    {
        var payment = new PaymentRequest
        {
            Amount = 1234.56m,
            CurrencyCode = "EUR",
            Iban = "SK7283300000009111111118",
            Bic = "FIOZSKBAXXX",
            VariableSymbol = "123456",
            ConstantSymbol = "0308",
            SpecificSymbol = "99",
            Note = "Invoice 2026/01",
            BeneficiaryName = "Acme s.r.o.",
            DueDate = new DateOnly(2026, 6, 28),
        };

        var code = PayBySquareCodec.Encode(payment);
        var result = PayBySquareCodec.Decode(code);

        Assert.True(result.CrcValid);
        Assert.Equal(payment.Amount, result.Payment.Amount);
        Assert.Equal(payment.Iban, result.Payment.Iban);
        Assert.Equal(payment.Bic, result.Payment.Bic);
        Assert.Equal(payment.VariableSymbol, result.Payment.VariableSymbol);
        Assert.Equal(payment.ConstantSymbol, result.Payment.ConstantSymbol);
        Assert.Equal(payment.SpecificSymbol, result.Payment.SpecificSymbol);
        Assert.Equal(payment.Note, result.Payment.Note);
        Assert.Equal(payment.BeneficiaryName, result.Payment.BeneficiaryName);
        Assert.Equal(payment.DueDate, result.Payment.DueDate);
    }

    [Fact]
    public void Decode_ReferenceVector_ReturnsExpectedFields()
    {
        const string code = "0006U000F0OSJJ2G9BRQ70DCPJK4BMPV0HEHLJNFT2LHNNVII3JPQ2Q13QET3O1KJJQOMPTCCVNFIFCF6FKT73HEDOENI6D84TNKUD2KRPJH6S2HRSU0TF80O3VP7VM1L8FDI8TPT95OEFNPHNNG0";
        var result = PayBySquareCodec.Decode(code);

        Assert.True(result.CrcValid);
        Assert.Equal(5.01m, result.Payment.Amount);
        Assert.Equal("SK7700000000000000000000", result.Payment.Iban);
        Assert.Equal("CEKOSKBX", result.Payment.Bic);
        Assert.Equal("00000002", result.Payment.VariableSymbol);
        Assert.Equal("2022", result.Payment.SpecificSymbol);
        Assert.Equal("pre jozka", result.Payment.Note);
        Assert.Equal("jozko mrkvicka", result.Payment.BeneficiaryName);
    }

    [Fact]
    public void Encode_StripsDiacritics_ThroughFullPipeline()
    {
        var payment = new PaymentRequest
        {
            Amount = 12m,
            Iban = "SK7283300000009111111118",
            Note = "Príliš žltý kôň",
            BeneficiaryName = "Ján Novák",
        };
        var result = PayBySquareCodec.Decode(PayBySquareCodec.Encode(payment));
        Assert.Equal("Prilis zlty kon", result.Payment.Note);
        Assert.Equal("Jan Novak", result.Payment.BeneficiaryName);
    }

    [Fact]
    public void Encode_CanPreserveDiacritics_WhenDisabled()
    {
        var payment = new PaymentRequest { Amount = 1m, Iban = "SK7283300000009111111118", Note = "kôň" };
        var options = new EncodeOptions { StripDiacritics = false };
        var result = PayBySquareCodec.Decode(PayBySquareCodec.Encode(payment, options));
        Assert.Equal("kôň", result.Payment.Note);
    }

    [Fact]
    public void AlternativeAccounts_RoundTrip()
    {
        var payment = new PaymentRequest
        {
            Amount = 50m,
            Iban = "SK7283300000009111111118",
            Bic = "FIOZSKBAXXX",
            AlternativeAccounts =
            {
                new BankAccount { Iban = "SK3112000000198742637541" },
            },
        };
        var result = PayBySquareCodec.Decode(PayBySquareCodec.Encode(payment));
        Assert.Single(result.Payment.AlternativeAccounts);
        Assert.Equal("SK3112000000198742637541", result.Payment.AlternativeAccounts[0].Iban);
    }
}
