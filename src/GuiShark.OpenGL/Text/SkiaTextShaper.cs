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
    public float Measure(string text, SKFont font) => text.Length == 0 ? 0 : Get(font).Shape(text, font).Width;
    public TextCaretMap Carets(string text, SKFont font, float scale)
    {
        if (text.Length == 0) return new("", [0]);
        using var buffer = new HarfBuzzSharp.Buffer();
        buffer.AddUtf16(text);
        buffer.GuessSegmentProperties();
        var result = Get(font).Shape(buffer, font);
        return ShapedCaretMap.Create(text, buffer, result.Width / scale);
    }
    public void Draw(SKCanvas canvas, string text, float x, float y, SKFont font, SKPaint paint) =>
        canvas.DrawShapedText(Get(font), text, x, y, SKTextAlign.Left, font, paint);
    public void Dispose()
    {
        foreach (var shaper in shapers.Values) shaper.Dispose();
        shapers.Clear();
    }
}
