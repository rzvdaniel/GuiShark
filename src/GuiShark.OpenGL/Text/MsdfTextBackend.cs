using System.Text;

namespace GuiShark.OpenGL;

/// <summary>Scalable MSDF glyphs from prebuilt atlases. Supports the atlas family, without native rasterization.</summary>
public sealed class MsdfTextBackend(MsdfAtlas regular, MsdfAtlas bold, string family) : ITextBackend
{
    private TextRenderOptions options = new();
    public TextBackendInfo Info { get; } = new("MSDF scalable atlas",
        "Distance-field edges; linear sampling; no font hinting; prebuilt charset", false, false);
    public TextCacheStatistics Cache => new(regular.Glyphs.Count + bold.Glyphs.Count,
        regular.Image.Pixels.LongLength + bold.Image.Pixels.LongLength);

    public bool Configure(float rasterScale, TextRenderOptions next)
    {
        var effective = next with { Sampling = TextSampling.Linear, Hinting = TextHinting.None };
        if (options == effective) return false;
        options = effective;
        return true;
    }

    private MsdfAtlas Atlas(bool isBold, string requested)
    {
        if (requested.Length > 0 && !requested.Equals(family, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"MSDF atlas is for '{family}', not '{requested}'. Generate an atlas for that font.");
        return isBold ? bold : regular;
    }

    private static MsdfGlyph Glyph(MsdfAtlas atlas, Rune rune) => atlas.Glyphs.GetValueOrDefault(rune.Value) ?? atlas.Glyphs['?'];
    public float MeasureWidth(string text, float size, bool isBold) => MeasureWidth(text, size, isBold, "");
    public float MeasureWidth(string text, float size, bool isBold, string requested)
    {
        var atlas = Atlas(isBold, requested);
        var width = 0f;
        foreach (var rune in text.EnumerateRunes()) width += Glyph(atlas, rune).Advance * size;
        return width;
    }

    public void Draw(TextDrawRequest request, ITextCanvas canvas)
    {
        var element = request.Element;
        var style = element.Style;
        var atlas = Atlas(style.Bold, style.FontFamily);
        var lines = TextLayout.Lines(element, element.TextBounds.Width, this);
        var draws = new List<TextGlyphDraw>();
        var baseline = TextPlacement.Top(request, lines.Count)
            + (style.LineHeight - (atlas.Ascender - atlas.Descender) * style.FontSize) / 2 + atlas.Ascender * style.FontSize;
        foreach (var line in lines)
        {
            var x = element.TextBounds.X + TextPlacement.Align(element.TextBounds.Width,
                MeasureWidth(line, style.FontSize, style.Bold, style.FontFamily), style.TextAlign);
            x = TextPlacement.Snap(x, request.ScaleX, options.PixelSnap);
            var y = TextPlacement.Snap(baseline, request.ScaleY, options.PixelSnap);
            foreach (var rune in line.EnumerateRunes())
            {
                var glyph = Glyph(atlas, rune);
                var p = glyph.Plane;
                if (p.Width > 0)
                    draws.Add(new(atlas.Image, new(x + p.X * style.FontSize, y + p.Y * style.FontSize,
                        p.Width * style.FontSize, p.Height * style.FontSize), glyph.Source));
                x += glyph.Advance * style.FontSize;
            }
            baseline += style.LineHeight;
        }
        TextPlacement.DrawGlyphs(canvas, draws, request, options, atlas.Range);
    }

    public void Dispose() { }
}
