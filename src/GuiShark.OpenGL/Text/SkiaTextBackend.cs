using System.Runtime.InteropServices;
using SkiaSharp;

namespace GuiShark.OpenGL;

/// <summary>Whole-label grayscale rasterization. Borrows FontBook; dispose it after this backend.</summary>
public sealed class SkiaTextBackend(FontBook fonts, bool shaping = true) : ITextBackend
{
    private readonly SkiaTextShaper? shaper = shaping ? new() : null;
    private readonly record struct Key(string Text, float Width, float Size, bool Bold, string Family, TextAlignment Align, float Offset, bool Editable, float OffsetY, float Height, bool Multiline);
    private readonly Dictionary<Key, TextImage> images = new();
    private float scale = 1;
    private TextRenderOptions options = new();
    public TextBackendInfo Info { get; } = new(shaping ? "Skia + HarfBuzz" : "Skia pixel aligned",
        shaping ? "Single-run HarfBuzz shaping; device-size grayscale bitmap" : "Grayscale bitmap at device size; exact texel mapping", true, true);
    public TextCacheStatistics Cache => new(images.Count, images.Values.Sum(i => (long)i.Pixels.Length));

    public bool Configure(float rasterScale, TextRenderOptions next)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rasterScale);
        if (scale == rasterScale && options == next) return false;
        scale = rasterScale;
        options = next;
        images.Clear();
        return true;
    }

    public float MeasureWidth(string text, float size, bool bold) => MeasureWidth(text, size, bold, "");
    public float MeasureWidth(string text, float size, bool bold, string family)
    {
        using var font = fonts.CreateFont(size * scale, bold, family, options);
        return MeasureLine(text, font) / scale;
    }

    private float MeasureLine(string text, SKFont font) => shaper?.Measure(text, font) ?? font.MeasureText(text);
    private void DrawLine(SKCanvas canvas, string line, float x, float baseline, SKFont font, SKPaint paint)
    {
        if (shaper != null) shaper.Draw(canvas, line, x, baseline, font, paint);
        else canvas.DrawText(line, x, baseline, SKTextAlign.Left, font, paint);
    }

    public void Draw(TextDrawRequest request, ITextCanvas canvas)
    {
        var element = request.Element;
        var key = CreateKey(element);
        if (!images.TryGetValue(key, out var image))
        {
            if (images.Count >= 128) images.Remove(images.First().Key);
            images[key] = image = Rasterize(element);
        }
        var top = TextPlacement.Top(request, TextLayout.Lines(element, key.Width, this).Count);
        var bounds = new UiRect(
            TextPlacement.Snap(element.TextInput != null ? element.ContentBounds.X : element.TextBounds.X, request.ScaleX, options.PixelSnap),
            TextPlacement.Snap(element.TextInput?.IsMultiline == true ? element.ContentBounds.Y : top, request.ScaleY, options.PixelSnap),
            image.Width / scale, image.Height / scale);
        TextPlacement.Draw(canvas, image, bounds, new(0, 0, 1, 1), request,
            options);
    }

    private static Key CreateKey(UiElement element)
    {
        var style = element.Style;
        var editable = element.TextInput != null;
        var multiline = element.TextInput?.IsMultiline == true;
        return new(element.Text, editable ? element.ContentBounds.Width : element.TextBounds.Width,
            style.FontSize, style.Bold, style.FontFamily, style.TextAlign,
            editable ? element.TextBounds.X - element.ContentBounds.X : 0, editable,
            multiline ? element.TextBounds.Y - element.ContentBounds.Y : 0,
            multiline ? element.ContentBounds.Height : 0, multiline);
    }

    private TextImage Rasterize(UiElement element)
    {
        var style = element.Style;
        var width = element.TextInput != null ? element.ContentBounds.Width : element.TextBounds.Width;
        var lines = TextLayout.Lines(element, width, this);
        using var bitmap = new SKBitmap(Math.Max(1, (int)Math.Ceiling(width * scale)),
            Math.Max(1, (int)Math.Ceiling((element.TextInput?.IsMultiline == true ? element.ContentBounds.Height : lines.Count * style.LineHeight) * scale)), SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        var unit = scale;
        using var font = fonts.CreateFont(style.FontSize * unit, style.Bold, style.FontFamily, options);
        using var paint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        var baseline = (style.LineHeight * unit - (font.Metrics.Descent - font.Metrics.Ascent)) / 2 - font.Metrics.Ascent
            + (element.TextInput?.IsMultiline == true ? (element.TextBounds.Y - element.ContentBounds.Y) * unit : 0);
        foreach (var line in lines)
        {
            var x = TextPlacement.Align(width * unit, MeasureLine(line, font), style.TextAlign)
                + (element.TextInput != null ? (element.TextBounds.X - element.ContentBounds.X) * unit : 0);
            DrawLine(canvas, line, options.PixelSnap ? MathF.Round(x) : x,
                options.PixelSnap ? MathF.Round(baseline) : baseline, font, paint);
            baseline += style.LineHeight * unit;
        }
        canvas.Flush();
        var result = new TextImage(bitmap.Width, bitmap.Height);
        Marshal.Copy(bitmap.GetPixels(), result.Pixels, 0, result.Pixels.Length);
        return result;
    }

    public void Dispose() { images.Clear(); shaper?.Dispose(); }
}
