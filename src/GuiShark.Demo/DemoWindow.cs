using GuiShark.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;

namespace GuiShark.Demo;

internal sealed class DemoWindow : GameWindow
{
    private readonly string assetsPath;
    private FontBook fonts = null!;
    private UiView view = null!;
    private OpenGlUiRenderer renderer = null!;
    private DemoController controller = null!;
    private Backdrop backdrop = null!;
    private double elapsed;
    private bool capture;

    public DemoWindow(string assetsPath) : base(new GameWindowSettings { UpdateFrequency = 60 }, new NativeWindowSettings
    {
        ClientSize = new Vector2i(1060, 820),
        MinimumClientSize = new Vector2i(840, 760),
        Title = "GuiShark · HTML meets OpenGL",
        APIVersion = new Version(3, 3),
        Profile = ContextProfile.Core,
        Flags = ContextFlags.ForwardCompatible
    }) => this.assetsPath = assetsPath;

    protected override void OnLoad()
    {
        base.OnLoad();
        VSync = VSyncMode.On;
        Console.WriteLine($"OpenGL {GL.GetString(StringName.Version)} / {GL.GetString(StringName.Renderer)}");
        fonts = new(Path.Combine(assetsPath, "fonts/Lato-Regular.ttf"), Path.Combine(assetsPath, "fonts/Lato-Bold.ttf"));
        backdrop = new();
        LoadUi();
        Console.WriteLine("F5 reloads HTML/CSS. F12 saves a screenshot. Tab/Shift+Tab navigate buttons; Enter/Space activate.");
    }

    private void LoadUi()
    {
        // Construct first so a CSS error on reload leaves the current UI usable.
        var assets = new DirectoryAssetSource(assetsPath);
        var document = HtmlLoader.Load(assets.ReadText("index.html"), assets);
        var nextView = new UiView(document, fonts);
        nextView.Resize(ClientSize.X, ClientSize.Y);
        nextView.Update();
        var nextRenderer = new OpenGlUiRenderer(nextView, fonts);
        renderer?.Dispose();
        view?.Dispose();
        view = nextView;
        renderer = nextRenderer;
        controller = new(document);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        elapsed += args.Time;
        backdrop.Render(FramebufferSize.X, FramebufferSize.Y, (float)elapsed, controller.DarkTheme);
        renderer.Render(FramebufferSize.X, FramebufferSize.Y);
        if (capture)
        {
            capture = false;
            FrameCapture.Save(FramebufferSize.X, FramebufferSize.Y);
        }
        SwapBuffers();
    }

    protected override void OnResize(ResizeEventArgs args)
    {
        base.OnResize(args);
        view?.Resize(ClientSize.X, ClientSize.Y);
    }

    protected override void OnMouseMove(MouseMoveEventArgs args)
    {
        base.OnMouseMove(args);
        view?.Input.PointerMove(args.X, args.Y);
    }

    protected override void OnMouseDown(MouseButtonEventArgs args)
    {
        base.OnMouseDown(args);
        if (args.Button == MouseButton.Left) view?.Input.PointerDown(MousePosition.X, MousePosition.Y);
    }

    protected override void OnMouseUp(MouseButtonEventArgs args)
    {
        base.OnMouseUp(args);
        if (args.Button == MouseButton.Left) view?.Input.PointerUp(MousePosition.X, MousePosition.Y);
    }

    protected override void OnFocusedChanged(FocusedChangedEventArgs args)
    {
        base.OnFocusedChanged(args);
        if (!args.IsFocused) view?.Input.Cancel();
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs args)
    {
        base.OnKeyDown(args);
        if (MapKey(args.Key) is { } key && view.Input.KeyDown(key, args.Shift, args.IsRepeat)) return;
        if (args.Key == Keys.Escape) Close();
        if (args.Key == Keys.F12) capture = true;
        if (args.Key == Keys.F5 && !args.IsRepeat)
            try { LoadUi(); } catch (Exception error) { Console.Error.WriteLine($"Reload failed: {error.Message}"); }
    }

    protected override void OnKeyUp(KeyboardKeyEventArgs args)
    {
        base.OnKeyUp(args);
        if (MapKey(args.Key) is { } key) view.Input.KeyUp(key);
    }

    private static UiKey? MapKey(Keys key) => key switch
    {
        Keys.Tab => UiKey.Tab, Keys.Enter or Keys.KeyPadEnter => UiKey.Enter,
        Keys.Space => UiKey.Space, Keys.Escape => UiKey.Escape, _ => null
    };

    protected override void OnUnload()
    {
        renderer?.Dispose();
        view?.Dispose();
        fonts?.Dispose();
        backdrop?.Dispose();
        base.OnUnload();
    }
}
