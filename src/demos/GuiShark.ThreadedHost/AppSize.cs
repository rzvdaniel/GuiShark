namespace GuiShark.ThreadedHost;

// Layout uses window coordinates; the texture uses actual framebuffer pixels.
internal readonly record struct AppSize(float LogicalWidth, float LogicalHeight, int PixelWidth, int PixelHeight);
