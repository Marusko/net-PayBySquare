using System.Numerics;

namespace PayBySquare.Core.Validation;

/// <summary>
/// IBAN validation via the ISO 13616 mod-97 check, plus per-country length checks.
/// Works for Slovak (SK, 24) and Czech (CZ, 24) accounts and every other IBAN country.
/// </summary>
public static class IbanValidator
{
    // Official IBAN lengths per country (ISO 13616 registry).
    private static readonly Dictionary<string, int> Lengths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AD"] = 24, ["AE"] = 23, ["AL"] = 28, ["AT"] = 20, ["AZ"] = 28, ["BA"] = 20,
        ["BE"] = 16, ["BG"] = 22, ["BH"] = 22, ["BR"] = 29, ["BY"] = 28, ["CH"] = 21,
        ["CR"] = 22, ["CY"] = 28, ["CZ"] = 24, ["DE"] = 22, ["DK"] = 18, ["DO"] = 28,
        ["EE"] = 20, ["EG"] = 29, ["ES"] = 24, ["FI"] = 18, ["FO"] = 18, ["FR"] = 27,
        ["GB"] = 22, ["GE"] = 22, ["GI"] = 23, ["GL"] = 18, ["GR"] = 27, ["GT"] = 28,
        ["HR"] = 21, ["HU"] = 28, ["IE"] = 22, ["IL"] = 23, ["IS"] = 26, ["IT"] = 27,
        ["JO"] = 30, ["KW"] = 30, ["KZ"] = 20, ["LB"] = 28, ["LC"] = 32, ["LI"] = 21,
        ["LT"] = 20, ["LU"] = 20, ["LV"] = 21, ["LY"] = 25, ["MC"] = 27, ["MD"] = 24,
        ["ME"] = 22, ["MK"] = 19, ["MR"] = 27, ["MT"] = 31, ["MU"] = 30, ["NL"] = 18,
        ["NO"] = 15, ["PK"] = 24, ["PL"] = 28, ["PS"] = 29, ["PT"] = 25, ["QA"] = 29,
        ["RO"] = 24, ["RS"] = 22, ["SA"] = 24, ["SE"] = 24, ["SI"] = 19, ["SK"] = 24,
        ["SM"] = 27, ["ST"] = 25, ["SV"] = 28, ["TN"] = 24, ["TR"] = 26, ["UA"] = 29,
        ["VA"] = 22, ["VG"] = 24, ["XK"] = 20,
    };

    /// <summary>Removes spaces and upper-cases an IBAN for storage / encoding.</summary>
    public static string Normalize(string? iban) =>
        (iban ?? string.Empty).Replace(" ", string.Empty).Trim().ToUpperInvariant();

    public static bool IsValid(string? iban) => Validate(iban, out _);

    /// <summary>
    /// Validates an IBAN. Returns false and an <paramref name="error"/> message on failure.
    /// </summary>
    public static bool Validate(string? iban, out string? error)
    {
        var value = Normalize(iban);
        if (value.Length == 0)
        {
            error = "IBAN is required.";
            return false;
        }
        if (value.Length < 15 || value.Length > 34)
        {
            error = "IBAN length must be between 15 and 34 characters.";
            return false;
        }
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            bool ok = i < 2 ? char.IsAsciiLetterUpper(c) : char.IsAsciiLetterOrDigit(c);
            if (!ok)
            {
                error = "IBAN contains invalid characters.";
                return false;
            }
        }

        var country = value[..2];
        if (Lengths.TryGetValue(country, out var expected) && value.Length != expected)
        {
            error = $"IBAN for country {country} must be {expected} characters long.";
            return false;
        }

        if (Mod97(value) != 1)
        {
            error = "IBAN check digits are invalid.";
            return false;
        }

        error = null;
        return true;
    }

    private static int Mod97(string iban)
    {
        // Move the first four chars to the end, then map letters to numbers (A=10 … Z=35).
        var rearranged = iban[4..] + iban[..4];
        var numeric = new System.Text.StringBuilder(rearranged.Length * 2);
        foreach (var c in rearranged)
        {
            if (char.IsAsciiDigit(c)) numeric.Append(c);
            else numeric.Append((c - 'A' + 10).ToString());
        }
        var number = BigInteger.Parse(numeric.ToString());
        return (int)(number % 97);
    }
}
