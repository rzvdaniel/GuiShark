using System.Collections.Concurrent;
using System.Diagnostics;
using GuiShark.OpenGL;
using GuiShark.AppProtocol;
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
    private readonly bool embedded;
    private Action<AppMessage>? send;
    private readonly ConcurrentQueue<AppMessage> incoming = new();
    private FontBook fonts = null!;
    private UiView view = null!;
    private OpenGlUiRenderer renderer = null!;
    private int verifyStep;
    private double elapsed;
    private int framebuffer;
    private int texture;
    private int renderWidth;
    private int renderHeight;
    private int targetWidth;
    private int targetHeight;
    private byte[] framePixels = [];
    private long frameSequence;
    private long lastFrameTicks;

    public AuroraWindow(bool verify, Action<AppMessage>? send = null, bool embedded = false) : base(new GameWindowSettings { UpdateFrequency = 60 }, new NativeWindowSettings
    {
        ClientSize = new Vector2i(730, 520),
        Title = "Aurora Monitor · GuiShark standalone app",
        StartVisible = !embedded,
        APIVersion = new Version(3, 3),
        Profile = ContextProfile.Core,
        Flags = ContextFlags.ForwardCompatible
    })
    {
        this.verify = verify;
        this.send = send;
        this.embedded = embedded;
    }

    public void Enqueue(AppMessage message) => incoming.Enqueue(message);

    public void SetSender(Action<AppMessage> sender) => send = sender;

    protected override void OnLoad()
    {
        base.OnLoad();
        VSync = embedded ? VSyncMode.Off : VSyncMode.On;
        var assetsPath = Path.Combine(AppContext.BaseDirectory, "Assets");
        fonts = new FontBook(Path.Combine(assetsPath, "fonts/Lato-Regular.ttf"), Path.Combine(assetsPath, "fonts/Lato-Bold.ttf"));
        var assets = new DirectoryAssetSource(assetsPath);
        var document = HtmlLoader.Load(assets.ReadText("app.html"), assets);
        view = new UiView(document, fonts);
        view.Resize(ClientSize.X, ClientSize.Y);
        renderer = new OpenGlUiRenderer(view, fonts);
        document.GetElement("increment").Clicked += _ => Increment();
        send?.Invoke(new AppMessage("ready", Value: "Aurora UI is ready"));
    }

    private void Increment()
    {
        counter.Increment();
        view.Document.GetElement("count").Text = counter.Count.ToString();
        view.Document.GetElement("message").Text = counter.Message;
        send?.Invoke(new AppMessage("state", Value: counter.Count.ToString()));
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        elapsed += args.Time;
        ProcessMessages();
        if (IsExiting) return;
        var width = embedded ? Math.Max(1, renderWidth) : FramebufferSize.X;
        var height = embedded ? Math.Max(1, renderHeight) : FramebufferSize.Y;
        if (embedded) EnsureTarget(width, height);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, embedded ? framebuffer : 0);
        GL.Viewport(0, 0, width, height);
        GL.ClearColor(.08f, .24f, .38f, 1);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        renderer.Render(width, height);
        if (embedded) PublishFrame(width, height);
        SwapBuffers();
        if (verify) VerifyClick();
    }

    private void ProcessMessages()
    {
        while (incoming.TryDequeue(out var message))
        {
            if (HandleLifecycleMessage(message)) continue;
            switch (message.Type)
            {
                case "resize" when message.Width > 0 && message.Height > 0 && message.PixelWidth > 0 && message.PixelHeight > 0:
                    view.Resize(message.Width, message.Height);
                    renderWidth = message.PixelWidth;
                    renderHeight = message.PixelHeight;
                    break;
                case "pointer-move": view.Input.PointerMove(message.X, message.Y); break;
                case "pointer-down": view.Input.PointerDown(message.X, message.Y, message.Shift); break;
                case "pointer-up": view.Input.PointerUp(message.X, message.Y); break;
                case "pointer-wheel": view.Input.PointerWheel(message.X, message.Y, message.Delta); break;
                case "key-down" when TryMapKey(message.Key, out var down):
                    view.Input.KeyDown(down, message.Shift, message.Repeat, message.Command);
                    break;
                case "key-up" when TryMapKey(message.Key, out var up): view.Input.KeyUp(up); break;
                case "text-input" when message.Value is not null: view.Input.TextInput(message.Value); break;
            }
        }
    }

    private bool HandleLifecycleMessage(AppMessage message)
    {
        if (message.Type == "stop") { Close(); return true; }
        if (message.Type == "freeze")
        {
            Thread.Sleep(5000);
            send?.Invoke(new AppMessage("status", Value: "Aurora resumed after five seconds"));
            return true;
        }
        if (message.Type == "crash") Environment.FailFast("Deliberate Aurora process crash");
        return false;
    }

    private void EnsureTarget(int width, int height)
    {
        if (framebuffer != 0 && width == targetWidth && height == targetHeight) return;
        if (framebuffer != 0) GL.DeleteFramebuffer(framebuffer);
        if (texture != 0) GL.DeleteTexture(texture);
        texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, texture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, width, height, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        framebuffer = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, texture, 0);
        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            throw new InvalidOperationException("Aurora's offscreen framebuffer is incomplete.");
        targetWidth = width;
        targetHeight = height;
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    private void PublishFrame(int width, int height)
    {
        if (Stopwatch.GetElapsedTime(lastFrameTicks) < TimeSpan.FromMilliseconds(100)) return;
        lastFrameTicks = Stopwatch.GetTimestamp();
        var requiredBytes = checked(width * height * 4);
        if (framePixels.Length != requiredBytes) framePixels = new byte[requiredBytes];
        GL.ReadPixels(0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, framePixels);
        send?.Invoke(AuroraFrameEncoder.Encode(width, height, framePixels, counter.Count, ++frameSequence));
    }

    private static bool TryMapKey(string? name, out UiKey key)
    {
        if (Enum.TryParse(name, out key)) return true;
        key = name switch
        {
            "Backspace" => UiKey.Backspace, "Delete" => UiKey.Delete, "PageUp" => UiKey.PageUp,
            "PageDown" => UiKey.PageDown, "Left" => UiKey.Left, "Right" => UiKey.Right,
            "Up" => UiKey.Up, "Down" => UiKey.Down, _ => default
        };
        return name is "Backspace" or "Delete" or "PageUp" or "PageDown" or "Left" or "Right" or "Up" or "Down";
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
        if (framebuffer != 0) GL.DeleteFramebuffer(framebuffer);
        if (texture != 0) GL.DeleteTexture(texture);
        renderer?.Dispose();
        view?.Dispose();
        fonts?.Dispose();
        base.OnUnload();
    }
}
