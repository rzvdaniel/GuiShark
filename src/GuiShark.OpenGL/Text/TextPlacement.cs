namespace GuiShark.OpenGL;

internal static class TextPlacement
{
    public static float Snap(float value, float scale, bool enabled) => enabled ? MathF.Round(value * scale) / scale : value;
    public static float Align(float width, float advance, TextAlignment alignment) => alignment switch
    {
        TextAlignment.Center => (width - advance) / 2,
        TextAlignment.Right => width - advance,
        _ => 0
    };

    public static float Top(TextDrawRequest request, int lines)
    {
        var element = request.Element;
        return element.ContentBounds.Y + (element.IsButton
            ? Math.Max(0, (element.ContentBounds.Height - lines * element.Style.LineHeight) / 2) : 0);
    }

    public static void Draw(ITextCanvas canvas, TextImage image, UiRect bounds, UiRect uv,
        TextDrawRequest request, TextRenderOptions options, float range = 0)
    {
        if (request.Element.Style.TextShadow is { } shadow)
            Shadow(canvas, image, bounds, uv, shadow, request, options, range);
        canvas.Draw(image, bounds, uv, request.Element.Style.Color, request.Opacity, options.Sampling, range);
    }

    public static void DrawGlyphs(ITextCanvas canvas, IReadOnlyList<TextGlyphDraw> glyphs,
        TextDrawRequest request, TextRenderOptions options, float range = 0)
    {
        // Paint the whole shadow layer first so an overlapping glyph's shadow cannot darken earlier ink.
        if (request.Element.Style.TextShadow is { } shadow)
            foreach (var glyph in glyphs)
                Shadow(canvas, glyph.Image, glyph.Bounds, glyph.Source, shadow, request, options, range);
        foreach (var glyph in glyphs)
            canvas.Draw(glyph.Image, glyph.Bounds, glyph.Source, request.Element.Style.Color,
                request.Opacity, options.Sampling, range);
    }

    private static void Shadow(ITextCanvas canvas, TextImage image, UiRect bounds, UiRect uv,
        TextShadow shadow, TextDrawRequest request, TextRenderOptions options, float range)
    {
        // Preserve fractional MSDF bearings; snap the displacement rather than each glyph's plane bounds.
        var x = bounds.X + Snap(shadow.X, request.ScaleX, options.PixelSnap);
        var y = bounds.Y + Snap(shadow.Y, request.ScaleY, options.PixelSnap);
        canvas.Draw(image, bounds with { X = x, Y = y }, uv, shadow.Color, request.Opacity, options.Sampling, range);
    }
}

internal readonly record struct TextGlyphDraw(TextImage Image, UiRect Bounds, UiRect Source);
