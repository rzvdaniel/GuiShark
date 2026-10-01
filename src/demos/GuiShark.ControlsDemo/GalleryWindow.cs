using GuiShark.OpenGL;
using OpenTK.Graphics.OpenGL4;
using Silk.NET.SDL;

namespace GuiShark.ControlsDemo;

internal sealed class GalleryWindow : IDisposable
{
    private readonly SdlWindow host;
    private readonly SdlInput input;
    private readonly string assetsPath;
    private readonly GalleryOptions options;
    private FontBook fonts = null!;
    private UiView view = null!;
    private OpenGlUiRenderer renderer = null!;
    private GalleryController controller = null!;
    private int frames;
    private bool capture;
    private bool disposed;
    private Silk.NET.Maths.Vector2D<int> ClientSize => host.ClientSize;
    private Silk.NET.Maths.Vector2D<int> FramebufferSize => host.FramebufferSize;
    public GalleryWindow(GalleryOptions options)
    {
        this.options = options;
        assetsPath = Path.GetFullPath(options.AssetsPath ?? Path.Combine(AppContext.BaseDirectory, "Assets"));
        host = new();
        input = new(host, () => view, Shortcut);
    }
    public void Run()
    {
        Console.WriteLine($"OpenGL {GL.GetString(StringName.Version)} / {GL.GetString(StringName.Renderer)}");
        LoadUi();
        Console.WriteLine("SDL native IME input. F5 reloads HTML/CSS; F12 captures. Tab navigates; the Multilingual tab includes a composition exercise.");
        while (host.Running)
        {
            input.Poll();
            if (!host.Running) break;
            if (FramebufferSize.X <= 0 || FramebufferSize.Y <= 0) { host.Api.Delay(10); continue; }
            RenderFrame();
        }
    }
    private void LoadUi()
    {
        // Construct first so a CSS error on reload leaves the current UI usable.
        var assets = new GalleryAssets(assetsPath);
        var document = HtmlLoader.Load(assets.ReadText("index.html"), assets, UiTheme.Neutral);
        fonts ??= new(document, "Lato");
        fonts.Load(document);
        var nextView = new UiView(document, fonts);
        var backend = new SkiaTextBackend(fonts);
        OpenGlUiRenderer? nextRenderer = null;
        try
        {
            nextView.Input.Clipboard = input.Text;
            nextView.Resize(ClientSize.X, ClientSize.Y);
            nextRenderer = new(nextView, backend, ownsTextBackend: true);
            var nextController = new GalleryController(nextView);
            GalleryPreview.Apply(nextView, nextController, options);
            renderer?.Dispose();
            view?.Dispose();
            view = nextView;
            renderer = nextRenderer;
            controller = nextController;
        }
        catch
        {
            if (nextRenderer == null) backend.Dispose(); else nextRenderer.Dispose();
            nextView.Dispose();
            throw;
        }
    }

    private void RenderFrame()
    {
        GL.Disable(EnableCap.ScissorTest);
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        GL.ClearColor(.035f, .055f, .09f, 1);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        controller.Update(view.Input.Focused);
        if (options.Capture && options.Tooltip != null)
        {
            view.Update();
            var bounds = view.Document.GetElement(options.Tooltip).Bounds;
            view.Input.PointerMove(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        }
        if (options.Capture && ++frames == 30) capture = true;
        renderer.Render(FramebufferSize.X, FramebufferSize.Y);
        if (capture)
        {
            capture = false;
            GuiShark.Demo.FrameCapture.Save(FramebufferSize.X, FramebufferSize.Y);
        }
        host.SwapBuffers();
        if (options.Capture && frames >= 30) host.Running = false;
    }

    private void Shortcut(KeyCode key)
    {
        if (key == KeyCode.KEscape) host.Running = false;
        if (key == KeyCode.KF12) capture = true;
        if (key == KeyCode.KF5)
            try { LoadUi(); } catch (Exception error) { Console.Error.WriteLine($"Reload failed: {error.Message}"); }
    }
    public void Dispose()
    {
        if (disposed) return;
        renderer?.Dispose();
        view?.Dispose();
        fonts?.Dispose();
        host.Dispose();
        disposed = true;
    }
}
