using System.Buffers.Binary;
using PayBySquare.Core.Qr;
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RenderPng_ProducesValidPng(bool frame)
    {
        var png = QrRenderer.RenderPng(Payload, new QrOptions { Frame = frame, TargetSize = 400 });
        Assert.True(png.Length > 100);
        Assert.Equal(PngSignature, png[..8]);
    }

    [Fact]
    public void RenderSvg_IsSelfContained_WithFrameAndWordmark()
    {
        var svg = QrRenderer.RenderSvg(Payload, new QrOptions { Frame = true });
        Assert.StartsWith("<svg", svg);
        Assert.Contains("fill-rule=\"evenodd\"", svg);  // frame ring / vectorised wordmark / icon
        Assert.Contains("<path", svg);
        Assert.DoesNotContain("font-family", svg);      // text is outlined, not font-dependent
    }

    [Fact]
    public void Frame_MakesImageTallerThanWide()
    {
        var (w, h) = ReadPngSize(QrRenderer.RenderPng(Payload, new QrOptions { Frame = true, TargetSize = 512 }));
        Assert.True(h > w);
    }

    [Fact]
    public void PlainCode_IsSquare()
    {
        var (w, h) = ReadPngSize(QrRenderer.RenderPng(Payload, new QrOptions { Frame = false, PixelsPerModule = 6 }));
        Assert.Equal(w, h);
    }
}
