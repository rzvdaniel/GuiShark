namespace GuiShark.OpenGL;

/// <summary>Draws one view into the currently bound framebuffer. Create, render and dispose on its GL thread.</summary>
public sealed class OpenGlUiRenderer : IDisposable
{
    private readonly UiView view;
    private readonly QuadPainter painter;
    private readonly TextTextureCache text;
    private readonly ImageTextureCache images;
    private bool disposed;

    public OpenGlUiRenderer(UiView view, FontBook fonts)
    {
        this.view = view;
        using var state = new GlStateScope();
        painter = new();
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
        var content = element.ContentBounds;
        painter.Clip(element.Clip.Intersect(content));
        if (element.ImageSource != null && content.Width > 0 && content.Height > 0)
            painter.Texture(content, images.Get(element.ImageSource), new(1, 1, 1), opacity, element.Style.Radius);
        if (element.Text.Length > 0 && content.Width > 0 && content.Height > 0)
        {
            var texture = text.Get(element, scale);
            var height = texture.Height / scale;
            var y = content.Y + (element.IsButton ? Math.Max(0, (content.Height - height) / 2) : 0);
            painter.Texture(new(content.X, y, content.Width, height), texture, element.Style.Color, opacity);
        }
        foreach (var child in element.Children) Paint(child, opacity, scale);
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
