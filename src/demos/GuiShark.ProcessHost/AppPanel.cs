using GuiShark.AppProtocol;
using GuiShark.OpenGL;
using OpenTK.Graphics.OpenGL4;

namespace GuiShark.ProcessHost;

// The GL context and all rendering remain owned by the host thread.
internal sealed class AppPanel : IDisposable
{
    private readonly FontBook fonts;
    private readonly UiView view;
    private readonly OpenGlUiRenderer renderer;
    private readonly Action<AppMessage> send;
    private int framebuffer;
    private int texture;
    private int pixelWidth;
    private int pixelHeight;

    public AppPanel(AppPackage package, Action<AppMessage> send)
    {
        this.send = send;
        fonts = new FontBook(Path.Combine(package.AssetsPath, "fonts/Lato-Regular.ttf"), Path.Combine(package.AssetsPath, "fonts/Lato-Bold.ttf"));
        var assets = new DirectoryAssetSource(package.AssetsPath);
        var document = HtmlLoader.Load(assets.ReadText(package.Document), assets);
        view = new UiView(document, fonts);
        renderer = new OpenGlUiRenderer(view, fonts);
        document.GetElement("increment").Clicked += _ => this.send(new AppMessage("click", "increment"));
    }

    public string Count => view.Document.GetElement("count").Text;
    public UiRect ButtonBounds { get { view.Update(); return view.Document.GetElement("increment").Bounds; } }

    public void SetText(string id, string value)
    {
        if (id.Length > 128 || value.Length > 8192) return;
        try { view.Document.GetElement(id).Text = value; }
        catch (KeyNotFoundException) { Console.Error.WriteLine($"Unknown app element: {id}"); }
    }

    public void Render(PanelViewport bounds, int screenHeight)
    {
        if (!bounds.IsValid) return;
        view.Resize(bounds.Bounds.Width, bounds.Bounds.Height);
        EnsureTarget(bounds.PixelWidth, bounds.PixelHeight);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
        GL.Viewport(0, 0, pixelWidth, pixelHeight);
        GL.ClearColor(.08f, .24f, .38f, 1);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        renderer.Render(pixelWidth, pixelHeight);
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, framebuffer);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        GL.BlitFramebuffer(0, 0, pixelWidth, pixelHeight, bounds.Left, screenHeight - bounds.Bottom,
            bounds.Right, screenHeight - bounds.Top, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void PointerMove(float x, float y) => view.Input.PointerMove(x, y);
    public void PointerDown(float x, float y) => view.Input.PointerDown(x, y);
    public void PointerUp(float x, float y) => view.Input.PointerUp(x, y);

    private void EnsureTarget(int width, int height)
    {
        if (framebuffer != 0 && width == pixelWidth && height == pixelHeight) return;
        DeleteTarget();
        pixelWidth = width;
        pixelHeight = height;
        texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, texture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        framebuffer = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texture, 0);
        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            throw new InvalidOperationException("The app panel framebuffer is incomplete.");
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    private void DeleteTarget()
    {
        if (framebuffer != 0) GL.DeleteFramebuffer(framebuffer);
        if (texture != 0) GL.DeleteTexture(texture);
        framebuffer = 0;
        texture = 0;
    }

    public void Dispose()
    {
        DeleteTarget();
        renderer.Dispose();
        view.Dispose();
        fonts.Dispose();
    }
}
