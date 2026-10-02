using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace GuiShark.OpenGL;

internal sealed class SkiaTextShaper : IDisposable
{
    private readonly Dictionary<SKTypeface, SKShaper> shapers = new();
    private SKShaper Get(SKFont font)
    {
        var face = font.Typeface;
        if (!shapers.TryGetValue(face, out var shaper)) shapers[face] = shaper = new(face);
        return shaper;
    }
    private static HarfBuzzSharp.Buffer Buffer(string text, bool rtl)
    {
        var buffer = new HarfBuzzSharp.Buffer();
        try
        {
            buffer.AddUtf16(text);
            buffer.GuessSegmentProperties();
            buffer.Direction = rtl ? HarfBuzzSharp.Direction.RightToLeft : HarfBuzzSharp.Direction.LeftToRight;
            return buffer;
        }
        catch { buffer.Dispose(); throw; }
    }
    public float Measure(string text, SKFont font, bool rtl)
    {
        if (text.Length == 0) return 0;
        using var buffer = Buffer(text, rtl);
        return Get(font).Shape(buffer, font).Width;
    }
    public TextCaretMap Carets(string text, SKFont font, float scale, bool rtl)
    {
        if (text.Length == 0) return new("", [0]);
        using var buffer = Buffer(text, rtl);
        var result = Get(font).Shape(buffer, font);
        return ShapedCaretMap.Create(text, buffer, result.Width / scale);
    }
    public void Draw(SKCanvas canvas, string text, float x, float y, SKFont font, SKPaint paint, bool rtl)
    {
        if (text.Length == 0) return;
        using var buffer = Buffer(text, rtl);
        var result = Get(font).Shape(buffer, font);
        using var builder = new SKTextBlobBuilder();
        builder.AddPositionedRun(result.Codepoints.Select(glyph => checked((ushort)glyph)).ToArray(), font, result.Points);
        using var blob = builder.Build();
        canvas.DrawText(blob, x, y, paint);
    }
    public void Dispose()
    {
        foreach (var shaper in shapers.Values) shaper.Dispose();
        shapers.Clear();
    }
}
