using System.Text;

namespace GuiShark.OpenGL;

/// <summary>Hinted grayscale glyph atlas using the packaged FreeType native library. Borrows FontBook.</summary>
public sealed class FreeTypeTextBackend : ITextBackend
{
    private readonly FontBook fonts;
    private readonly Dictionary<FontAsset, FreeTypeFace> faces = new();
    private readonly Dictionary<(FontAsset Font, int Size, int Rune), Glyph> glyphs = new();
    private readonly GlyphAtlas atlas = new();
    private float scale = 1;
    private TextRenderOptions options = new();
    public TextBackendInfo Info { get; } = new("FreeType glyph atlas",
        "Hinted grayscale glyphs at integer device sizes; shared atlas", true, true);
    public TextCacheStatistics Cache => new(glyphs.Count, atlas.Bytes);

    public FreeTypeTextBackend(FontBook fonts)
    {
        this.fonts = fonts;
        // Resolve native availability now so a host can report an unavailable backend honestly.
        var regular = fonts.Resolve("", false);
        faces.Add(regular, new(regular.Bytes));
    }

    public bool Configure(float rasterScale, TextRenderOptions next)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rasterScale);
        if (scale == rasterScale && options == next) return false;
        if (scale != rasterScale || options.Hinting != next.Hinting) { glyphs.Clear(); atlas.Clear(); }
        scale = rasterScale;
        options = next;
        return true;
    }

    private Glyph Get(Rune rune, float size, bool bold, string family)
    {
        var font = fonts.Resolve(family, bold);
        var pixels = Math.Max(1, (int)MathF.Round(size * scale));
        var key = (font, pixels, rune.Value);
        if (glyphs.TryGetValue(key, out var cached)) return cached;
        if (!faces.TryGetValue(font, out var face)) faces[font] = face = new(font.Bytes);
        var raster = face.Rasterize(rune, pixels, options.Hinting);
        TextImage? image = null;
        UiRect source = default;
        if (raster.Width > 0 && raster.Height > 0) (image, source) = atlas.Add(raster.Coverage, raster.Width, raster.Height);
        return glyphs[key] = new(raster with { Coverage = [] }, image, source);
    }

    public float MeasureWidth(string text, float size, bool bold) => MeasureWidth(text, size, bold, "");
    public float MeasureWidth(string text, float size, bool bold, string family)
    {
        var width = 0f;
        foreach (var rune in text.EnumerateRunes()) width += Get(rune, size, bold, family).Raster.Advance / scale;
        return width;
    }

    public void Draw(TextDrawRequest request, ITextCanvas canvas)
    {
        var element = request.Element;
        var style = element.Style;
        var lines = TextLayout.Wrap(element.Text, element.ContentBounds.Width, style, this);
        var draws = new List<TextGlyphDraw>();
        var metrics = Get(new Rune('M'), style.FontSize, style.Bold, style.FontFamily).Raster;
        var baseline = TextPlacement.Top(request, lines.Count)
            + (style.LineHeight - (metrics.Ascender - metrics.Descender) / scale) / 2 + metrics.Ascender / scale;
        foreach (var line in lines)
        {
            var x = element.ContentBounds.X + TextPlacement.Align(element.ContentBounds.Width,
                MeasureWidth(line, style.FontSize, style.Bold, style.FontFamily), style.TextAlign);
            x = TextPlacement.Snap(x, request.ScaleX, options.PixelSnap);
            var y = TextPlacement.Snap(baseline, request.ScaleY, options.PixelSnap);
            foreach (var rune in line.EnumerateRunes())
            {
                var glyph = Get(rune, style.FontSize, style.Bold, style.FontFamily);
                var raster = glyph.Raster;
                if (glyph.Image != null)
                    draws.Add(new(glyph.Image, new(x + raster.Left / scale, y - raster.Top / scale,
                        raster.Width / scale, raster.Height / scale), glyph.Source));
                x += raster.Advance / scale;
            }
            baseline += style.LineHeight;
        }
        TextPlacement.DrawGlyphs(canvas, draws, request, options);
    }

    public void Dispose()
    {
        foreach (var face in faces.Values) face.Dispose();
        faces.Clear(); glyphs.Clear(); atlas.Clear();
    }

    private sealed record Glyph(RasterGlyph Raster, TextImage? Image, UiRect Source);
}
