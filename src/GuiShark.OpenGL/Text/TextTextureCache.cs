using SkiaSharp;

namespace GuiShark.OpenGL;

internal sealed class TextTextureCache(FontBook fonts) : IDisposable
{
    private readonly record struct Key(string Text, float Width, float Size, bool Bold, TextAlignment Align, float Scale);
    private readonly Dictionary<Key, GpuTexture> textures = new();

    public GpuTexture Get(UiElement element, float scale)
    {
        var style = element.Style;
        var width = element.ContentBounds.Width;
        var key = new Key(element.Text, width, style.FontSize, style.Bold, style.TextAlign, scale);
        if (textures.TryGetValue(key, out var cached)) return cached;
        // Bound memory when counters or labels change continually.
        if (textures.Count >= 128)
        {
            var oldest = textures.First();
            oldest.Value.Dispose();
            textures.Remove(oldest.Key);
        }
        var lines = TextLayout.Wrap(element.Text, width, style, fonts);
        using var bitmap = new SKBitmap(Math.Max(1, (int)Math.Ceiling(width * scale)),
            Math.Max(1, (int)Math.Ceiling(lines.Count * style.LineHeight * scale)), SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.Scale(scale);
        using var font = fonts.CreateFont(style.FontSize, style.Bold);
        using var paint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        var baseline = (style.LineHeight - (font.Metrics.Descent - font.Metrics.Ascent)) / 2 - font.Metrics.Ascent;
        foreach (var line in lines)
        {
            var free = width - font.MeasureText(line);
            var x = style.TextAlign switch { TextAlignment.Center => free / 2, TextAlignment.Right => free, _ => 0 };
            canvas.DrawText(line, x, baseline, SKTextAlign.Left, font, paint);
            baseline += style.LineHeight;
        }
        canvas.Flush();
        return textures[key] = new GpuTexture(bitmap);
    }

    public void Dispose() { foreach (var texture in textures.Values) texture.Dispose(); textures.Clear(); }
}
