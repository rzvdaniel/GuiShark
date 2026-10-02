using OpenTK.Mathematics;

namespace GuiShark.ThreadedHost;

internal readonly record struct PanelViewport(UiRect Bounds, int Left, int Top, int Right, int Bottom)
{
    public int PixelWidth => Right - Left;
    public int PixelHeight => Bottom - Top;
    public AppSize AppSize => new(Bounds.Width, Bounds.Height, PixelWidth, PixelHeight);
    public bool IsValid => Bounds.Width > 0 && Bounds.Height > 0 && PixelWidth > 0 && PixelHeight > 0;

    public static PanelViewport Measure(UiView view, Vector2i client, Vector2i framebuffer)
    {
        view.Update();
        var bounds = view.Document.GetElement("viewport").ContentBounds;
        if (client.X <= 0 || client.Y <= 0) return default;
        var scaleX = (float)framebuffer.X / client.X;
        var scaleY = (float)framebuffer.Y / client.Y;
        return new(bounds,
            (int)MathF.Round(bounds.X * scaleX),
            (int)MathF.Round(bounds.Y * scaleY),
            (int)MathF.Round((bounds.X + bounds.Width) * scaleX),
            (int)MathF.Round((bounds.Y + bounds.Height) * scaleY));
    }

    public bool Contains(float x, float y) =>
        x >= Bounds.X && y >= Bounds.Y && x < Bounds.X + Bounds.Width && y < Bounds.Y + Bounds.Height;
}
