namespace PayBySquare.Core.Qr;

/// <summary>
/// Builds the <see cref="ComposedImage"/> for the standard PAY by square presentation: the QR code
/// inside a blue rounded frame, with the "PAY by square" wordmark and a small card icon beneath it.
/// The wordmark comes from pre-baked vector outlines (no runtime font dependency), so PNG and SVG
/// are identical and self-contained.
/// </summary>
public static class QrComposer
{
    public const string BrandBlue = "#5B9BD5";
    private const string CaptionGray = "#8C8C8C";

    public static ComposedImage Compose(bool[][] modules, int moduleSize, QrOptions options)
    {
        int n = modules.Length;
        float ms = moduleSize;
        float qrPx = n * ms;

        float gap = MathF.Round(ms * 1.5f);
        float stroke = MathF.Max(3f, MathF.Round(qrPx * 0.018f));
        float pad = MathF.Round(ms * 1.5f) + stroke;
        float corner = MathF.Round(qrPx * 0.05f);

        float x0 = pad, y0 = pad;
        float x1 = x0 + qrPx + 2 * gap;
        float y1 = y0 + qrPx + 2 * gap;
        var moduleOrigin = new Pt(x0 + gap, y0 + gap);

        // Plain code (no frame): just the QR + its quiet zone, cropped square.
        if (!options.Frame)
        {
            return new ComposedImage
            {
                Width = (int)qrPx,
                Height = (int)qrPx,
                Background = options.LightColor,
                Modules = modules,
                ModuleSize = ms,
                ModuleOrigin = new Pt(0, 0),
                ModuleColor = options.DarkColor,
            };
        }

        string brand = options.BrandColor;

        // Caption row sizing (a right-aligned group: [ wordmark ] [ gap ] [ icon ]).
        float captionTop = y1 + MathF.Round(ms * 1.5f);
        float iconSize = MathF.Round(qrPx * 0.15f);
        float font = MathF.Max(10f, MathF.Round(qrPx * 0.085f));
        float gapTI = MathF.Round(ms * 1.4f);

        float scaleW = font / Wordmark.ReferenceSize;
        float textW = (Wordmark.Bold.Advance + Wordmark.Regular.Advance) * scaleW;
        float available = (x1 - x0) - iconSize - gapTI;
        if (textW > available && textW > 0)
        {
            font *= available / textW;
            scaleW = font / Wordmark.ReferenceSize;
            textW = (Wordmark.Bold.Advance + Wordmark.Regular.Advance) * scaleW;
        }

        float rowH = MathF.Max(font * 1.25f, iconSize);
        int width = (int)MathF.Round(x1 + pad);
        int height = (int)MathF.Round(captionTop + rowH + pad);

        var image = new ComposedImage
        {
            Width = width,
            Height = height,
            Background = options.LightColor,
            Modules = modules,
            ModuleSize = ms,
            ModuleOrigin = moduleOrigin,
            ModuleColor = options.DarkColor,
        };

        // Closed rounded frame as a filled ring (between an outer and an inner rounded rectangle).
        float w = x1 - x0, h = y1 - y0, hs = stroke / 2f;
        var outer = RoundedRect(x0 - hs, y0 - hs, w + stroke, h + stroke, corner + hs);
        var inner = RoundedRect(x0 + hs, y0 + hs, w - stroke, h - stroke, MathF.Max(0, corner - hs));
        image.Fills.Add(new FillShape(new[] { outer, inner }, brand));

        // Right-aligned caption group.
        float iconX = x1 - iconSize;
        float textX = iconX - gapTI - textW;
        float iconY = captionTop + (rowH - iconSize) / 2f;

        var (minY, maxY) = Wordmark.VerticalExtent();
        float textTop = captionTop + (rowH - (maxY - minY) * scaleW) / 2f;
        float ty = textTop - minY * scaleW;

        image.Fills.Add(new FillShape(TransformRun(Wordmark.Bold, scaleW, textX, ty), brand));
        image.Fills.Add(new FillShape(TransformRun(Wordmark.Regular, scaleW, textX + Wordmark.Bold.Advance * scaleW, ty), CaptionGray));

        AddCardIcon(image, iconX, iconY, iconSize, brand, options.LightColor);
        return image;
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

    // Inverted card icon: a brand-coloured rounded tile with a light card and brand details on it.
    private static void AddCardIcon(ComposedImage img, float x, float y, float s, string brand, string light)
    {
        img.Fills.Add(new FillShape(new[] { RoundedRect(x, y, s, s, s * 0.18f) }, brand));

        float cw = s * 0.66f, ch = s * 0.46f;
        float cx = x + (s - cw) / 2f, cy = y + (s - ch) / 2f;
        img.Fills.Add(new FillShape(new[] { RoundedRect(cx, cy, cw, ch, ch * 0.18f) }, light));      // light card
        img.Fills.Add(new FillShape(new[] { Rect(cx, cy + ch * 0.16f, cw, ch * 0.22f) }, brand));    // brand stripe
        img.Fills.Add(new FillShape(new[] { RoundedRect(cx + cw * 0.12f, cy + ch * 0.62f, cw * 0.46f, ch * 0.12f, ch * 0.06f) }, brand)); // line
    }

    // ---- geometry helpers ----

    private static Pt[] Rect(float x, float y, float w, float h) =>
        new Pt[] { new(x, y), new(x + w, y), new(x + w, y + h), new(x, y + h) };

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
}
