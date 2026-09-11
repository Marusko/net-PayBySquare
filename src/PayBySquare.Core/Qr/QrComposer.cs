namespace PayBySquare.Core.Qr;

/// <summary>
/// Builds the <see cref="ComposedImage"/> for the two official PAY by square lock-ups.
///
/// Every proportion below was measured off the vector paths in "PAY by square logo manual 1.0.4"
/// (SBA): the print lock-up from the "basic logo for print devices" page, the electronic one from
/// the "basic logo for electronic devices" page cross-checked against the applied examples on the
/// colour-variation and rotation pages.
///
/// Both share one rule: the code plus its 4-module quiet zone is the unit everything else is
/// measured from — <c>Q</c> below. For the print logo the frame sits exactly on the quiet zone's
/// edge, which reproduces the proportions the manual states (a 30 mm code gives a 31.044 mm wide,
/// 36.379 mm tall logo).
///
/// The wordmark comes from pre-baked vector outlines (no runtime font dependency), so PNG and SVG
/// are identical and self-contained.
/// </summary>
public static class QrComposer
{
    /// <summary>The colour of the basic logo. See <see cref="PayBySquareStandard.BrandColors"/>.</summary>
    public const string BrandBlue = PayBySquareStandard.Blue;

    // ---- print lock-up, normalised to W (the frame's outer side) ----

    private const float FrameOuter = 1.034796f;      // W / Q
    private const float StrokeRatio = 0.016814f;     // rule thickness
    private const float CornerRatio = 0.015553f;     // outer corner radius (the inner corners are square)
    private const float BottomRunRatio = 0.745243f;  // where the bottom rule stops, from the left edge
    private const float RightRunRatio = 0.947322f;   // where the right rule stops, from the top edge
    private const float PrintIconRatio = 0.188719f;  // card icon tile side
    private const float PrintCapRatio = 0.056192f;   // wordmark cap height
    private const float PrintBaselineRatio = 1.112245f; // wordmark baseline, below the frame's top edge
    private const float PrintTextRightRatio = 0.745535f; // wordmark right edge, from the left edge
    private const float PrintTotalRatio = 1.171893f; // overall logo height

    // ---- electronic lock-up: the icon hangs off the code's bottom-right corner, so its own side
    //      (T) is the natural unit; only its size is tied to Q ----

    private const float ScreenIconRatio = 0.171840f;    // T / Q
    private const float ScreenCapRatio = 0.274250f;     // wordmark cap height / T
    private const float ScreenGapRatio = 0.247500f;     // wordmark-to-icon gap / T
    private const float ScreenBaselineRatio = 0.403400f; // wordmark baseline below the icon's top / T

    private const float IconCornerRatio = 0.182823f; // icon tile corner radius, as a fraction of its side

    public static ComposedImage Compose(bool[][] modules, int moduleSize, QrOptions options)
    {
        float ms = moduleSize;
        float q = modules.Length * ms;   // the matrix always carries its 4-module quiet zone
        float pad = MathF.Round(ms * 1.5f);

        return options.Logo == LogoStyle.Electronic
            ? ComposeElectronic(modules, ms, q, pad, options)
            : ComposePrint(modules, ms, q, pad, options);
    }

    private static ComposedImage ComposePrint(bool[][] modules, float ms, float q, float pad, QrOptions options)
    {
        float w = q * FrameOuter;
        float s = w * StrokeRatio;
        float h = w * PrintTotalRatio;
        float x0 = pad, y0 = pad;

        var image = new ComposedImage
        {
            Width = (int)MathF.Round(w + 2 * pad),
            Height = (int)MathF.Round(h + 2 * pad),
            Background = PayBySquareStandard.CodeLight,
            Modules = modules,
            ModuleSize = ms,
            ModuleOrigin = new Pt(x0 + s, y0 + s),
            ModuleColor = PayBySquareStandard.CodeDark,
        };

        image.Fills.Add(new FillShape(new[] { FramePath(x0, y0, w, s) }, options.BrandColor));

        // Caption row: the icon hangs off the bottom-right corner with its top on the frame's inner
        // edge and its right edge flush with the frame; the wordmark sits on a fixed baseline and is
        // right-aligned to where the bottom rule stops.
        float iconSize = w * PrintIconRatio;
        AddCardIcon(image, x0 + w - iconSize, y0 + w - s, iconSize, options.BrandColor);
        AddWordmark(image, x0 + w * PrintTextRightRatio, y0 + w * PrintBaselineRatio,
            w * PrintCapRatio, options.BrandColor);

        return image;
    }

    private static ComposedImage ComposeElectronic(bool[][] modules, float ms, float q, float pad, QrOptions options)
    {
        float t = q * ScreenIconRatio;
        float x0 = pad, y0 = pad;

        var image = new ComposedImage
        {
            Width = (int)MathF.Round(q + 2 * pad),
            Height = (int)MathF.Round(q + t + 2 * pad),
            Background = PayBySquareStandard.CodeLight,
            Modules = modules,
            ModuleSize = ms,
            ModuleOrigin = new Pt(x0, y0),
            ModuleColor = PayBySquareStandard.CodeDark,
        };

        // The lock-up hangs straight off the code: icon flush with the code's right edge, its top on
        // the code's bottom edge, the wordmark to its left on a baseline inside the icon.
        float iconX = x0 + q - t;
        AddCardIcon(image, iconX, y0 + q, t, options.BrandColor);
        AddWordmark(image, iconX - t * ScreenGapRatio, y0 + q + t * ScreenBaselineRatio,
            t * ScreenCapRatio, options.BrandColor);

        return image;
    }

    /// <summary>Draws "PAY by square" with its ink right edge at <paramref name="right"/> and caps <paramref name="capHeight"/> tall.</summary>
    private static void AddWordmark(ComposedImage img, float right, float baseline, float capHeight, string brand)
    {
        var bold = Wordmark.Extent(Wordmark.Bold);
        var regular = Wordmark.Extent(Wordmark.Regular);

        float scale = capHeight / (bold.MaxY - bold.MinY);
        float inkRight = MathF.Max(bold.MaxX, Wordmark.Bold.Advance + regular.MaxX) * scale;
        float x = right - inkRight;
        float y = baseline - bold.MaxY * scale;

        img.Fills.Add(new FillShape(TransformRun(Wordmark.Bold, scale, x, y), brand));
        img.Fills.Add(new FillShape(TransformRun(Wordmark.Regular, scale, x + Wordmark.Bold.Advance * scale, y),
            PayBySquareStandard.CaptionColorFor(brand)));
    }

    /// <summary>
    /// The frame: a square rule that is open at the bottom-right, drawn as one closed contour. The
    /// three joined corners carry a small radius, the two free ends are rounded off.
    /// </summary>
    private static Pt[] FramePath(float x0, float y0, float w, float s)
    {
        float r = w * CornerRatio;
        float hs = s / 2f;
        float x1 = x0 + w, y1 = y0 + w;
        float bottomTip = x0 + w * BottomRunRatio;   // free end of the bottom rule
        float rightTip = y0 + w * RightRunRatio;     // free end of the right rule

        var p = new List<Pt> { new(x1, y0 + r) };
        Arc(p, new Pt(x1 - r, y0 + r), r, 0f, -MathF.PI / 2f);                      // top-right
        p.Add(new Pt(x0 + r, y0));
        Arc(p, new Pt(x0 + r, y0 + r), r, -MathF.PI / 2f, -MathF.PI);               // top-left
        p.Add(new Pt(x0, y1 - r));
        Arc(p, new Pt(x0 + r, y1 - r), r, MathF.PI, MathF.PI / 2f);                 // bottom-left
        p.Add(new Pt(bottomTip - hs, y1));
        Arc(p, new Pt(bottomTip - hs, y1 - hs), hs, MathF.PI / 2f, -MathF.PI / 2f); // bottom rule cap
        p.Add(new Pt(x0 + s, y1 - s));
        p.Add(new Pt(x0 + s, y0 + s));
        p.Add(new Pt(x1 - s, y0 + s));
        p.Add(new Pt(x1 - s, rightTip - hs));
        Arc(p, new Pt(x1 - hs, rightTip - hs), hs, MathF.PI, 0f);                   // right rule cap
        return p.ToArray();
    }

    // The card icon, normalised to its tile: a tilted card outline (with its hole), the magnetic
    // stripe and the two short rules below it, straight from the manual's artwork. A run of six
    // numbers is a cubic curve (two control points and the end point), a run of two is a line.
    private static readonly float[][] CardOutline =
    {
        new[] { 0.80140f, 0.25765f },
        new[] { 0.14514f, 0.35272f },
        new[] { 0.11857f, 0.35636f, 0.10042f, 0.38448f, 0.10506f, 0.41530f },
        new[] { 0.15414f, 0.75456f },
        new[] { 0.15864f, 0.78535f, 0.18370f, 0.80702f, 0.21041f, 0.80306f },
        new[] { 0.86672f, 0.70799f },
        new[] { 0.89332f, 0.70417f, 0.91118f, 0.67634f, 0.90682f, 0.64567f },
        new[] { 0.85773f, 0.30618f },
        new[] { 0.85323f, 0.27551f, 0.82806f, 0.25387f, 0.80140f, 0.25765f },
    };

    private static readonly float[] CardHole =
        { 0.84592f, 0.65608f, 0.20191f, 0.75035f, 0.15583f, 0.40855f, 0.80028f, 0.31418f };

    private static readonly float[][] CardDetails =
    {
        new[] { 0.14593f, 0.47232f, 0.81926f, 0.37387f, 0.83262f, 0.46415f, 0.15929f, 0.56276f }, // stripe
        new[] { 0.54321f, 0.58779f, 0.23117f, 0.63351f, 0.22710f, 0.60579f, 0.53914f, 0.56021f },
        new[] { 0.55516f, 0.65138f, 0.24301f, 0.69678f, 0.23890f, 0.66909f, 0.55109f, 0.62351f },
    };

    private static void AddCardIcon(ComposedImage img, float x, float y, float size, string brand)
    {
        img.Fills.Add(new FillShape(
            new[] { RoundedRect(x, y, size, size, size * IconCornerRatio) }, brand));

        var outline = new List<Pt> { Map(CardOutline[0], 0, x, y, size) };
        for (int i = 1; i < CardOutline.Length; i++)
        {
            var seg = CardOutline[i];
            if (seg.Length == 2)
                outline.Add(Map(seg, 0, x, y, size));
            else
                Cubic(outline, outline[^1], Map(seg, 0, x, y, size), Map(seg, 2, x, y, size), Map(seg, 4, x, y, size));
        }

        const string artwork = PayBySquareStandard.IconArtwork;
        img.Fills.Add(new FillShape(new[] { outline.ToArray(), Polygon(CardHole, x, y, size) }, artwork));
        foreach (var detail in CardDetails)
            img.Fills.Add(new FillShape(new[] { Polygon(detail, x, y, size) }, artwork));
    }

    private static Pt Map(float[] a, int i, float x, float y, float size) =>
        new(x + a[i] * size, y + a[i + 1] * size);

    private static Pt[] Polygon(float[] a, float x, float y, float size)
    {
        var pts = new Pt[a.Length / 2];
        for (int i = 0; i < pts.Length; i++) pts[i] = Map(a, i * 2, x, y, size);
        return pts;
    }

    private static Pt[][] TransformRun(Wordmark.Run run, float scale, float tx, float ty)
    {
        var result = new Pt[run.Contours.Length][];
        for (int c = 0; c < run.Contours.Length; c++)
        {
            var src = run.Contours[c];
            var dst = new Pt[src.Length];
            for (int i = 0; i < src.Length; i++)
                dst[i] = new Pt(src[i].X * scale + tx, src[i].Y * scale + ty);
            result[c] = dst;
        }
        return result;
    }

    // ---- geometry helpers ----

    private static Pt[] RoundedRect(float x, float y, float w, float h, float r)
    {
        r = MathF.Min(r, MathF.Min(w, h) / 2f);
        var pts = new List<Pt> { new(x + r, y), new(x + w - r, y) };
        Quad(pts, new(x + w - r, y), new(x + w, y), new(x + w, y + r));
        pts.Add(new(x + w, y + h - r));
        Quad(pts, new(x + w, y + h - r), new(x + w, y + h), new(x + w - r, y + h));
        pts.Add(new(x + r, y + h));
        Quad(pts, new(x + r, y + h), new(x, y + h), new(x, y + h - r));
        pts.Add(new(x, y + r));
        Quad(pts, new(x, y + r), new(x, y), new(x + r, y));
        return pts.ToArray();
    }

    private static void Quad(List<Pt> pts, Pt a, Pt c, Pt b, int segments = 8)
    {
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments, u = 1 - t;
            pts.Add(new Pt(
                u * u * a.X + 2 * u * t * c.X + t * t * b.X,
                u * u * a.Y + 2 * u * t * c.Y + t * t * b.Y));
        }
    }

    private static void Cubic(List<Pt> pts, Pt a, Pt c1, Pt c2, Pt b, int segments = 8)
    {
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments, u = 1 - t;
            pts.Add(new Pt(
                u * u * u * a.X + 3 * u * u * t * c1.X + 3 * u * t * t * c2.X + t * t * t * b.X,
                u * u * u * a.Y + 3 * u * u * t * c1.Y + 3 * u * t * t * c2.Y + t * t * t * b.Y));
        }
    }

    /// <summary>Appends an arc around <paramref name="c"/>, sweeping from angle a0 to a1 (screen angles, y down).</summary>
    private static void Arc(List<Pt> pts, Pt c, float r, float a0, float a1, int segments = 8)
    {
        for (int i = 1; i <= segments; i++)
        {
            float a = a0 + (a1 - a0) * i / segments;
            pts.Add(new Pt(c.X + r * MathF.Cos(a), c.Y + r * MathF.Sin(a)));
        }
    }
}
