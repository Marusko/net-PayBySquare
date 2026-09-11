using System.Globalization;
using System.Text;
using QRCoder;

namespace PayBySquare.Core.Qr;

public enum QrImageFormat
{
    Png,
    Svg,
}

/// <summary>
/// What a caller may choose about the rendering. Everything the standard fixes — error correction
/// level L, the 4-module quiet zone, black modules on white, the presence of the logo — is not an
/// option: see <see cref="PayBySquareStandard"/>.
/// </summary>
public sealed class QrOptions
{
    /// <summary>Pixels per QR module. Ignored when <see cref="TargetSize"/> is set.</summary>
    public int PixelsPerModule { get; set; } = 8;

    /// <summary>Approximate target image edge length in pixels. Overrides <see cref="PixelsPerModule"/> when set.</summary>
    public int? TargetSize { get; set; }

    /// <summary>Which official lock-up to draw around the code.</summary>
    public LogoStyle Logo { get; set; } = LogoStyle.Print;

    /// <summary>
    /// Colour of the frame, wordmark and card icon. Must be one of
    /// <see cref="PayBySquareStandard.BrandColors"/>.
    /// </summary>
    public string BrandColor { get; set; } = QrComposer.BrandBlue;
}

/// <summary>
/// Renders a PAY by square payload string into a QR image. Both PNG and SVG are produced from the
/// same <see cref="ComposedImage"/>, so every endpoint and format returns the identical design.
/// Rendering is fully self-contained (no third-party imaging or font library).
/// </summary>
public static class QrRenderer
{
    private const int Supersample = 4;

    public static byte[] RenderPng(string payload, QrOptions options)
    {
        var c = Compose(payload, options);
        var canvas = new Canvas(c.Width, c.Height, Supersample, Rgb.Parse(c.Background));

        var moduleColor = Rgb.Parse(c.ModuleColor);
        foreach (var (x, y, w, h) in ModuleRuns(c))
            canvas.FillRect(x, y, w, h, moduleColor);

        foreach (var fill in c.Fills)
            canvas.FillPolygons(fill.Contours, Rgb.Parse(fill.Color));

        return PngEncoder.Encode(c.Width, c.Height, canvas.ToRgb());
    }

    public static string RenderSvg(string payload, QrOptions options)
    {
        var c = Compose(payload, options);
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture,
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{c.Width}\" height=\"{c.Height}\" viewBox=\"0 0 {c.Width} {c.Height}\">");
        sb.Append(CultureInfo.InvariantCulture, $"<rect width=\"{c.Width}\" height=\"{c.Height}\" fill=\"{c.Background}\"/>");

        // Modules as run-length rectangles (crisp edges).
        sb.Append(CultureInfo.InvariantCulture, $"<g fill=\"{c.ModuleColor}\" shape-rendering=\"crispEdges\">");
        foreach (var (x, y, w, h) in ModuleRuns(c))
            sb.Append(CultureInfo.InvariantCulture, $"<rect x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(w)}\" height=\"{F(h)}\"/>");
        sb.Append("</g>");

        foreach (var fill in c.Fills)
            sb.Append(CultureInfo.InvariantCulture,
                $"<path d=\"{PathData(fill.Contours)}\" fill=\"{fill.Color}\" fill-rule=\"evenodd\"/>");

        sb.Append("</svg>");
        return sb.ToString();
    }

    private static ComposedImage Compose(string payload, QrOptions options)
    {
        if (payload.Length > PayBySquareStandard.MaxSequenceLength)
            throw new ArgumentException(
                $"The encoded sequence is {payload.Length} characters; specification table 10 caps a " +
                $"PAY by square code at {PayBySquareStandard.MaxSequenceLength}.", nameof(payload));

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, PayBySquareStandard.EccLevel);
        var modules = ToMatrix(data);
        int moduleSize = ResolveModuleSize(modules.Length, options);
        return QrComposer.Compose(modules, moduleSize, options);
    }

    /// <summary>
    /// The matrix QRCoder hands back already carries the 4-module quiet zone the standard requires
    /// ("quiet area = 4 basic squares"), so it is kept in full — the quiet zone is part of the code.
    /// </summary>
    private static bool[][] ToMatrix(QRCodeData data)
    {
        var matrix = data.ModuleMatrix;
        int n = matrix.Count;
        var result = new bool[n][];
        for (int r = 0; r < n; r++)
        {
            result[r] = new bool[n];
            for (int col = 0; col < n; col++)
                result[r][col] = matrix[r][col];
        }
        return result;
    }

    private static int ResolveModuleSize(int modules, QrOptions options)
    {
        if (options.TargetSize is { } target && target > 0)
        {
            // Height per module, so the longest edge lands on the target: the print logo is
            // 1.034796 x 1.171893 of the code, the electronic one adds the icon's 0.171840, and both
            // carry a 1.5-module margin on each side.
            double perModule = options.Logo == LogoStyle.Electronic
                ? 1.171840 * modules + 3
                : 1.034796 * 1.171893 * modules + 3;
            return Math.Max(1, (int)Math.Round(target / perModule));
        }
        return Math.Clamp(options.PixelsPerModule, 1, 100);
    }

    private static IEnumerable<(float X, float Y, float W, float H)> ModuleRuns(ComposedImage c)
    {
        float ms = c.ModuleSize;
        for (int r = 0; r < c.Modules.Length; r++)
        {
            var row = c.Modules[r];
            int col = 0;
            while (col < row.Length)
            {
                if (!row[col]) { col++; continue; }
                int start = col;
                while (col < row.Length && row[col]) col++;
                yield return (c.ModuleOrigin.X + start * ms, c.ModuleOrigin.Y + r * ms, (col - start) * ms, ms);
            }
        }
    }

    private static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    private static string PathData(Pt[][] contours)
    {
        var sb = new StringBuilder();
        foreach (var contour in contours)
        {
            if (contour.Length == 0) continue;
            sb.Append(CultureInfo.InvariantCulture, $"M{F(contour[0].X)} {F(contour[0].Y)}");
            for (int i = 1; i < contour.Length; i++)
                sb.Append(CultureInfo.InvariantCulture, $"L{F(contour[i].X)} {F(contour[i].Y)}");
            sb.Append('Z');
        }
        return sb.ToString();
    }
}
