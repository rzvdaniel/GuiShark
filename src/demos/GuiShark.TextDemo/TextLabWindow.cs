using GuiShark.OpenGL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace GuiShark.TextDemo;

internal sealed class TextLabWindow : GameWindow
{
    private readonly LaunchOptions launch;
    private readonly LabSettings settings;
    private readonly List<TextPane> panes = [];
    private FontBook fonts = null!;
    private DirectoryAssetSource assets = null!;
    private BackendCatalog catalog = null!;
    private ScreenPainter painter = null!;
    private LabChrome? chrome;
    private IReadOnlyList<PaneLayout> layouts = [];
    private bool rebuild = true, capture;
    private int previousMode = -1, previousScript = -1, frames;
    private Vector2i previousFramebuffer;
    private double elapsed, statisticsTime;

    public TextLabWindow(LaunchOptions launch) : base(new GameWindowSettings { UpdateFrequency = 60 }, new NativeWindowSettings
    {
        ClientSize = new(launch.Width, launch.Height), MinimumClientSize = new(1180, 900),
        Title = "GuiShark · Text Lab", APIVersion = new(3, 3), Profile = ContextProfile.Core,
        Flags = ContextFlags.ForwardCompatible
    }) { this.launch = launch; settings = launch.Settings; }

    protected override void OnLoad()
    {
        base.OnLoad(); VSync = VSyncMode.On;
        assets = new(launch.Assets);
        // CSS is the source of truth for the sample fonts.
        fonts = new(SampleDocument.Create(assets, settings, 1), "Lato");
        catalog = new(fonts); painter = new();
        Console.WriteLine($"OpenGL {GL.GetString(StringName.Version)} / {GL.GetString(StringName.Renderer)}");
        Console.WriteLine("Text Lab: compare Skia with and without HarfBuzz shaping. Tab/Enter operate controls. F12 saves PNG.");
    }

    private UiRect Pixels(UiRect rect)
    {
        var sx = (float)FramebufferSize.X / ClientSize.X;
        var sy = (float)FramebufferSize.Y / ClientSize.Y;
        var left = MathF.Round(rect.X * sx); var top = MathF.Round(rect.Y * sy);
        return new(left, top, MathF.Round(rect.Right * sx) - left, MathF.Round(rect.Bottom * sy) - top);
    }

    private void Rebuild()
    {
        foreach (var pane in panes) pane.Dispose();
        panes.Clear();
        layouts = PaneLayout.Arrange(ClientSize.X, ClientSize.Y, settings.Mode);
        var errors = new Dictionary<int, string>();
        var nativeScale = Math.Max((float)FramebufferSize.X / ClientSize.X, (float)FramebufferSize.Y / ClientSize.Y);
        foreach (var layout in layouts)
        {
            try { panes.Add(new(layout.Index, Pixels(layout.Sample), settings.Density * nativeScale, settings, assets, catalog)); }
            catch (Exception error) when (error is DllNotFoundException or BadImageFormatException or TypeInitializationException or IOException)
            {
                errors[layout.Index] = error.GetBaseException().Message;
                Console.Error.WriteLine($"{BackendCatalog.Name(layout.Index)} unavailable: {error}");
            }
        }
        if (NeedsChrome(errors.Count))
        {
            chrome?.Dispose();
            chrome = new(assets, fonts, settings, ClientSize.X, ClientSize.Y, layouts, errors, nativeScale, () => rebuild = true);
        }
        previousMode = settings.Mode; previousFramebuffer = FramebufferSize;
        previousScript = settings.Script;
        chrome!.Refresh(); rebuild = false;
    }

    private bool NeedsChrome(int errors) => chrome == null || settings.Mode != previousMode
        || settings.Script != previousScript
        || FramebufferSize != previousFramebuffer || errors > 0;

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        if (FramebufferSize.X <= 0 || FramebufferSize.Y <= 0) return;
        if (rebuild || FramebufferSize != previousFramebuffer) Rebuild();
        elapsed += args.Time; statisticsTime += args.Time;
        var time = launch.Capture == null ? (float)elapsed : 1;
        foreach (var pane in panes) pane.Render(painter, settings, time);
        RefreshStatistics();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        painter.Background(FramebufferSize.X, FramebufferSize.Y, 0, time);
        chrome!.Render(FramebufferSize.X, FramebufferSize.Y);
        foreach (var pane in panes)
        {
            painter.Image(FramebufferSize.X, FramebufferSize.Y, pane.Texture, pane.Bounds, new(0, 0, 1, 1));
            var zoom = Pixels(layouts.First(l => l.Index == pane.Index).Zoom);
            zoom = zoom with { Width = (int)zoom.Width / settings.Zoom * settings.Zoom, Height = (int)zoom.Height / settings.Zoom * settings.Zoom };
            painter.Image(FramebufferSize.X, FramebufferSize.Y, pane.Texture, zoom,
                pane.MagnifiedSource((int)zoom.Width, (int)zoom.Height, settings.Zoom));
        }
        if (capture || launch.Capture != null && frames == 30)
        {
            FrameCapture.Save(FramebufferSize.X, FramebufferSize.Y, launch.Capture);
            capture = false;
            if (launch.Capture != null) Close();
        }
        frames++; SwapBuffers();
    }

    private void RefreshStatistics()
    {
        if (statisticsTime > .25 || frames == 0)
        {
            foreach (var pane in panes) chrome!.Statistics(pane);
            statisticsTime = 0;
        }
    }

    protected override void OnResize(ResizeEventArgs args) { base.OnResize(args); rebuild = true; }
    protected override void OnMouseMove(MouseMoveEventArgs args) { base.OnMouseMove(args); chrome?.View.Input.PointerMove(args.X, args.Y); }
    protected override void OnMouseDown(MouseButtonEventArgs args)
    {
        base.OnMouseDown(args);
        if (args.Button == MouseButton.Left) chrome?.View.Input.PointerDown(MousePosition.X, MousePosition.Y);
    }
    protected override void OnMouseUp(MouseButtonEventArgs args)
    {
        base.OnMouseUp(args);
        if (args.Button == MouseButton.Left) chrome?.View.Input.PointerUp(MousePosition.X, MousePosition.Y);
    }
    protected override void OnFocusedChanged(FocusedChangedEventArgs args)
    {
        base.OnFocusedChanged(args); if (!args.IsFocused) chrome?.View.Input.Cancel();
    }
    protected override void OnKeyDown(KeyboardKeyEventArgs args)
    {
        base.OnKeyDown(args);
        if (Key(args.Key) is { } key && chrome?.View.Input.KeyDown(key, args.Shift, args.IsRepeat) == true) return;
        if (args.Key == Keys.Escape) Close();
        if (args.Key == Keys.F12) capture = true;
    }
    protected override void OnKeyUp(KeyboardKeyEventArgs args)
    {
        base.OnKeyUp(args); if (Key(args.Key) is { } key) chrome?.View.Input.KeyUp(key);
    }
    private static UiKey? Key(Keys key) => key switch
    {
        Keys.Tab => UiKey.Tab, Keys.Enter or Keys.KeyPadEnter => UiKey.Enter,
        Keys.Space => UiKey.Space, Keys.Escape => UiKey.Escape, _ => null
    };
    protected override void OnUnload()
    {
        foreach (var pane in panes) pane.Dispose();
        chrome?.Dispose(); painter?.Dispose(); fonts?.Dispose(); base.OnUnload();
    }
}
