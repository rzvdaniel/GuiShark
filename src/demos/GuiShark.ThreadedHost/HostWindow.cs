using GuiShark.OpenGL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace GuiShark.ThreadedHost;

internal sealed class HostWindow : GameWindow
{
    private readonly string assetsPath;
    private readonly bool verify;
    private readonly bool capture;
    private readonly SharedFrames frames = new();
    private PanelViewport panel;
    private FontBook fonts = null!;
    private UiView view = null!;
    private OpenGlUiRenderer renderer = null!;
    private AppWorker worker = null!;
    private int readFramebuffer;
    private int hostFrames;
    private double elapsed;
    private int verifyStep;
    private int hostAtFreeze;
    private int appAtFreeze;
    private int presentedFrames;
    private int presentedAtResize;
    private int widthBeforeResize;

    public HostWindow(string assetsPath, bool verify, bool capture) : base(new GameWindowSettings { UpdateFrequency = 60 }, new NativeWindowSettings
    {
        ClientSize = new Vector2i(920, 730),
        Title = "GuiShark threaded application host",
        APIVersion = new Version(3, 3),
        Profile = ContextProfile.Core,
        Flags = ContextFlags.ForwardCompatible
    })
    {
        this.assetsPath = assetsPath;
        this.verify = verify;
        this.capture = capture;
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        VSync = VSyncMode.On;
        fonts = new FontBook(Path.Combine(assetsPath, "fonts/Lato-Regular.ttf"), Path.Combine(assetsPath, "fonts/Lato-Bold.ttf"));
        var assets = new DirectoryAssetSource(assetsPath);
        var document = HtmlLoader.Load(assets.ReadText("host.html"), assets);
        view = new UiView(document, fonts);
        view.Resize(ClientSize.X, ClientSize.Y);
        renderer = new OpenGlUiRenderer(view, fonts);
        panel = PanelViewport.Measure(view, ClientSize, FramebufferSize);
        if (!panel.IsValid) throw new InvalidOperationException("The app panel has no drawable area.");
        readFramebuffer = GL.GenFramebuffer();

        var appWindow = SharedAppContext.Create(this);
        worker = new AppWorker(appWindow, assetsPath, frames, panel.AppSize);
        worker.Start();
        document.GetElement("freeze").Clicked += _ => worker.Send(new(WorkerAction.Freeze));
        document.GetElement("error").Clicked += _ => worker.Send(new(WorkerAction.Error));
        Console.WriteLine("Host and app running. Use the buttons to freeze or fault the app; Escape closes.");
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        elapsed += args.Time;
        hostFrames++;
        if (hostFrames % 15 == 0) UpdateStatus();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        GL.ClearColor(.04f, .08f, .12f, 1);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        renderer.Render(FramebufferSize.X, FramebufferSize.Y);
        UpdatePanel();
        if (panel.IsValid && frames.TryUseLatest(panel.AppSize, DrawAppTexture)) presentedFrames++;
        if (capture && elapsed > 1 && worker.FrameCount > 5)
        {
            FrameCapture.Save(FramebufferSize.X, FramebufferSize.Y);
            Close();
        }
        SwapBuffers();
        if (verify) VerifyIsolation();
    }

    private void DrawAppTexture(int texture)
    {
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, readFramebuffer);
        GL.FramebufferTexture2D(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texture, 0);
        GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        GL.BlitFramebuffer(0, 0, panel.PixelWidth, panel.PixelHeight,
            panel.Left, FramebufferSize.Y - panel.Bottom, panel.Right, FramebufferSize.Y - panel.Top,
            ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
        GL.Finish(); // The worker can reuse an old displayed slot after this draw.
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    private void UpdatePanel()
    {
        var measured = PanelViewport.Measure(view, ClientSize, FramebufferSize);
        if (measured.AppSize != panel.AppSize && measured.IsValid)
        {
            worker.Send(new(WorkerAction.Resize, Size: measured.AppSize));
            if (verifyStep == 5) presentedAtResize = presentedFrames;
        }
        panel = measured;
    }

    private void UpdateStatus()
    {
        view.Document.GetElement("frames").Text = $"Host frames: {hostFrames} / App frames: {worker.FrameCount}";
        view.Document.GetElement("worker-status").Text = worker.Status;
        view.Document.GetElement("host-status").Text = $"Host running ({elapsed:F1}s)";
    }

    private void VerifyIsolation()
    {
        if (verifyStep == 0 && elapsed > 1 && worker.FrameCount > 5)
        {
            ClickAppButton();
            verifyStep = 1;
            Console.WriteLine("VERIFY: clicked the app's GuiShark button");
        }
        if (verifyStep == 1 && elapsed > 1.4)
        {
            Require(worker.ClickCount == 1, "app button callback did not run");
            worker.Send(new(WorkerAction.Freeze));
            hostAtFreeze = hostFrames;
            appAtFreeze = worker.FrameCount;
            verifyStep = 2;
            Console.WriteLine("VERIFY: requested five-second app freeze");
        }
        if (verifyStep == 2 && elapsed > 2.8)
        {
            Require(hostFrames - hostAtFreeze > 20, "host stopped during app freeze");
            Require(worker.FrameCount <= appAtFreeze + 1, "app did not freeze");
            verifyStep = 3;
            Console.WriteLine("VERIFY: host advanced while app frames stopped");
        }
        if (verifyStep == 3 && elapsed > 7.5)
        {
            Require(worker.FrameCount > appAtFreeze + 5, "app did not resume");
            worker.Send(new(WorkerAction.Error));
            verifyStep = 4;
            Console.WriteLine("VERIFY: app resumed; requested handled exception");
        }
        if (verifyStep >= 4) VerifyRecoveryAndResize();
        if (elapsed > 13 && verifyStep < 6) Require(false, "timed out");
    }

    private void VerifyRecoveryAndResize()
    {
        if (verifyStep == 4 && elapsed > 8)
        {
            Require(worker.Status.StartsWith("Handled app error:", StringComparison.Ordinal), "app error was not contained");
            Require(worker.FrameCount > appAtFreeze + 8, "app stopped after handled error");
            widthBeforeResize = panel.PixelWidth;
            presentedAtResize = presentedFrames;
            verifyStep = 5;
            ClientSize = new Vector2i(1040, 730);
            Console.WriteLine("VERIFY: resized the host panel");
        }
        if (verifyStep == 5 && elapsed > 8.8)
        {
            Require(panel.PixelWidth > widthBeforeResize, "host panel did not resize");
            Require(presentedFrames > presentedAtResize + 5, "app did not publish frames at the new pixel size");
            verifyStep = 6;
            Console.WriteLine("VERIFY PASS: host remained responsive; app resumed and handled its exception");
            Close();
        }
    }

    private void ClickAppButton()
    {
        ForwardPointer(WorkerAction.Down, panel.Bounds.X + 200, panel.Bounds.Y + 275);
        ForwardPointer(WorkerAction.Up, panel.Bounds.X + 200, panel.Bounds.Y + 275);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException($"VERIFY FAIL: {message}");
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        view?.Input.PointerMove(e.X, e.Y);
        ForwardPointer(WorkerAction.Move, e.X, e.Y);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButton.Left) return;
        view?.Input.PointerDown(MousePosition.X, MousePosition.Y);
        ForwardPointer(WorkerAction.Down, MousePosition.X, MousePosition.Y);
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButton.Left) return;
        view?.Input.PointerUp(MousePosition.X, MousePosition.Y);
        ForwardPointer(WorkerAction.Up, MousePosition.X, MousePosition.Y);
    }

    private void ForwardPointer(WorkerAction action, float x, float y)
    {
        if (worker is null || !panel.Contains(x, y)) return;
        worker.Send(new(action, x - panel.Bounds.X, y - panel.Bounds.Y));
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        view?.Resize(ClientSize.X, ClientSize.Y);
        if (view is not null && worker is not null) UpdatePanel();
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Keys.Escape) Close();
    }

    protected override void OnUnload()
    {
        worker?.Dispose();
        if (readFramebuffer != 0) GL.DeleteFramebuffer(readFramebuffer);
        renderer?.Dispose();
        view?.Dispose();
        fonts?.Dispose();
        base.OnUnload();
    }
}
