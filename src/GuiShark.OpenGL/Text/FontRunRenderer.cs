using System.Globalization;
using SkiaSharp;

namespace GuiShark.OpenGL;

internal readonly record struct FontRunStyle(float Size, bool Bold, string Family, TextRenderOptions Options);

/// <summary>Uses identical font runs for measurement, painting and editing coordinates.</summary>
internal sealed class FontRunRenderer(FontBook fonts, bool shaping) : IDisposable
{
    private readonly SkiaTextShaper? shaper = shaping ? new() : null;
    private float Width(string text, SKFont font) => shaper?.Measure(text, font) ?? font.MeasureText(text);

    public float Measure(string text, FontRunStyle style)
    {
        var width = 0f;
        foreach (var run in fonts.Runs(text, style.Family, style.Bold))
        {
            using var font = FontBook.CreateFont(run.Font, style.Size, style.Options);
            width += Width(run.Text, font);
        }
        return width;
    }

    public void Draw(SKCanvas canvas, string text, float x, float baseline, FontRunStyle style, SKPaint paint)
    {
        foreach (var run in fonts.Runs(text, style.Family, style.Bold))
        {
            using var font = FontBook.CreateFont(run.Font, style.Size, style.Options);
            if (shaper != null) shaper.Draw(canvas, run.Text, x, baseline, font, paint);
            else canvas.DrawText(run.Text, x, baseline, SKTextAlign.Left, font, paint);
            x += Width(run.Text, font);
        }
    }

    public TextCaretMap Carets(string text, FontRunStyle style, float scale)
    {
        var boundaries = StringInfo.ParseCombiningCharacters(text).Append(text.Length).ToArray();
        var coordinates = new float[boundaries.Length];
        var pen = 0f;
        foreach (var run in fonts.Runs(text, style.Family, style.Bold))
        {
            using var font = FontBook.CreateFont(run.Font, style.Size, style.Options);
            var local = shaper?.Carets(run.Text, font, scale)
                ?? TextCaretMap.Measure(run.Text, prefix => font.MeasureText(prefix) / scale);
            foreach (var offset in StringInfo.ParseCombiningCharacters(run.Text).Append(run.Text.Length))
                coordinates[Array.BinarySearch(boundaries, run.Start + offset)] = pen + local.X(offset);
            pen += Width(run.Text, font) / scale;
        }
        return new(text, coordinates);
    }

    public void Dispose() => shaper?.Dispose();
}
