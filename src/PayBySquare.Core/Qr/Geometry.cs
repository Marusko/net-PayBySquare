namespace PayBySquare.Core.Qr;

/// <summary>A 2D point in image coordinates.</summary>
public readonly record struct Pt(float X, float Y);

/// <summary>An RGB colour parsed from a #RRGGBB string.</summary>
public readonly record struct Rgb(byte R, byte G, byte B)
{
    public static Rgb Parse(string hex)
    {
        var s = hex.TrimStart('#');
        if (s.Length == 3) s = string.Concat(s[0], s[0], s[1], s[1], s[2], s[2]);
        if (s.Length != 6) throw new FormatException($"Invalid colour '{hex}'. Expected #RRGGBB.");
        return new Rgb(
            Convert.ToByte(s.Substring(0, 2), 16),
            Convert.ToByte(s.Substring(2, 2), 16),
            Convert.ToByte(s.Substring(4, 2), 16));
    }
}

/// <summary>A solid-filled shape made of one or more contours (even-odd winding, so holes work).</summary>
public sealed record FillShape(Pt[][] Contours, string Color);

/// <summary>
/// A resolution-independent description of the framed PAY by square image. Both the PNG and SVG
/// renderers consume this same model, so the two outputs are identical.
/// </summary>
public sealed class ComposedImage
{
    public int Width { get; init; }
    public int Height { get; init; }
    public string Background { get; init; } = "#FFFFFF";

    public bool[][] Modules { get; init; } = Array.Empty<bool[]>();
    public float ModuleSize { get; init; }
    public Pt ModuleOrigin { get; init; }
    public string ModuleColor { get; init; } = "#000000";

    /// <summary>Filled shapes (frame ring, card icon, wordmark) drawn over the modules, in order.</summary>
    public List<FillShape> Fills { get; } = new();
}
