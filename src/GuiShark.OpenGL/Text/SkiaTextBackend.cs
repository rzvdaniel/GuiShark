using System.Runtime.InteropServices;
using SkiaSharp;

namespace GuiShark.OpenGL;

/// <summary>Whole-label grayscale rasterization. Borrows FontBook; dispose it after this backend.</summary>
public sealed class SkiaTextBackend(FontBook fonts, bool shaping = true) : ITextBackend
{
    private readonly FontRunRenderer runs = new(fonts, shaping);
    private int fontRevision = fonts.Revision;
    private readonly record struct Key(string Text, float Width, float Size, bool Bold, string Family, TextAlignment Align, float Offset, bool Editable, float OffsetY, float Height, bool Multiline);
    private readonly Dictionary<Key, TextImage> images = new();
    private readonly Dictionary<(string Text, float Size, bool Bold, string Family), TextCaretMap> caretMaps = new();
    private float scale = 1;
    private TextRenderOptions options = new();
    public TextBackendInfo Info { get; } = new(shaping ? "Skia + HarfBuzz" : "Skia pixel aligned",
        shaping ? "HarfBuzz shaping with local font fallback; device-size grayscale bitmap" : "Local font fallback; grayscale bitmap at device size", true, true);
    public TextCacheStatistics Cache => new(images.Count, images.Values.Sum(i => (long)i.Pixels.Length));

    public bool Configure(float rasterScale, TextRenderOptions next)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rasterScale);
        if (scale == rasterScale && options == next) return false;
        scale = rasterScale;
        options = next;
        images.Clear();
        caretMaps.Clear();
        return true;
    }

    public float MeasureWidth(string text, float size, bool bold) => MeasureWidth(text, size, bold, "");
    public float MeasureWidth(string text, float size, bool bold, string family)
    {
        SynchronizeFonts();
        return runs.Measure(text, new(size * scale, bold, family, options)) / scale;
    }

    public TextCaretMap CreateCaretMap(string text, float fontSize, bool bold, string family)
    {
        SynchronizeFonts();
        var key = (text, fontSize, bold, family);
        if (caretMaps.TryGetValue(key, out var map)) return map;
        map = runs.Carets(text, new(fontSize * scale, bold, family, options), scale);
        if (caretMaps.Count >= 128) caretMaps.Remove(caretMaps.First().Key);
        caretMaps[key] = map;
        return map;
    }
    private void SynchronizeFonts()
    {
        if (fontRevision == fonts.Revision) return;
        fontRevision = fonts.Revision;
        images.Clear();
        caretMaps.Clear();
    }

    public void Draw(TextDrawRequest request, ITextCanvas canvas)
    {
        var element = request.Element;
        SynchronizeFonts();
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
        var runStyle = new FontRunStyle(style.FontSize * unit, style.Bold, style.FontFamily, options);
        using var paint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        var baseline = (style.LineHeight * unit - (font.Metrics.Descent - font.Metrics.Ascent)) / 2 - font.Metrics.Ascent
            + (element.TextInput?.IsMultiline == true ? (element.TextBounds.Y - element.ContentBounds.Y) * unit : 0);
        foreach (var line in lines)
        {
            var x = TextPlacement.Align(width * unit, runs.Measure(line, runStyle), style.TextAlign)
                + (element.TextInput != null ? (element.TextBounds.X - element.ContentBounds.X) * unit : 0);
            runs.Draw(canvas, line, options.PixelSnap ? MathF.Round(x) : x,
                options.PixelSnap ? MathF.Round(baseline) : baseline, runStyle, paint);
            baseline += style.LineHeight * unit;
        }
        canvas.Flush();
        var result = new TextImage(bitmap.Width, bitmap.Height);
        Marshal.Copy(bitmap.GetPixels(), result.Pixels, 0, result.Pixels.Length);
        return result;
    }

    public void Dispose() { images.Clear(); caretMaps.Clear(); runs.Dispose(); }
}
