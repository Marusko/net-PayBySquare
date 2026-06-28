namespace PayBySquare.Core.Qr;

/// <summary>
/// A simple RGB raster surface. Everything is drawn at an integer supersample factor and then
/// box-downsampled, which antialiases the frame, card icon and wordmark while keeping the QR
/// modules perfectly crisp (their edges fall on supersample boundaries).
/// </summary>
public sealed class Canvas
{
    private readonly int _w;        // final width
    private readonly int _h;        // final height
    private readonly int _scale;
    private readonly int _sw;       // supersample width
    private readonly int _sh;       // supersample height
    private readonly byte[] _buf;   // RGB at supersample resolution

    public Canvas(int width, int height, int scale, Rgb background)
    {
        _w = width;
        _h = height;
        _scale = scale;
        _sw = width * scale;
        _sh = height * scale;
        _buf = new byte[_sw * _sh * 3];
        for (int i = 0; i < _buf.Length; i += 3)
        {
            _buf[i] = background.R;
            _buf[i + 1] = background.G;
            _buf[i + 2] = background.B;
        }
    }

    /// <summary>Fill an axis-aligned rectangle (final coordinates). Stays crisp after downsampling.</summary>
    public void FillRect(float x, float y, float w, float h, Rgb color)
    {
        int x0 = Math.Clamp((int)MathF.Round(x * _scale), 0, _sw);
        int y0 = Math.Clamp((int)MathF.Round(y * _scale), 0, _sh);
        int x1 = Math.Clamp((int)MathF.Round((x + w) * _scale), 0, _sw);
        int y1 = Math.Clamp((int)MathF.Round((y + h) * _scale), 0, _sh);
        for (int py = y0; py < y1; py++)
            SetSpan(py, x0, x1, color);
    }

    /// <summary>Fill one or more contours (final coordinates) using the even-odd rule.</summary>
    public void FillPolygons(Pt[][] contours, Rgb color)
    {
        if (contours.Length == 0) return;

        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (var c in contours)
            foreach (var p in c)
            {
                float sy = p.Y * _scale;
                if (sy < minY) minY = sy;
                if (sy > maxY) maxY = sy;
            }
        int yStart = Math.Clamp((int)MathF.Floor(minY), 0, _sh - 1);
        int yEnd = Math.Clamp((int)MathF.Ceiling(maxY), 0, _sh - 1);

        var crossings = new List<float>(16);
        for (int py = yStart; py <= yEnd; py++)
        {
            float yc = py + 0.5f;
            crossings.Clear();
            foreach (var contour in contours)
            {
                int n = contour.Length;
                for (int i = 0; i < n; i++)
                {
                    var a = contour[i];
                    var b = contour[(i + 1) % n];
                    float ay = a.Y * _scale, by = b.Y * _scale;
                    if ((ay <= yc && by > yc) || (by <= yc && ay > yc))
                    {
                        float t = (yc - ay) / (by - ay);
                        crossings.Add((a.X + t * (b.X - a.X)) * _scale);
                    }
                }
            }
            if (crossings.Count < 2) continue;
            crossings.Sort();
            for (int i = 0; i + 1 < crossings.Count; i += 2)
            {
                int xa = Math.Clamp((int)MathF.Round(crossings[i]), 0, _sw);
                int xb = Math.Clamp((int)MathF.Round(crossings[i + 1]), 0, _sw);
                SetSpan(py, xa, xb, color);
            }
        }
    }

    private void SetSpan(int py, int xa, int xb, Rgb color)
    {
        int row = py * _sw * 3;
        for (int px = xa; px < xb; px++)
        {
            int o = row + px * 3;
            _buf[o] = color.R;
            _buf[o + 1] = color.G;
            _buf[o + 2] = color.B;
        }
    }

    /// <summary>Box-downsample to the final resolution and return row-major RGB bytes.</summary>
    public byte[] ToRgb()
    {
        var outBuf = new byte[_w * _h * 3];
        int samples = _scale * _scale;
        for (int y = 0; y < _h; y++)
        {
            for (int x = 0; x < _w; x++)
            {
                int sumR = 0, sumG = 0, sumB = 0;
                int sy0 = y * _scale, sx0 = x * _scale;
                for (int sy = 0; sy < _scale; sy++)
                {
                    int row = (sy0 + sy) * _sw * 3;
                    for (int sx = 0; sx < _scale; sx++)
                    {
                        int o = row + (sx0 + sx) * 3;
                        sumR += _buf[o];
                        sumG += _buf[o + 1];
                        sumB += _buf[o + 2];
                    }
                }
                int d = (y * _w + x) * 3;
                outBuf[d] = (byte)(sumR / samples);
                outBuf[d + 1] = (byte)(sumG / samples);
                outBuf[d + 2] = (byte)(sumB / samples);
            }
        }
        return outBuf;
    }
}
