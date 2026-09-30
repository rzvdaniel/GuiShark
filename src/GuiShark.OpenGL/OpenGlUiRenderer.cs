namespace GuiShark.OpenGL;

/// <summary>Draws one view into the currently bound framebuffer. Create, render and dispose on its GL thread.</summary>
public sealed class OpenGlUiRenderer : IDisposable
{
    private readonly UiView view;
    private readonly QuadPainter painter;
    private readonly OpenGlTextCanvas text;
    private ITextBackend backend;
    private bool ownsBackend;
    private readonly ImageTextureCache images;
    private readonly ImagePainter imagePainter;
    private bool disposed;
    public TextRenderOptions TextOptions { get; set; } = new();
    public ITextBackend TextBackend => backend;
    public TextRenderStatistics TextStatistics => new(text.DrawCalls, text.Uploads, text.Bytes, backend.Cache);

    public OpenGlUiRenderer(UiView view, FontBook fonts) : this(view, CreateDefault(view, fonts), true) { }

    private static ITextBackend CreateDefault(UiView view, FontBook fonts)
    {
        fonts.Load(view.Document);
        return new SkiaTextBackend(fonts);
    }

    /// <summary>Backends are borrowed unless ownsTextBackend is true. Dispose on the GL thread.</summary>
    public OpenGlUiRenderer(UiView view, ITextBackend textBackend, bool ownsTextBackend = false)
    {
        this.view = view;
        backend = textBackend;
        ownsBackend = ownsTextBackend;
        view.SetTextMetrics(backend);
        using var state = new GlStateScope();
        painter = new();
        imagePainter = new(painter);
        text = new(painter);
        images = new(view.Document.Assets);
    }

    public void SetTextBackend(ITextBackend next, bool ownsTextBackend = false)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (ReferenceEquals(backend, next)) return;
        text.Dispose();
        if (ownsBackend) backend.Dispose();
        backend = next;
        ownsBackend = ownsTextBackend;
        view.SetTextMetrics(backend);
    }

    public void Render(int framebufferWidth, int framebufferHeight)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (framebufferWidth <= 0 || framebufferHeight <= 0 || view.Width <= 0 || view.Height <= 0) return;
        var scaleX = framebufferWidth / view.Width;
        var scaleY = framebufferHeight / view.Height;
        if (backend.Configure(Math.Max(scaleX, scaleY), TextOptions)) view.SetTextMetrics(backend);
        view.Update();
        using var state = new GlStateScope();
        painter.Begin(view.Width, view.Height, framebufferWidth, framebufferHeight);
        text.Begin();
        Paint(view.Document.Root, 1, scaleX, scaleY);
        text.End();
    }

    private void Paint(UiElement element, float parentOpacity, float scaleX, float scaleY)
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
            backend.Draw(new(element, opacity, scaleX, scaleY), text);
        foreach (var child in element.Children) Paint(child, opacity, scaleX, scaleY);
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
        if (ownsBackend) backend.Dispose();
        images.Dispose();
        painter.Dispose();
        disposed = true;
    }
}
