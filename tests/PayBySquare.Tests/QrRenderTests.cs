using System.Buffers.Binary;
using PayBySquare.Core.Qr;
using QRCoder;
using Xunit;

namespace PayBySquare.Tests;

public class QrRenderTests
{
    private const string Payload = "0006U000F0OSJJ2G9BRQ70DCPJK4BMPV0HEHLJNFT2LHNNVII3JPQ2Q13QET3O1KJJQOMPTCCVNFIFCF6FKT73HEDOENI6D84TNKUD2KRPJH6S2HRSU0TF80O3VP7VM1L8FDI8TPT95OEFNPHNNG0";

    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    private static (int Width, int Height) ReadPngSize(byte[] png)
    {
        Assert.Equal(PngSignature, png[..8]);
        // IHDR data starts at byte 16 (8 sig + 4 length + 4 "IHDR").
        int width = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4));
        int height = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4));
        return (width, height);
    }

    private static int MatrixSize()
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(Payload, PayBySquareStandard.EccLevel);
        return data.ModuleMatrix.Count;   // includes the 4-module quiet zone on each side
    }

    [Theory]
    [InlineData(LogoStyle.Print)]
    [InlineData(LogoStyle.Electronic)]
    public void RenderPng_ProducesValidPng(LogoStyle logo)
    {
        var png = QrRenderer.RenderPng(Payload, new QrOptions { Logo = logo, TargetSize = 400 });
        Assert.True(png.Length > 100);
        Assert.Equal(PngSignature, png[..8]);
    }

    [Fact]
    public void RenderSvg_IsSelfContained_WithFrameAndWordmark()
    {
        var svg = QrRenderer.RenderSvg(Payload, new QrOptions { Logo = LogoStyle.Print });
        Assert.StartsWith("<svg", svg);
        Assert.Contains("fill-rule=\"evenodd\"", svg);  // frame rule / vectorised wordmark / icon
        Assert.Contains("<path", svg);
        Assert.DoesNotContain("font-family", svg);      // text is outlined, not font-dependent
    }

    /// <summary>Every code carries a logo, so no rendering is ever a bare square.</summary>
    [Theory]
    [InlineData(LogoStyle.Print)]
    [InlineData(LogoStyle.Electronic)]
    public void Logo_MakesImageTallerThanWide(LogoStyle logo)
    {
        var (w, h) = ReadPngSize(QrRenderer.RenderPng(Payload, new QrOptions { Logo = logo, TargetSize = 512 }));
        Assert.True(h > w);
    }

    /// <summary>The print logo frames the code; the electronic one only hangs the lock-up beneath it.</summary>
    [Fact]
    public void ElectronicLogo_IsNarrowerAndShorterThanThePrintOne()
    {
        var options = new QrOptions { PixelsPerModule = 8 };
        var print = ReadPngSize(QrRenderer.RenderPng(Payload, options));
        options.Logo = LogoStyle.Electronic;
        var screen = ReadPngSize(QrRenderer.RenderPng(Payload, options));

        Assert.True(screen.Width < print.Width);
        Assert.True(screen.Height < print.Height);
    }

    /// <summary>The manual's proportions: a 30 mm code yields a 31.044 x 36.379 mm print logo.</summary>
    [Fact]
    public void PrintLogo_MatchesTheProportionsInTheManual()
    {
        const int ppm = 10;
        var (w, h) = ReadPngSize(QrRenderer.RenderPng(Payload, new QrOptions { PixelsPerModule = ppm }));

        float code = MatrixSize() * ppm;   // the code plus its 4-module quiet zone
        float pad = 2 * MathF.Round(ppm * 1.5f);

        Assert.Equal(code * 31.044f / 30f + pad, w, 1.0);
        Assert.Equal(code * 36.379f / 30f + pad, h, 1.0);
    }

    /// <summary>The electronic lock-up hangs the icon straight off the code, adding only its height.</summary>
    [Fact]
    public void ElectronicLogo_AddsOnlyTheIconBelowTheCode()
    {
        const int ppm = 10;
        var (w, h) = ReadPngSize(QrRenderer.RenderPng(Payload,
            new QrOptions { Logo = LogoStyle.Electronic, PixelsPerModule = ppm }));

        float code = MatrixSize() * ppm;
        float pad = 2 * MathF.Round(ppm * 1.5f);

        Assert.Equal(code + pad, w, 1.0);
        Assert.Equal(code * 1.171840f + pad, h, 1.0);
    }

    /// <summary>The quiet zone is part of the code and is never dropped.</summary>
    [Fact]
    public void Code_KeepsTheFourModuleQuietZone()
    {
        const int ppm = 4;
        var (w, _) = ReadPngSize(QrRenderer.RenderPng(Payload,
            new QrOptions { Logo = LogoStyle.Electronic, PixelsPerModule = ppm }));

        float pad = 2 * MathF.Round(ppm * 1.5f);
        Assert.Equal(MatrixSize() * ppm + pad, w, 1.0);
    }

    /// <summary>Specification table 11 fixes the encoding parameters at level L.</summary>
    [Fact]
    public void EccLevel_IsL()
    {
        Assert.Equal(QRCodeGenerator.ECCLevel.L, PayBySquareStandard.EccLevel);
    }

    [Fact]
    public void Payload_LongerThanTheStandardAllows_IsRejected()
    {
        var overlong = new string('A', PayBySquareStandard.MaxSequenceLength + 1);
        Assert.Throws<ArgumentException>(() => QrRenderer.RenderPng(overlong, new QrOptions()));
    }

    [Theory]
    [InlineData("#6FA4D7", "#6FA4D7")]
    [InlineData("6fa4d7", "#6FA4D7")]
    [InlineData("black", "#000000")]
    [InlineData("#A1C7E9", "#A1C7E9")]
    [InlineData(null, "#6FA4D7")]
    public void BrandColor_AcceptsThePermittedVariants(string? input, string expected)
    {
        Assert.True(PayBySquareStandard.TryParseBrandColor(input, out var color, out _));
        Assert.Equal(expected, color);
    }

    [Theory]
    [InlineData("#5B9BD5")]   // plausible, but not in the manual
    [InlineData("#FF0000")]
    [InlineData("#F5871F")]   // an INVOICE by square colour
    public void BrandColor_RejectsAnythingElse(string input)
    {
        Assert.False(PayBySquareStandard.TryParseBrandColor(input, out _, out var error));
        Assert.Contains("logo manual", error);
    }

    /// <summary>The all-black variant draws "by square" in black too; the rest keep the grey.</summary>
    [Theory]
    [InlineData("#6FA4D7", "#B2B4B9")]
    [InlineData("#A1C7E9", "#B2B4B9")]
    [InlineData("#5F6062", "#B2B4B9")]
    [InlineData("#000000", "#000000")]
    public void CaptionColor_FollowsTheManual(string brand, string expected)
    {
        Assert.Equal(expected, PayBySquareStandard.CaptionColorFor(brand));
    }

    /// <summary>
    /// The two lock-ups are separate artworks, not one scaled to two sizes. At the same code size
    /// the manual draws the electronic caption noticeably smaller than the print one — cap height
    /// 0.0471 of the code against 0.0581 — and hangs it high on the icon (centre 0.266 of the icon
    /// below its top) instead of centring it (0.535). Measured off pages 4, 9 and 10 of the manual.
    /// </summary>
    [Theory]
    [InlineData(LogoStyle.Print, 0.05810f, 0.19526f, 0.5352f)]
    [InlineData(LogoStyle.Electronic, 0.04713f, 0.17184f, 0.2663f)]
    public void CaptionSize_MatchesTheLockUpItBelongsTo(
        LogoStyle logo, float capOverCode, float iconOverCode, float capCentreBelowIconTop)
    {
        const int ppm = 20;
        var svg = QrRenderer.RenderSvg(Payload, new QrOptions { Logo = logo, PixelsPerModule = ppm });
        float code = MatrixSize() * ppm;

        // Fills run frame (print only), icon tile, card artwork, then the two wordmark halves, so
        // the tile is the first rounded shape and "PAY" is the second-to-last path.
        var paths = SvgPaths(svg);
        var tile = PathBounds(paths[logo == LogoStyle.Print ? 1 : 0]);
        var pay = PathBounds(paths[^2]);

        float icon = tile.MaxY - tile.MinY;
        Assert.Equal(iconOverCode, icon / code, 0.002);
        Assert.Equal(capOverCode, (pay.MaxY - pay.MinY) / code, 0.002);
        Assert.Equal(capCentreBelowIconTop, ((pay.MinY + pay.MaxY) / 2 - tile.MinY) / icon, 0.01);
    }

    private static List<string> SvgPaths(string svg)
    {
        var found = new List<string>();
        int at = 0;
        while ((at = svg.IndexOf("<path d=\"", at, StringComparison.Ordinal)) >= 0)
        {
            at += 9;
            found.Add(svg[at..svg.IndexOf('"', at)]);
        }
        return found;
    }

    private static (float MinY, float MaxY) PathBounds(string data)
    {
        float min = float.MaxValue, max = float.MinValue;
        var numbers = data.Split(new[] { 'M', 'L', 'Z', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < numbers.Length; i += 2)
        {
            float y = float.Parse(numbers[i], System.Globalization.CultureInfo.InvariantCulture);
            if (y < min) min = y;
            if (y > max) max = y;
        }
        return (min, max);
    }

    /// <summary>
    /// The baked wordmark is the manual's own artwork, so its metrics must still match what was
    /// measured off the logo manual: cap height 1000, "PAY" 2.320 cap widths, "by square" 6.499,
    /// ascender 1.070 and descender 0.273.
    /// </summary>
    [Fact]
    public void Wordmark_KeepsTheManualsMetrics()
    {
        var bold = Wordmark.Extent(Wordmark.Bold);
        var regular = Wordmark.Extent(Wordmark.Regular);
        float cap = bold.MaxY - bold.MinY;

        Assert.Equal(1000f, cap, 0.5);
        Assert.Equal(0f, bold.MaxY, 0.5);            // "PAY" sits on the baseline
        Assert.Equal(2.320f, bold.MaxX / cap, 0.005);
        Assert.Equal(6.499f, regular.MaxX / cap, 0.005);
        Assert.Equal(2.827f, Wordmark.Bold.Advance / cap, 0.005);
        Assert.Equal(1.070f, -regular.MinY / cap, 0.005);
        Assert.Equal(0.273f, regular.MaxY / cap, 0.005);
    }
}
