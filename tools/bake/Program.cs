// Dev-time tool: flattens the "PAY by square" wordmark outlines into a binary blob so the shipped
// Core project needs no font or path library at runtime. Input is wordmark-outlines.txt, traced
// from the artwork in the official logo manual — see the header of that file. Run once; output
// goes to Core/Assets.
using System.Globalization;
using System.Text;

const float Flatness = 1.0f;   // max deviation, in the source file's units (cap height = 1000)

var source = File.ReadAllLines("wordmark-outlines.txt");
int reference = 1000;
var runs = new List<(float Advance, List<List<(float X, float Y)>> Contours)>();

foreach (var line in source)
{
    var text = line.Trim();
    if (text.Length == 0 || text[0] == '#') continue;

    if (text.StartsWith("reference ", StringComparison.Ordinal))
    {
        reference = int.Parse(text[10..], CultureInfo.InvariantCulture);
    }
    else if (text.StartsWith("run ", StringComparison.Ordinal))
    {
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        runs.Add((float.Parse(parts[3], CultureInfo.InvariantCulture), new()));
    }
    else
    {
        runs[^1].Contours.Add(Flatten(text));
    }
}

var outPath = Path.Combine("..", "..", "src", "PayBySquare.Core", "Assets", "Wordmark.bin");
Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
using (var fs = File.Create(outPath))
using (var w = new BinaryWriter(fs))
{
    w.Write(reference);
    w.Write(runs.Count);
    foreach (var (advance, contours) in runs)
    {
        w.Write(advance);
        w.Write(contours.Count);
        foreach (var contour in contours)
        {
            w.Write(contour.Count);
            foreach (var (x, y) in contour) { w.Write(x); w.Write(y); }
        }
    }
}

Console.WriteLine($"Wrote {outPath}: runs={runs.Count}, " +
    $"adv=[{string.Join(",", runs.Select(r => r.Advance.ToString("0.#", CultureInfo.InvariantCulture)))}], " +
    $"contours=[{string.Join(",", runs.Select(r => r.Contours.Count))}], " +
    $"points=[{string.Join(",", runs.Select(r => r.Contours.Sum(c => c.Count)))}]");

// ---- one contour of SVG-style path data -> a polygon ----

static List<(float X, float Y)> Flatten(string path)
{
    var pts = new List<(float X, float Y)>();
    var numbers = new List<float>();
    var sb = new StringBuilder();
    char op = ' ';
    (float X, float Y) cursor = (0, 0);

    void Flush()
    {
        if (sb.Length > 0) { numbers.Add(float.Parse(sb.ToString(), CultureInfo.InvariantCulture)); sb.Clear(); }
        if (op == 'M' || op == 'L')
        {
            cursor = (numbers[0], numbers[1]);
            pts.Add(cursor);
        }
        else if (op == 'C')
        {
            Cubic(pts, cursor, (numbers[0], numbers[1]), (numbers[2], numbers[3]), (numbers[4], numbers[5]));
            cursor = (numbers[4], numbers[5]);
        }
        numbers.Clear();
    }

    foreach (char ch in path)
    {
        if (ch is 'M' or 'L' or 'C' or 'Z')
        {
            if (op != ' ') Flush();
            op = ch;
        }
        else if (ch is ' ' or ',')
        {
            if (sb.Length > 0) { numbers.Add(float.Parse(sb.ToString(), CultureInfo.InvariantCulture)); sb.Clear(); }
        }
        else sb.Append(ch);
    }
    if (op is 'M' or 'L' or 'C') Flush();
    return pts;
}

/// <summary>Subdivides a cubic until it is flat to within <see cref="Flatness"/>, endpoint included.</summary>
static void Cubic(List<(float X, float Y)> pts, (float X, float Y) a, (float X, float Y) c1,
    (float X, float Y) c2, (float X, float Y) b, int depth = 0)
{
    if (depth >= 16 || IsFlat(a, c1, c2, b))
    {
        pts.Add(b);
        return;
    }

    (float X, float Y) Mid((float X, float Y) p, (float X, float Y) q) => ((p.X + q.X) / 2, (p.Y + q.Y) / 2);
    var ab = Mid(a, c1);
    var bc = Mid(c1, c2);
    var cd = Mid(c2, b);
    var abc = Mid(ab, bc);
    var bcd = Mid(bc, cd);
    var mid = Mid(abc, bcd);

    Cubic(pts, a, ab, abc, mid, depth + 1);
    Cubic(pts, mid, bcd, cd, b, depth + 1);
}

static bool IsFlat((float X, float Y) a, (float X, float Y) c1, (float X, float Y) c2, (float X, float Y) b)
{
    // Distance of both control points from the chord, the usual cheap flatness test.
    float dx = b.X - a.X, dy = b.Y - a.Y;
    float d1 = MathF.Abs((c1.X - b.X) * dy - (c1.Y - b.Y) * dx);
    float d2 = MathF.Abs((c2.X - b.X) * dy - (c2.Y - b.Y) * dx);
    float sum = d1 + d2;
    return sum * sum <= Flatness * (dx * dx + dy * dy);
}
