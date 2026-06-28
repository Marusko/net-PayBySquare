using PayBySquare.Core.Qr;
using QRCoder;

namespace PayBySquare.Api;

internal static class QrOptionsMapper
{
    public static QrOptions Build(string? ecc, int? size, int? ppm, bool? margin, string? dark, string? light,
        bool? logo = null, string? brandColor = null)
    {
        var options = new QrOptions
        {
            EccLevel = ParseEcc(ecc),
            QuietZone = margin ?? true,
            Frame = logo ?? true,
        };
        if (size is { } s && s > 0) options.TargetSize = Math.Clamp(s, 32, 4096);
        if (ppm is { } p && p > 0) options.PixelsPerModule = Math.Clamp(p, 1, 100);
        if (!string.IsNullOrWhiteSpace(dark)) options.DarkColor = Normalize(dark!);
        if (!string.IsNullOrWhiteSpace(light)) options.LightColor = Normalize(light!);
        if (!string.IsNullOrWhiteSpace(brandColor)) options.BrandColor = Normalize(brandColor!);
        return options;
    }

    public static QRCodeGenerator.ECCLevel ParseEcc(string? ecc) => (ecc?.Trim().ToUpperInvariant()) switch
    {
        "L" => QRCodeGenerator.ECCLevel.L,
        "Q" => QRCodeGenerator.ECCLevel.Q,
        "H" => QRCodeGenerator.ECCLevel.H,
        _ => QRCodeGenerator.ECCLevel.M,
    };

    private static string Normalize(string color)
    {
        var c = color.Trim();
        return c.StartsWith('#') ? c : "#" + c;
    }
}
