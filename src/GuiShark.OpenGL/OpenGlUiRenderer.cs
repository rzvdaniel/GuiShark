namespace GuiShark.OpenGL;

/// <summary>Draws one view into the currently bound framebuffer. Create, render and dispose on its GL thread.</summary>
public sealed class OpenGlUiRenderer : IDisposable
{
    private readonly UiView view;
    private readonly QuadPainter painter;
    private readonly TextTextureCache text;
    private readonly ImageTextureCache images;
    private readonly ImagePainter imagePainter;
    private bool disposed;

    public OpenGlUiRenderer(UiView view, FontBook fonts)
    {
        this.view = view;
        using var state = new GlStateScope();
        painter = new();
        imagePainter = new(painter);
        text = new(fonts);
        images = new(view.Document.Assets);
    }

    public void Render(int framebufferWidth, int framebufferHeight)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        view.Update();
        if (framebufferWidth <= 0 || framebufferHeight <= 0 || view.Width <= 0 || view.Height <= 0) return;
        using var state = new GlStateScope();
        painter.Begin(view.Width, view.Height, framebufferWidth, framebufferHeight);
        Paint(view.Document.Root, 1, Math.Max(framebufferWidth / view.Width, framebufferHeight / view.Height));
    }

    private void Paint(UiElement element, float parentOpacity, float scale)
    {
        if (element.Style.Hidden || element.Clip.Width <= 0 || element.Clip.Height <= 0) return;
        var opacity = parentOpacity * element.Style.Opacity;
        painter.Clip(element.Clip);
        painter.Shape(element.Bounds, element.Style, opacity);
        PaintBackground(element, opacity);
        var content = element.ContentBounds;
        painter.Clip(element.Clip.Intersect(content));
        if (element.ImageSource != null && content.Width > 0 && content.Height > 0)
            imagePainter.Draw(content, images.Get(element.ImageSource), element.Style.ObjectFit,
                element.Style.ImageTint, opacity, element.Style.Radius, element.Style.ObjectFlip);
        if (element.Text.Length > 0 && content.Width > 0 && content.Height > 0)
            PaintText(element, content, opacity, scale);
        foreach (var child in element.Children) Paint(child, opacity, scale);
    }

    private void PaintText(UiElement element, UiRect content, float opacity, float scale)
    {
        var texture = text.Get(element, scale);
        var height = texture.Height / scale;
        var y = content.Y + (element.IsButton ? Math.Max(0, (content.Height - height) / 2) : 0);
        var bounds = new UiRect(content.X, y, content.Width, height);
        if (element.Style.TextShadow is { } shadow)
            painter.Texture(bounds with { X = bounds.X + shadow.X, Y = bounds.Y + shadow.Y }, texture, shadow.Color, opacity);
        painter.Texture(bounds, texture, element.Style.Color, opacity);
    }

    private void PaintBackground(UiElement element, float opacity)
    {
        var style = element.Style;
        if (style.BackgroundImage == null) return;
        var texture = images.Get(style.BackgroundImage);
        if (style.BackgroundSlice.Horizontal > 0 || style.BackgroundSlice.Vertical > 0)
            imagePainter.Frame(element.Bounds, texture, style.BackgroundSlice, style.BackgroundSliceWidth ?? style.BackgroundSlice, style.ImageTint, opacity);
        else
            imagePainter.Draw(element.Bounds, texture, style.BackgroundSize, style.ImageTint, opacity, style.Radius);
    }

    public void Dispose()
    {
        if (disposed) return;
        text.Dispose();
        images.Dispose();
        painter.Dispose();
        disposed = true;
    }
}
