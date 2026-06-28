// Dev-time tool: bakes the "PAY by square" wordmark glyph outlines into a binary blob so the
// shipped Core project needs no font library at runtime. Run once; output goes to Core/Assets.
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;

const int Reference = 100;

var collection = new FontCollection();
var regular = collection.Add("Lato-Regular.ttf").CreateFont(Reference, FontStyle.Regular);
var bold = collection.Add("Lato-Bold.ttf").CreateFont(Reference, FontStyle.Bold);

(float advance, List<float[]> contours) Run(string text, Font font)
{
    var options = new TextOptions(font) { Origin = new PointF(0, 0) };
    float advance = TextMeasurer.MeasureAdvance(text, options).Width;
    var glyphs = TextBuilder.GenerateGlyphs(text, options);
    var contours = new List<float[]>();
    foreach (var path in glyphs)
        foreach (var simple in path.Flatten())
        {
            var pts = simple.Points.ToArray();
            var flat = new float[pts.Length * 2];
            for (int i = 0; i < pts.Length; i++) { flat[i * 2] = pts[i].X; flat[i * 2 + 1] = pts[i].Y; }
            contours.Add(flat);
        }
    return (advance, contours);
}

var runs = new[] { Run("PAY ", bold), Run("by square", regular) };

var outPath = System.IO.Path.Combine("..", "..", "src", "PayBySquare.Core", "Assets", "Wordmark.bin");
Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outPath)!);
using var fs = File.Create(outPath);
using var w = new BinaryWriter(fs);
w.Write(Reference);
w.Write(runs.Length);
foreach (var (advance, contours) in runs)
{
    w.Write(advance);
    w.Write(contours.Count);
    foreach (var c in contours)
    {
        w.Write(c.Length / 2);
        foreach (var v in c) w.Write(v);
    }
}
Console.WriteLine($"Wrote {outPath}: runs={runs.Length}, " +
    $"adv=[{runs[0].advance:0.#},{runs[1].advance:0.#}], contours=[{runs[0].contours.Count},{runs[1].contours.Count}]");
