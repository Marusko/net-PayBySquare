using Microsoft.AspNetCore.Mvc;
using PayBySquare.Core.Models;

namespace PayBySquare.Api;

/// <summary>Payment fields accepted on the JSON (POST) endpoints.</summary>
public sealed class PaymentDto
{
    public decimal? Amount { get; set; }
    public string? CurrencyCode { get; set; }
    /// <summary>Due date as yyyy-MM-dd.</summary>
    public DateOnly? DueDate { get; set; }
    public string? Iban { get; set; }
    public string? Bic { get; set; }
    public string? VariableSymbol { get; set; }
    public string? ConstantSymbol { get; set; }
    public string? SpecificSymbol { get; set; }
    public string? OriginatorReference { get; set; }
    public string? Note { get; set; }
    public string? BeneficiaryName { get; set; }
    public string? BeneficiaryAddressLine1 { get; set; }
    public string? BeneficiaryAddressLine2 { get; set; }
    public List<BankAccount>? AlternativeAccounts { get; set; }

    public PaymentRequest ToModel() => new()
    {
        Amount = Amount,
        CurrencyCode = string.IsNullOrWhiteSpace(CurrencyCode) ? "EUR" : CurrencyCode!.Trim(),
        DueDate = DueDate,
        Iban = (Iban ?? string.Empty).Trim(),
        Bic = Bic,
        VariableSymbol = VariableSymbol,
        ConstantSymbol = ConstantSymbol,
        SpecificSymbol = SpecificSymbol,
        OriginatorReference = OriginatorReference,
        Note = Note,
        BeneficiaryName = BeneficiaryName,
        BeneficiaryAddressLine1 = BeneficiaryAddressLine1,
        BeneficiaryAddressLine2 = BeneficiaryAddressLine2,
        AlternativeAccounts = AlternativeAccounts ?? new(),
    };
}

/// <summary>Query parameters shared by the GET QR endpoint (handy for &lt;img src&gt; embedding).</summary>
public sealed class QrQuery
{
    // Payment fields (with friendly aliases for the original PHP qr.php names).
    [FromQuery(Name = "amount")] public decimal? Amount { get; set; }
    [FromQuery(Name = "price")] public decimal? Price { get; set; }
    [FromQuery(Name = "currency")] public string? Currency { get; set; }
    [FromQuery(Name = "date")] public DateOnly? Date { get; set; }
    [FromQuery(Name = "iban")] public string? Iban { get; set; }
    [FromQuery(Name = "bic")] public string? Bic { get; set; }
    [FromQuery(Name = "swift")] public string? Swift { get; set; }
    [FromQuery(Name = "vs")] public string? Vs { get; set; }
    [FromQuery(Name = "cs")] public string? Cs { get; set; }
    [FromQuery(Name = "ss")] public string? Ss { get; set; }
    [FromQuery(Name = "note")] public string? Note { get; set; }
    [FromQuery(Name = "beneficiary")] public string? Beneficiary { get; set; }
    [FromQuery(Name = "recipient")] public string? Recipient { get; set; }

    // Rendering options.
    [FromQuery(Name = "format")] public string? Format { get; set; }       // png | svg | string
    [FromQuery(Name = "size")] public int? Size { get; set; }              // target px
    [FromQuery(Name = "ppm")] public int? Ppm { get; set; }               // pixels per module
    [FromQuery(Name = "margin")] public bool? Margin { get; set; }         // draw quiet zone
    [FromQuery(Name = "ecc")] public string? Ecc { get; set; }            // L|M|Q|H
    [FromQuery(Name = "dark")] public string? Dark { get; set; }          // #RRGGBB
    [FromQuery(Name = "light")] public string? Light { get; set; }        // #RRGGBB
    [FromQuery(Name = "logo")] public bool? Logo { get; set; }            // PAY by square frame (default true)
    [FromQuery(Name = "brandcolor")] public string? BrandColor { get; set; } // #RRGGBB

    public PaymentDto ToPayment() => new()
    {
        Amount = Amount ?? Price,
        CurrencyCode = Currency,
        DueDate = Date,
        Iban = Iban,
        Bic = Bic ?? Swift,
        VariableSymbol = Vs,
        ConstantSymbol = Cs,
        SpecificSymbol = Ss,
        Note = Note,
        BeneficiaryName = Beneficiary ?? Recipient,
    };
}

/// <summary>Visual options accepted on the POST QR endpoint via query string.</summary>
public sealed class RenderQuery
{
    [FromQuery(Name = "format")] public string? Format { get; set; }
    [FromQuery(Name = "size")] public int? Size { get; set; }
    [FromQuery(Name = "ppm")] public int? Ppm { get; set; }
    [FromQuery(Name = "margin")] public bool? Margin { get; set; }
    [FromQuery(Name = "ecc")] public string? Ecc { get; set; }
    [FromQuery(Name = "dark")] public string? Dark { get; set; }
    [FromQuery(Name = "light")] public string? Light { get; set; }
    [FromQuery(Name = "logo")] public bool? Logo { get; set; }
    [FromQuery(Name = "brandcolor")] public string? BrandColor { get; set; }
}

public sealed record EncodeResponse(string Code);

public sealed record DecodeRequest(string Code);

public sealed record DecodeResponse(bool CrcValid, string RawData, PaymentDto Payment);

public sealed record ValidateRequest(string Iban);

public sealed record ValidateResponse(bool Valid, string Normalized, string? Error);
