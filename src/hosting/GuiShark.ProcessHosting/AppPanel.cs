using GuiShark.AppProtocol;
using OpenTK.Graphics.OpenGL4;
using SkiaSharp;

namespace GuiShark.ProcessHosting;

// Displays the app-owned frame and forwards coordinates in the app's logical space.
public sealed class AppPanel : IDisposable
{
    private const int MaximumDimension = 8192;
    private const int MaximumPixels = 16 * 1024 * 1024;
    private int framebuffer;
    private int texture;
    private int pixelWidth;
    private int pixelHeight;
    private long latestSequence;
    private bool bottomUp;
    public bool HasFrame => latestSequence > 0 && texture != 0;

    public void Apply(AppMessage message)
    {
        if (message.Type != "frame" || message.Pixels is null || message.Sequence <= latestSequence) return;
        if (message.PixelWidth is <= 0 or > MaximumDimension || message.PixelHeight is <= 0 or > MaximumDimension
            || (long)message.PixelWidth * message.PixelHeight > MaximumPixels)
            throw new InvalidDataException("App frame dimensions are outside the supported limits.");

        var encoded = Convert.FromBase64String(message.Pixels);
        using var bitmap = SKBitmap.Decode(encoded) ?? throw new InvalidDataException("App frame was not a supported image.");
        if (bitmap.Width != message.PixelWidth || bitmap.Height != message.PixelHeight)
            throw new InvalidDataException("App frame image dimensions did not match its header.");
        using var rgba = bitmap.ColorType == SKColorType.Rgba8888
            ? bitmap.Copy()
            : bitmap.Copy(SKColorType.Rgba8888);
        if (rgba is null) throw new InvalidDataException("Could not decode app frame pixels.");

        EnsureTarget(rgba.Width, rgba.Height);
        GL.BindTexture(TextureTarget.Texture2D, texture);
        GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, pixelWidth, pixelHeight,
            PixelFormat.Rgba, PixelType.UnsignedByte, rgba.GetPixels());
        latestSequence = message.Sequence;
        bottomUp = false;
    }

    public bool Apply(AppMessage message, AppProcess source)
    {
        if (message.BufferSlot < 0) { Apply(message); return true; }
        if (message.Sequence <= latestSequence) { source.ReleaseFrame(message); return false; }
        var applied = source.ReadFrame(message, pointer =>
        {
            EnsureTarget(message.PixelWidth, message.PixelHeight);
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, pixelWidth, pixelHeight,
                PixelFormat.Rgba, PixelType.UnsignedByte, pointer);
        });
        if (applied) { latestSequence = message.Sequence; bottomUp = true; }
        return applied;
    }

    public void Render(PanelViewport bounds, int screenHeight)
    {
        if (!bounds.IsValid || framebuffer == 0) return;
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, framebuffer);
        GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        GL.BlitFramebuffer(0, bottomUp ? 0 : pixelHeight, pixelWidth, bottomUp ? pixelHeight : 0, bounds.Left, screenHeight - bounds.Bottom,
            bounds.Right, screenHeight - bounds.Top, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    private void EnsureTarget(int width, int height)
    {
        if (framebuffer != 0 && width == pixelWidth && height == pixelHeight) return;
        DeleteTarget();
        pixelWidth = width;
        pixelHeight = height;
        texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, texture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, width, height, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        framebuffer = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, texture, 0);
        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            throw new InvalidOperationException("The app frame framebuffer is incomplete.");
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    private void DeleteTarget()
    {
        if (framebuffer != 0) GL.DeleteFramebuffer(framebuffer);
        if (texture != 0) GL.DeleteTexture(texture);
        framebuffer = texture = 0;
    }

    public void Dispose() => DeleteTarget();
}
