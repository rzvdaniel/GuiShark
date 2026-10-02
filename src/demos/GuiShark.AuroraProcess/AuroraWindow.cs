using GuiShark.OpenGL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace GuiShark.AuroraProcess;

internal sealed class AuroraWindow : GameWindow
{
    private readonly AuroraCounter counter = new();
    private readonly bool verify;
    private FontBook fonts = null!;
    private UiView view = null!;
    private OpenGlUiRenderer renderer = null!;
    private int verifyStep;
    private double elapsed;

    public AuroraWindow(bool verify) : base(new GameWindowSettings { UpdateFrequency = 60 }, new NativeWindowSettings
    {
        ClientSize = new Vector2i(730, 520),
        Title = "Aurora Monitor · GuiShark standalone app",
        APIVersion = new Version(3, 3),
        Profile = ContextProfile.Core,
        Flags = ContextFlags.ForwardCompatible
    }) => this.verify = verify;

    protected override void OnLoad()
    {
        base.OnLoad();
        VSync = VSyncMode.On;
        var assetsPath = Path.Combine(AppContext.BaseDirectory, "Assets");
        fonts = new FontBook(Path.Combine(assetsPath, "fonts/Lato-Regular.ttf"), Path.Combine(assetsPath, "fonts/Lato-Bold.ttf"));
        var assets = new DirectoryAssetSource(assetsPath);
        var document = HtmlLoader.Load(assets.ReadText("app.html"), assets);
        view = new UiView(document, fonts);
        view.Resize(ClientSize.X, ClientSize.Y);
        renderer = new OpenGlUiRenderer(view, fonts);
        document.GetElement("increment").Clicked += _ => Increment();
    }

    private void Increment()
    {
        counter.Increment();
        view.Document.GetElement("count").Text = counter.Count.ToString();
        view.Document.GetElement("message").Text = counter.Message;
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        elapsed += args.Time;
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        GL.ClearColor(.08f, .24f, .38f, 1);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        renderer.Render(FramebufferSize.X, FramebufferSize.Y);
        SwapBuffers();
        if (verify) VerifyClick();
    }

    private void VerifyClick()
    {
        if (verifyStep == 0 && elapsed > .5)
        {
            var button = view.Document.GetElement("increment").Bounds;
            var x = button.X + button.Width / 2;
            var y = button.Y + button.Height / 2;
            view.Input.PointerMove(x, y);
            view.Input.PointerDown(x, y);
            view.Input.PointerUp(x, y);
            verifyStep = 1;
        }
        else if (verifyStep == 1)
        {
            if (counter.Count != 1) throw new InvalidOperationException("Standalone Aurora click did not update its state.");
            Console.WriteLine("VERIFY PASS: standalone window rendered and handled a button click");
            Close();
        }
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        view?.Resize(ClientSize.X, ClientSize.Y);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        view?.Input.PointerMove(e.X, e.Y);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButton.Left) view?.Input.PointerDown(MousePosition.X, MousePosition.Y);
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButton.Left) view?.Input.PointerUp(MousePosition.X, MousePosition.Y);
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Keys.Escape) Close();
    }

    protected override void OnUnload()
    {
        renderer?.Dispose();
        view?.Dispose();
        fonts?.Dispose();
        base.OnUnload();
    }
}
