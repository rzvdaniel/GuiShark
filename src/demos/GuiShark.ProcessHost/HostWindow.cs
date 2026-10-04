using GuiShark.ProcessHosting;
using GuiShark.AppProtocol;
using GuiShark.OpenGL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace GuiShark.ProcessHost;

internal sealed class HostWindow : GameWindow
{
    private readonly HostOptions options;
    private AppPackage package = null!;
    private FontBook fonts = null!;
    private UiView view = null!;
    private OpenGlUiRenderer renderer = null!;
    private AppPanel? panel;
    private AppProcess? app;
    private PanelViewport viewport;
    private double elapsed;
    private int frames;
    private int verifyStep;
    private int framesAtFreeze;
    private int framesAtCrash;
    private int appPid;
    private int appCount;
    private int sentWidth;
    private int sentHeight;
    private int sentPixelWidth;
    private int sentPixelHeight;
    private bool ready;
    private bool resumed;
    private bool appFocused;
    private bool appPointerCaptured;

    public HostWindow(HostOptions options) : base(new GameWindowSettings { UpdateFrequency = 60 }, new NativeWindowSettings
    {
        ClientSize = new Vector2i(1100, 680),
        Title = "GuiShark process workspace",
        APIVersion = new Version(3, 3),
        Profile = ContextProfile.Core,
        Flags = ContextFlags.ForwardCompatible
    }) => this.options = options;

    protected override void OnLoad()
    {
        base.OnLoad();
        VSync = VSyncMode.On;
        package = AppPackage.Load(options.ManifestPath);
        var assetsPath = Path.Combine(AppContext.BaseDirectory, "Assets");
        fonts = new FontBook(Path.Combine(assetsPath, "fonts/Lato-Regular.ttf"), Path.Combine(assetsPath, "fonts/Lato-Bold.ttf"));
        var assets = new DirectoryAssetSource(assetsPath);
        var document = HtmlLoader.Load(assets.ReadText("host.html"), assets);
        view = new UiView(document, fonts);
        view.Resize(ClientSize.X, ClientSize.Y);
        renderer = new OpenGlUiRenderer(view, fonts);
        document.GetElement("load").Clicked += _ => StartApp();
        document.GetElement("restart").Clicked += _ => StartApp();
        document.GetElement("freeze").Clicked += _ => app?.Send(new AppMessage("freeze"));
        document.GetElement("crash").Clicked += _ => app?.Send(new AppMessage("crash"));
        StartApp();
        Console.WriteLine($"Loaded {package.Title} from {options.ManifestPath}. Escape closes the host.");
    }

    private void StartApp()
    {
        app?.Dispose();
        panel?.Dispose();
        app = new AppProcess(package);
        panel = new AppPanel();
        appPid = app.Id;
        sentWidth = sentHeight = sentPixelWidth = sentPixelHeight = 0;
        appFocused = appPointerCaptured = false;
        ready = resumed = false;
        SetStatus($"Starting {package.Title}…");
        view.Document.GetElement("pid").Text = $"PID {appPid}";
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        elapsed += args.Time;
        frames++;
        ReceiveMessages();
        if (frames % 20 == 0) view.Document.GetElement("frames").Text = $"Host frames: {frames}";
        viewport = PanelViewport.Measure(view, ClientSize, FramebufferSize);
        SendResizeIfNeeded();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        GL.ClearColor(.04f, .08f, .13f, 1);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        renderer.Render(FramebufferSize.X, FramebufferSize.Y);
        panel?.Render(viewport, FramebufferSize.Y);
        SwapBuffers();
        if (options.Verify) Verify();
        if (options.Capture && elapsed > 1.5) { FrameCapture.Save(FramebufferSize.X, FramebufferSize.Y); Close(); }
    }

    private void SendResizeIfNeeded()
    {
        var width = (int)MathF.Round(viewport.Bounds.Width);
        var height = (int)MathF.Round(viewport.Bounds.Height);
        if (!viewport.IsValid || app is null || sentWidth == width && sentHeight == height
            && sentPixelWidth == viewport.PixelWidth && sentPixelHeight == viewport.PixelHeight) return;
        sentWidth = width;
        sentHeight = height;
        sentPixelWidth = viewport.PixelWidth;
        sentPixelHeight = viewport.PixelHeight;
        app.Send(new AppMessage("resize", Width: sentWidth, Height: sentHeight,
            PixelWidth: sentPixelWidth, PixelHeight: sentPixelHeight));
    }

    private void ReceiveMessages()
    {
        if (app is null) return;
        while (app.TryReceive(out var message))
        {
            try
            {
                switch (message!.Type)
                {
                    case "ready": ready = true; SetStatus(message.Value ?? "App connected"); break;
                    case "frame":
                        panel?.Apply(message);
                        if (options.Verify && int.TryParse(message.Value, out var frameCount)) appCount = frameCount;
                        break;
                    case "state" when options.Verify && int.TryParse(message.Value, out var stateCount): appCount = stateCount; break;
                    case "status": resumed = true; SetStatus(message.Value ?? "Running"); break;
                    case "disconnected": SetStatus($"App process {appPid} disconnected. Host is still running."); break;
                }
            }
            catch (Exception error) when (error is InvalidDataException or FormatException or ArgumentException)
            {
                SetStatus($"Invalid app frame: {error.Message}");
            }
        }
        if (app.HasExited) view.Document.GetElement("app-status").Text = $"App exited ({app.ExitCode}) • host continues";
    }

    private void SetStatus(string status)
    {
        view.Document.GetElement("status").Text = status;
        view.Document.GetElement("app-status").Text = status;
    }

    private void Verify()
    {
        if (verifyStep == 0 && ready && panel?.HasFrame == true && elapsed > 1)
        {
            // Aurora's sample button is centered in its content card.
            appFocused = true;
            SendAppInput(new AppMessage("pointer-down", X: 200, Y: 310));
            SendAppInput(new AppMessage("pointer-up", X: 200, Y: 310));
            verifyStep = 1;
            Console.WriteLine("VERIFY: forwarded pointer input to Aurora");
        }
        if (verifyStep == 1 && appCount == 1)
        {
            app!.Send(new AppMessage("freeze"));
            framesAtFreeze = frames;
            verifyStep = 2;
            Console.WriteLine("VERIFY: froze child for five seconds");
        }
        if (verifyStep == 2 && frames - framesAtFreeze > 50)
        {
            Require(!resumed, "child resumed too early");
            verifyStep = 3;
            Console.WriteLine("VERIFY: host rendered during child freeze");
        }
        VerifyRecovery();
        if (elapsed > 14 && verifyStep < 6) Require(false, $"timed out at step {verifyStep}");
    }

    private void VerifyRecovery()
    {
        if (verifyStep == 3 && resumed)
        {
            app!.Send(new AppMessage("crash"));
            framesAtCrash = frames;
            verifyStep = 4;
            Console.WriteLine("VERIFY: sent fatal child crash");
        }
        if (verifyStep == 4 && app!.HasExited && frames - framesAtCrash > 20)
        {
            StartApp();
            verifyStep = 5;
            Console.WriteLine("VERIFY: host stayed responsive; restarting child");
        }
        if (verifyStep == 5 && ready && panel?.HasFrame == true)
        {
            verifyStep = 6;
            Console.WriteLine("VERIFY PASS: forwarded input, froze, crashed, and restarted Aurora");
            Close();
        }
    }

    private static void Require(bool condition, string reason)
    {
        if (!condition) throw new InvalidOperationException($"VERIFY FAIL: {reason}");
    }

    private void SendAppInput(AppMessage message)
    {
        if (app is { HasExited: false } && appFocused) app.Send(message);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        view?.Input.PointerMove(e.X, e.Y);
        if (app is { HasExited: false })
            app.Send(new AppMessage("pointer-move", X: e.X - viewport.Bounds.X, Y: e.Y - viewport.Bounds.Y));
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButton.Left) view?.Input.PointerDown(MousePosition.X, MousePosition.Y);
        if (e.Button == MouseButton.Left && viewport.Contains(MousePosition.X, MousePosition.Y))
        {
            appFocused = appPointerCaptured = true;
            SendPointer("pointer-down", MousePosition.X, MousePosition.Y, IsShiftDown());
        }
        else if (e.Button == MouseButton.Left) appFocused = false;
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButton.Left) view?.Input.PointerUp(MousePosition.X, MousePosition.Y);
        if (e.Button == MouseButton.Left && appPointerCaptured)
        {
            SendPointer("pointer-up", MousePosition.X, MousePosition.Y, IsShiftDown());
            appPointerCaptured = false;
        }
    }

    private void SendPointer(string type, float x, float y, bool shift)
    {
        app?.Send(new AppMessage(type, X: x - viewport.Bounds.X, Y: y - viewport.Bounds.Y, Shift: shift));
    }

    private bool IsShiftDown() => KeyboardState.IsKeyDown(Keys.LeftShift) || KeyboardState.IsKeyDown(Keys.RightShift);

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (viewport.Contains(MousePosition.X, MousePosition.Y))
            app?.Send(new AppMessage("pointer-wheel", X: MousePosition.X - viewport.Bounds.X,
                Y: MousePosition.Y - viewport.Bounds.Y, Delta: e.OffsetY));
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        if (appFocused && e.AsString.Length > 0) SendAppInput(new AppMessage("text-input", Value: e.AsString));
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        view?.Resize(ClientSize.X, ClientSize.Y);
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Keys.Escape) { Close(); return; }
        if (appFocused) SendAppInput(new AppMessage("key-down", Key: e.Key.ToString(), Shift: e.Shift,
            Command: e.Control, Repeat: e.IsRepeat));
    }

    protected override void OnKeyUp(KeyboardKeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (appFocused) SendAppInput(new AppMessage("key-up", Key: e.Key.ToString()));
    }

    protected override void OnUnload()
    {
        app?.Dispose();
        panel?.Dispose();
        renderer?.Dispose();
        view?.Dispose();
        fonts?.Dispose();
        base.OnUnload();
    }
}
