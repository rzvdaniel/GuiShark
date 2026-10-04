using System.Collections.Concurrent;
using System.Diagnostics;
using GuiShark.OpenGL;
using GuiShark.AppProtocol;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace GuiShark.ProcessHosting;

public abstract class ProcessAppWindow : GameWindow
{
    private readonly bool embedded;
    private Action<AppMessage>? send;
    private readonly ConcurrentQueue<AppMessage> incoming = new();
    private FontBook fonts = null!;
    private UiView view = null!;
    private OpenGlUiRenderer renderer = null!;
    private int framebuffer;
    private int texture;
    private int renderWidth;
    private int renderHeight;
    private int targetWidth;
    private int targetHeight;
    private byte[] framePixels = [];
    private long frameSequence;
    private long lastFrameTicks;

    protected ProcessAppWindow(string title, bool embedded = false) : base(new GameWindowSettings { UpdateFrequency = 60 }, new NativeWindowSettings
    {
        ClientSize = new Vector2i(730, 520),
        Title = "Aurora Monitor · GuiShark standalone app",
        StartVisible = !embedded,
        APIVersion = new Version(3, 3),
        Profile = ContextProfile.Core,
        Flags = ContextFlags.ForwardCompatible
    })
    {
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
        OnAppLoaded(view);
        Send(new AppMessage("ready", Value: "App UI is ready"));
    }

    protected abstract void OnAppLoaded(UiView appView);
    protected virtual void OnAppUpdate(UiView appView, double seconds) { }
    protected virtual string? FrameValue => null;
    protected void Send(AppMessage message) => send?.Invoke(message);

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        ProcessMessages();
        if (IsExiting) return;
        OnAppUpdate(view, args.Time);
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
                case "focus-lost": view.Input.Cancel(); break;
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
            send?.Invoke(new AppMessage("status", Value: "App resumed after five seconds"));
            return true;
        }
        if (message.Type == "crash") Environment.FailFast("Deliberate app process crash");
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
            throw new InvalidOperationException("App offscreen framebuffer is incomplete.");
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
        send?.Invoke(AppFrameEncoder.Encode(width, height, framePixels, FrameValue, ++frameSequence));
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
        if (e.Button == MouseButton.Left) view?.Input.PointerDown(MousePosition.X, MousePosition.Y, KeyboardState.IsKeyDown(Keys.LeftShift) || KeyboardState.IsKeyDown(Keys.RightShift));
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButton.Left) view?.Input.PointerUp(MousePosition.X, MousePosition.Y);
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);
        var handled = TryMapKey(e.Key.ToString(), out var key) && view is not null &&
            view.Input.KeyDown(key, e.Shift, e.IsRepeat, e.Control);
        if (e.Key == Keys.Escape && !handled) Close();
    }

    protected override void OnKeyUp(KeyboardKeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (TryMapKey(e.Key.ToString(), out var key)) view?.Input.KeyUp(key);
    }
    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        view?.Input.TextInput(e.AsString);
    }
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        view?.Input.PointerWheel(MousePosition.X, MousePosition.Y, e.OffsetY);
    }
    protected override void OnFocusedChanged(FocusedChangedEventArgs e)
    {
        base.OnFocusedChanged(e);
        if (!e.IsFocused) view?.Input.Cancel();
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
