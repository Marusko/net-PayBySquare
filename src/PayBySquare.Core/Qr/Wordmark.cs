namespace PayBySquare.Core.Qr;

/// <summary>
/// The pre-baked "PAY by square" wordmark outlines (see tools/bake). Loaded once from an embedded
/// binary blob so no font library is needed at runtime. Coordinates are at a reference em size;
/// callers scale them to the desired font size.
/// </summary>
public static class Wordmark
{
    public sealed record Run(float Advance, Pt[][] Contours);

    public static int ReferenceSize { get; }
    /// <summary>"PAY " (bold).</summary>
    public static Run Bold { get; }
    /// <summary>"by square" (regular).</summary>
    public static Run Regular { get; }

    static Wordmark()
    {
        var asm = typeof(Wordmark).Assembly;
        using var stream = asm.GetManifestResourceStream("PayBySquare.Core.Assets.Wordmark.bin")
            ?? throw new InvalidOperationException("Embedded resource 'Wordmark.bin' not found.");
        using var reader = new BinaryReader(stream);

        ReferenceSize = reader.ReadInt32();
        int runCount = reader.ReadInt32();
        var runs = new Run[runCount];
        for (int r = 0; r < runCount; r++)
        {
            float advance = reader.ReadSingle();
            int contourCount = reader.ReadInt32();
            var contours = new Pt[contourCount][];
            for (int c = 0; c < contourCount; c++)
            {
                int pointCount = reader.ReadInt32();
                var pts = new Pt[pointCount];
                for (int i = 0; i < pointCount; i++)
                    pts[i] = new Pt(reader.ReadSingle(), reader.ReadSingle());
                contours[c] = pts;
            }
            runs[r] = new Run(advance, contours);
        }
        Bold = runs[0];
        Regular = runs[1];
    }

    /// <summary>Vertical extent (min/max Y) of both runs combined, at reference size.</summary>
    public static (float Min, float Max) VerticalExtent()
    {
        float min = float.MaxValue, max = float.MinValue;
        foreach (var run in new[] { Bold, Regular })
            foreach (var contour in run.Contours)
                foreach (var p in contour)
                {
                    if (p.Y < min) min = p.Y;
                    if (p.Y > max) max = p.Y;
                }
        return (min, max);
    }
}
