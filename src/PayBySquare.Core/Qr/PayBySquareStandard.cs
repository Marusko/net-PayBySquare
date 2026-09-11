using QRCoder;

namespace PayBySquare.Core.Qr;

/// <summary>Which of the two official lock-ups from the logo manual to draw.</summary>
public enum LogoStyle
{
    /// <summary>"Basic logo for print devices": the code inside the bracket frame, caption beneath it.</summary>
    Print,

    /// <summary>"Basic logo for electronic devices": the caption lock-up alone, hung under the bare code.</summary>
    Electronic,
}

/// <summary>
/// The values the PAY by square standard fixes, so the renderer cannot produce a non-conforming code.
/// Sources: "PAY by square specifications 1.1.0" (SBA) and "PAY by square logo manual 1.0.4" (SBA).
/// </summary>
public static class PayBySquareStandard
{
    // ---- logo manual, "permitted color variations" (the PAY half of each page) ----

    /// <summary>#A1C7E9 — R:161 G:199 B:233.</summary>
    public const string Sky = "#A1C7E9";

    /// <summary>#6FA4D7 — R:111 G:164 B:215. The colour used by the basic logo.</summary>
    public const string Blue = "#6FA4D7";

    /// <summary>#5F6062 — R:95 G:96 B:98.</summary>
    public const string Grey = "#5F6062";

    /// <summary>#000000 — the black-and-white representation.</summary>
    public const string Black = "#000000";

    /// <summary>The grey of the "by square" half of the wordmark in the three coloured variants.</summary>
    public const string CaptionGrey = "#B2B4B9";

    /// <summary>Every code in the manual is black on white, and scanners rely on that contrast.</summary>
    public const string CodeDark = "#000000";

    /// <inheritdoc cref="CodeDark"/>
    public const string CodeLight = "#FFFFFF";

    /// <summary>The card icon's artwork is white in every permitted variant, including the black one.</summary>
    internal const string IconArtwork = "#FFFFFF";

    /// <summary>"Quiet area = 4 basic squares" (logo manual, "correct QR code position").</summary>
    public const int QuietZoneModules = 4;

    /// <summary>
    /// Specification table 11 lists the QR encoding parameters as "xL", so L is the level a PAY by
    /// square code is built at — a higher one grows the symbol past the version the standard's fixed
    /// physical size (table 10) assumes.
    /// </summary>
    public const QRCodeGenerator.ECCLevel EccLevel = QRCodeGenerator.ECCLevel.L;

    /// <summary>Specification table 10: the longest by square sequence a PAY code may carry.</summary>
    public const int MaxSequenceLength = 550;

    /// <summary>The four colours a PAY by square logo may be drawn in, in the manual's order.</summary>
    public static IReadOnlyList<string> BrandColors { get; } = new[] { Sky, Blue, Grey, Black };

    /// <summary>Friendly aliases for <see cref="BrandColors"/>, so callers need not paste hex.</summary>
    private static readonly Dictionary<string, string> BrandAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sky"] = Sky,
        ["light"] = Sky,
        ["blue"] = Blue,
        ["default"] = Blue,
        ["grey"] = Grey,
        ["gray"] = Grey,
        ["black"] = Black,
    };

    /// <summary>
    /// Accepts one of the permitted brand colours (hex, with or without the '#', or a friendly name)
    /// and returns it in canonical form.
    /// </summary>
    public static bool TryParseBrandColor(string? value, out string color, out string? error)
    {
        color = Blue;
        error = null;
        if (string.IsNullOrWhiteSpace(value)) return true;

        var raw = value.Trim();
        if (BrandAliases.TryGetValue(raw, out var alias)) { color = alias; return true; }

        var hex = "#" + raw.TrimStart('#').ToUpperInvariant();
        foreach (var permitted in BrandColors)
        {
            if (string.Equals(hex, permitted, StringComparison.Ordinal)) { color = permitted; return true; }
        }

        error = $"'{value}' is not a permitted PAY by square logo colour. The logo manual allows only " +
                $"{string.Join(", ", BrandColors)} (or the names sky, blue, grey, black).";
        return false;
    }

    /// <summary>
    /// The wordmark's "by square" half. It is the light grey in the three coloured variants, but the
    /// all-black variant is drawn entirely in black.
    /// </summary>
    public static string CaptionColorFor(string brandColor) =>
        string.Equals(brandColor, Black, StringComparison.OrdinalIgnoreCase) ? Black : CaptionGrey;
}
