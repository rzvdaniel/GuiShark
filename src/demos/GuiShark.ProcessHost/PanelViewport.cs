using OpenTK.Mathematics;

namespace GuiShark.ProcessHost;

internal readonly record struct PanelViewport(UiRect Bounds, int Left, int Top, int Right, int Bottom)
{
    public int PixelWidth => Right - Left;
    public int PixelHeight => Bottom - Top;
    public bool IsValid => Bounds.Width > 0 && Bounds.Height > 0 && PixelWidth > 0 && PixelHeight > 0;

    public static PanelViewport Measure(UiView view, Vector2i client, Vector2i framebuffer)
    {
        view.Update();
        var bounds = view.Document.GetElement("app-panel").ContentBounds;
        if (client.X <= 0 || client.Y <= 0) return default;
        var x = (float)framebuffer.X / client.X;
        var y = (float)framebuffer.Y / client.Y;
        return new(bounds, (int)MathF.Round(bounds.X * x), (int)MathF.Round(bounds.Y * y),
            (int)MathF.Round(bounds.Right * x), (int)MathF.Round(bounds.Bottom * y));
    }

    public bool Contains(float x, float y) =>
        x >= Bounds.X && y >= Bounds.Y && x < Bounds.Right && y < Bounds.Bottom;
}
