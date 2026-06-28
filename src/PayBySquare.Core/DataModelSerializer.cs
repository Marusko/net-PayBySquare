using System.Globalization;
using PayBySquare.Core.Encoding;
using PayBySquare.Core.Models;
using PayBySquare.Core.Validation;

namespace PayBySquare.Core;

/// <summary>
/// Builds and parses the tab-delimited PAY by square data model (the string that is then
/// CRC-prefixed, LZMA-compressed and base32-encoded). The layout matches the official
/// specification and is byte-compatible with the common single-payment reference output.
/// </summary>
public static class DataModelSerializer
{
    private const char Sep = '\t';

    public static string Serialize(PaymentRequest payment, bool stripDiacritics)
    {
        string Clean(string? s) => stripDiacritics ? Diacritics.Remove(s ?? string.Empty) : (s ?? string.Empty);

        var accounts = new List<BankAccount> { new() { Iban = payment.Iban, Bic = payment.Bic } };
        accounts.AddRange(payment.AlternativeAccounts);

        var fields = new List<string>
        {
            payment.InvoiceId ?? string.Empty,   // 0  invoice id
            "1",                                  // 1  number of payments
            "1",                                  // 2  payment options (1 = payment order)
            FormatAmount(payment.Amount),         // 3  amount
            (payment.CurrencyCode ?? "EUR").ToUpperInvariant(), // 4 currency
            payment.DueDate is { } d ? d.ToString("yyyyMMdd") : string.Empty, // 5 due date
            payment.VariableSymbol ?? string.Empty,  // 6
            payment.ConstantSymbol ?? string.Empty,  // 7
            payment.SpecificSymbol ?? string.Empty,  // 8
            Clean(payment.OriginatorReference),      // 9
            Clean(payment.Note),                     // 10
            accounts.Count.ToString(CultureInfo.InvariantCulture), // 11 number of accounts
        };

        foreach (var account in accounts)
        {
            fields.Add(IbanValidator.Normalize(account.Iban));      // iban
            fields.Add((account.Bic ?? string.Empty).Trim().ToUpperInvariant()); // bic
        }

        fields.Add("0"); // standing order extension flag
        fields.Add("0"); // direct debit extension flag

        // Beneficiary fields. The name is always emitted (matching the reference layout); the
        // address lines are appended only when supplied so the simple case stays byte-identical.
        fields.Add(Clean(payment.BeneficiaryName));
        if (!string.IsNullOrEmpty(payment.BeneficiaryAddressLine1) || !string.IsNullOrEmpty(payment.BeneficiaryAddressLine2))
        {
            fields.Add(Clean(payment.BeneficiaryAddressLine1));
            fields.Add(Clean(payment.BeneficiaryAddressLine2));
        }

        return string.Join(Sep, fields);
    }

    public static string FormatAmount(decimal? amount) =>
        amount?.ToString("0.############", CultureInfo.InvariantCulture) ?? string.Empty;

    /// <summary>
    /// Parses the first payment of a tab-delimited data string back into a
    /// <see cref="PaymentRequest"/>. Handles multiple accounts and the optional beneficiary lines.
    /// </summary>
    public static PaymentRequest Deserialize(string data)
    {
        var f = data.Split(Sep);
        int i = 0;
        string Next() => i < f.Length ? f[i++] : string.Empty;

        Next();                       // 0 invoice id
        var invoiceId = f.Length > 0 ? f[0] : string.Empty;
        Next();                       // 1 payments count (we read the first payment only)

        Next();                       // 2 payment options
        var amountStr = Next();       // 3 amount
        var currency = Next();        // 4 currency
        var dueStr = Next();          // 5 due date
        var vs = Next();              // 6
        var cs = Next();              // 7
        var ss = Next();              // 8
        var originator = Next();      // 9
        var note = Next();            // 10
        int accountCount = int.TryParse(Next(), out var ac) ? ac : 0; // 11

        var accounts = new List<BankAccount>();
        for (int a = 0; a < accountCount; a++)
            accounts.Add(new BankAccount { Iban = Next(), Bic = Next() });

        var standing = Next();        // standing order ext flag
        if (standing == "1") SkipFields(ref i, 4);
        var directDebit = Next();     // direct debit ext flag
        if (directDebit == "1") SkipFields(ref i, 10);

        var beneficiaryName = i < f.Length ? Next() : string.Empty;
        var address1 = i < f.Length ? Next() : string.Empty;
        var address2 = i < f.Length ? Next() : string.Empty;

        var primary = accounts.Count > 0 ? accounts[0] : new BankAccount();
        return new PaymentRequest
        {
            InvoiceId = string.IsNullOrEmpty(invoiceId) ? null : invoiceId,
            Amount = decimal.TryParse(amountStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var am) ? am : null,
            CurrencyCode = string.IsNullOrEmpty(currency) ? "EUR" : currency,
            DueDate = DateOnly.TryParseExact(dueStr, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dd) ? dd : null,
            VariableSymbol = Empty(vs),
            ConstantSymbol = Empty(cs),
            SpecificSymbol = Empty(ss),
            OriginatorReference = Empty(originator),
            Note = Empty(note),
            Iban = primary.Iban,
            Bic = Empty(primary.Bic),
            AlternativeAccounts = accounts.Skip(1).ToList(),
            BeneficiaryName = Empty(beneficiaryName),
            BeneficiaryAddressLine1 = Empty(address1),
            BeneficiaryAddressLine2 = Empty(address2),
        };

        static void SkipFields(ref int idx, int count) => idx += count;
        static string? Empty(string? s) => string.IsNullOrEmpty(s) ? null : s;
    }
}
