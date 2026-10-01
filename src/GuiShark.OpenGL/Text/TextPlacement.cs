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
        return element.TextBounds.Y + (element.IsButton
            ? Math.Max(0, (element.TextBounds.Height - lines * element.Style.LineHeight) / 2) : 0);
    }

    public static void Draw(ITextCanvas canvas, TextImage image, UiRect bounds, UiRect uv,
        TextDrawRequest request, TextRenderOptions options)
    {
        if (request.Element.Style.TextShadow is { } shadow)
            Shadow(canvas, image, bounds, uv, shadow, request, options);
        canvas.Draw(image, bounds, uv, Color(request.Element), request.Opacity, options.Sampling);
    }

    private static UiColor Color(UiElement element) => element.TextInput?.IsPlaceholder == true ? element.Style.Color with { A = element.Style.Color.A * .55f } : element.Style.Color;

    private static void Shadow(ITextCanvas canvas, TextImage image, UiRect bounds, UiRect uv,
        TextShadow shadow, TextDrawRequest request, TextRenderOptions options)
    {
        var x = bounds.X + Snap(shadow.X, request.ScaleX, options.PixelSnap);
        var y = bounds.Y + Snap(shadow.Y, request.ScaleY, options.PixelSnap);
        canvas.Draw(image, bounds with { X = x, Y = y }, uv, shadow.Color, request.Opacity, options.Sampling);
    }
}
