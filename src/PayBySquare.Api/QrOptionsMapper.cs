using PayBySquare.Core.Qr;

namespace PayBySquare.Api;

/// <summary>A render parameter the caller sent that the PAY by square standard does not permit.</summary>
internal sealed record RenderProblem(string Field, string Detail);

internal static class QrOptionsMapper
{
    /// <summary>
    /// Turns the query parameters into <see cref="QrOptions"/>, rejecting anything the standard does
    /// not allow rather than quietly rendering a non-conforming code.
    /// </summary>
    public static bool TryBuild(int? size, int? ppm, string? logo, string? brandColor,
        out QrOptions options, out RenderProblem? problem)
    {
        options = new QrOptions();
        problem = null;

        if (!TryParseLogo(logo, out var style, out var logoError))
        {
            problem = new RenderProblem("logo", logoError!);
            return false;
        }
        options.Logo = style;

        if (!PayBySquareStandard.TryParseBrandColor(brandColor, out var brand, out var brandError))
        {
            problem = new RenderProblem("brandcolor", brandError!);
            return false;
        }
        options.BrandColor = brand;

        if (size is { } s && s > 0) options.TargetSize = Math.Clamp(s, 32, 4096);
        if (ppm is { } p && p > 0) options.PixelsPerModule = Math.Clamp(p, 1, 100);
        return true;
    }

    /// <summary>Accepts the two lock-up names, plus "true" as the legacy spelling of the framed one.</summary>
    private static bool TryParseLogo(string? logo, out LogoStyle style, out string? error)
    {
        style = LogoStyle.Print;
        error = null;
        if (string.IsNullOrWhiteSpace(logo)) return true;

        switch (logo.Trim().ToLowerInvariant())
        {
            case "print":
            case "frame":
            case "true":
            case "1":
                style = LogoStyle.Print;
                return true;
            case "electronic":
            case "screen":
            case "digital":
                style = LogoStyle.Electronic;
                return true;
            case "none":
            case "false":
            case "0":
                error = "A bare code without the logo is not produced: specification 3.2 makes the " +
                        "logo mandatory alongside the code. Use 'print' (default) or 'electronic'.";
                return false;
            default:
                error = $"'{logo}' is not a known logo style. Use 'print' (the framed logo, default) " +
                        "or 'electronic' (the on-screen lock-up under a bare code).";
                return false;
        }
    }
}
