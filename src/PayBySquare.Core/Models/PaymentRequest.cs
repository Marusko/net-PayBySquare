namespace PayBySquare.Core.Models;

/// <summary>
/// A single PAY by square payment order. Mirrors the official data model but keeps the common
/// case simple. Only <see cref="Iban"/> is strictly required; everything else is optional and
/// omitted from the payload when empty (matching the reference generator's byte layout).
/// </summary>
public sealed class PaymentRequest
{
    /// <summary>Optional invoice / order identifier (rarely used).</summary>
    public string? InvoiceId { get; set; }

    /// <summary>Amount to pay. Optional — some QR codes intentionally leave the amount blank.</summary>
    public decimal? Amount { get; set; }

    /// <summary>ISO 4217 currency code. Defaults to EUR; use CZK for Czech accounts.</summary>
    public string CurrencyCode { get; set; } = "EUR";

    /// <summary>Payment due date. Serialized as yyyymmdd. Optional.</summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>Variable symbol (max 10 digits).</summary>
    public string? VariableSymbol { get; set; }

    /// <summary>Constant symbol (max 4 digits).</summary>
    public string? ConstantSymbol { get; set; }

    /// <summary>Specific symbol (max 10 digits).</summary>
    public string? SpecificSymbol { get; set; }

    /// <summary>Originator's reference information (SEPA structured reference).</summary>
    public string? OriginatorReference { get; set; }

    /// <summary>Free-text payment note / message for the recipient (max 140 chars).</summary>
    public string? Note { get; set; }

    /// <summary>
    /// IBAN of the primary account (required). Spaces are ignored.
    /// </summary>
    public string Iban { get; set; } = string.Empty;

    /// <summary>BIC / SWIFT of the primary account. Optional (banks no longer require it).</summary>
    public string? Bic { get; set; }

    /// <summary>
    /// Additional fallback accounts. PAY by square allows several accounts so a payer can pick
    /// the one matching their bank.
    /// </summary>
    public List<BankAccount> AlternativeAccounts { get; set; } = new();

    /// <summary>Beneficiary (payee) name. Required by some banks, e.g. SLSP.</summary>
    public string? BeneficiaryName { get; set; }

    /// <summary>Beneficiary address line 1 (street).</summary>
    public string? BeneficiaryAddressLine1 { get; set; }

    /// <summary>Beneficiary address line 2 (city / ZIP).</summary>
    public string? BeneficiaryAddressLine2 { get; set; }
}

/// <summary>A bank account (IBAN + optional BIC).</summary>
public sealed class BankAccount
{
    public string Iban { get; set; } = string.Empty;
    public string? Bic { get; set; }
}
