using System.Globalization;
using SkiaSharp;

namespace GuiShark.OpenGL;

internal readonly record struct FontRunStyle(float Size, bool Bold, string Family, TextRenderOptions Options);

/// <summary>Uses identical font runs for measurement, painting and editing coordinates.</summary>
internal sealed class FontRunRenderer(FontBook fonts, bool shaping) : IDisposable
{
    private readonly SkiaTextShaper? shaper = shaping ? new() : null;
    private readonly BidiFontRuns bidi = new(fonts);
    private float Width(string text, SKFont font, bool rtl) => shaper?.Measure(text, font, rtl) ?? font.MeasureText(text);

    public float Measure(TextLineContext context, FontRunStyle style)
    {
        var width = 0f;
        foreach (var run in bidi.Resolve(context, style))
        {
            using var font = FontBook.CreateFont(run.Font, style.Size, style.Options);
            width += Width(run.Text, font, run.RightToLeft);
        }
        return width;
    }

    public void Draw(SKCanvas canvas, TextLineContext context, float x, float baseline, FontRunStyle style, SKPaint paint)
    {
        foreach (var run in bidi.Resolve(context, style))
        {
            using var font = FontBook.CreateFont(run.Font, style.Size, style.Options);
            if (shaper != null) shaper.Draw(canvas, run.Text, x, baseline, font, paint, run.RightToLeft);
            else canvas.DrawText(UnshapedVisual(run), x, baseline, SKTextAlign.Left, font, paint);
            x += Width(run.Text, font, run.RightToLeft);
        }
    }

    public TextCaretMap Carets(TextLineContext context, FontRunStyle style, float scale)
    {
        var text = context.Line;
        var boundaries = StringInfo.ParseCombiningCharacters(text).Append(text.Length).ToArray();
        var leading = new float[boundaries.Length - 1];
        var trailing = new float[leading.Length];
        var pen = 0f;
        foreach (var run in bidi.Resolve(context, style))
        {
            using var font = FontBook.CreateFont(run.Font, style.Size, style.Options);
            var local = shaper?.Carets(run.Text, font, scale, run.RightToLeft) ?? UnshapedCarets(run, font, scale);
            var offsets = StringInfo.ParseCombiningCharacters(run.Text).Append(run.Text.Length).ToArray();
            for (var i = 0; i < offsets.Length - 1; i++)
            {
                var cell = Array.BinarySearch(boundaries, run.Start + offsets[i]);
                leading[cell] = pen + local.X(offsets[i]);
                trailing[cell] = pen + local.X(new TextCaretPosition(offsets[i + 1], true));
            }
            pen += Width(run.Text, font, run.RightToLeft) / scale;
        }
        return new(text, leading, trailing, bidi.IsRightToLeft(context));
    }

    private static string UnshapedVisual(VisualFontRun run)
    {
        if (!run.RightToLeft) return run.Text;
        var starts = StringInfo.ParseCombiningCharacters(run.Text).Append(run.Text.Length).ToArray();
        return string.Concat(Enumerable.Range(0, starts.Length - 1).Reverse().Select(i => run.Text[starts[i]..starts[i + 1]]));
    }
    private static TextCaretMap UnshapedCarets(VisualFontRun run, SKFont font, float scale)
    {
        var width = font.MeasureText(run.Text);
        return TextCaretMap.Measure(run.Text, prefix => (run.RightToLeft ? width - font.MeasureText(prefix) : font.MeasureText(prefix)) / scale);
    }

    public void Dispose() { shaper?.Dispose(); bidi.Clear(); }
}
