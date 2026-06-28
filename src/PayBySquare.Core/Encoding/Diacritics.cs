using System.Text;

namespace PayBySquare.Core.Encoding;

/// <summary>
/// Transliterates accented Latin characters to plain ASCII. Some bank apps render diacritics
/// poorly, so PAY by square payloads are commonly stripped to ASCII.
///
/// Uses an explicit character map rather than Unicode normalization so it works identically
/// in globalization-invariant mode and on minimal containers without ICU (e.g. Alpine).
/// Covers Latin-1 Supplement and Latin Extended-A, which includes every Slovak and Czech
/// letter (á ä č ď é í ĺ ľ ň ó ô ŕ š ť ú ý ž ě ř ů …) plus common European diacritics.
/// </summary>
public static class Diacritics
{
    private static readonly Dictionary<char, string> Map = Build();

    public static string Remove(string? input)
    {
        if (string.IsNullOrEmpty(input)) return input ?? string.Empty;

        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (ch < 0x80) sb.Append(ch);
            else if (Map.TryGetValue(ch, out var replacement)) sb.Append(replacement);
            else sb.Append(ch); // leave unknown characters intact
        }
        return sb.ToString();
    }

    private static Dictionary<char, string> Build()
    {
        var m = new Dictionary<char, string>();

        void Add(string from, string to)
        {
            foreach (var c in from) m[c] = to;
        }

        // Latin-1 Supplement + Latin Extended-A, grouped by base letter.
        Add("ÀÁÂÃÄÅĀĂĄ", "A");
        Add("àáâãäåāăą", "a");
        Add("Ç ĆĈĊČ".Replace(" ", ""), "C");
        Add("çćĉċč", "c");
        Add("ÐĎĐ", "D");
        Add("ðďđ", "d");
        Add("ÈÉÊËĒĔĖĘĚ", "E");
        Add("èéêëēĕėęě", "e");
        Add("ĜĞĠĢ", "G");
        Add("ĝğġģ", "g");
        Add("ĤĦ", "H");
        Add("ĥħ", "h");
        Add("ÌÍÎÏĨĪĬĮİ", "I");
        Add("ìíîïĩīĭįı", "i");
        Add("Ĵ", "J");
        Add("ĵ", "j");
        Add("Ķ", "K");
        Add("ķ", "k");
        Add("ĹĻĽĿŁ", "L");
        Add("ĺļľŀł", "l");
        Add("ÑŃŅŇ", "N");
        Add("ñńņňŉ", "n");
        Add("ÒÓÔÕÖØŌŎŐ", "O");
        Add("òóôõöøōŏő", "o");
        Add("ŔŖŘ", "R");
        Add("ŕŗř", "r");
        Add("ŚŜŞŠ", "S");
        Add("śŝşš", "s");
        Add("ŢŤŦ", "T");
        Add("ţťŧ", "t");
        Add("ÙÚÛÜŨŪŬŮŰŲ", "U");
        Add("ùúûüũūŭůűų", "u");
        Add("Ŵ", "W");
        Add("ŵ", "w");
        Add("ÝŸŶ", "Y");
        Add("ýÿŷ", "y");
        Add("ŹŻŽ", "Z");
        Add("źżž", "z");

        // Multi-character expansions.
        m['Æ'] = "AE"; m['æ'] = "ae";
        m['Œ'] = "OE"; m['œ'] = "oe";
        m['Ĳ'] = "IJ"; m['ĳ'] = "ij";
        m['ß'] = "ss";
        m['Þ'] = "TH"; m['þ'] = "th";

        return m;
    }
}
