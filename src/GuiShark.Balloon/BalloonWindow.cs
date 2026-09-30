using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;

namespace GuiShark.Balloon;

/// <summary>Host application: owns context, simulation and input routing; draws the UI last.</summary>
internal sealed class BalloonWindow : GameWindow
{
    private readonly LaunchOptions options;
    private readonly Expedition expedition = new();
    private readonly FollowCamera camera = new();
    private WorldRenderer world = null!;
    private BalloonInterface ui = null!;
    private GameMode mode;
    private bool capture;
    private int frames;

    public BalloonWindow(LaunchOptions options) : base(new GameWindowSettings { UpdateFrequency = 60 }, new NativeWindowSettings
    {
        ClientSize = new Vector2i(1200, 820),
        MinimumClientSize = new Vector2i(920, 680),
        Title = "Lantern Valley · GuiShark OpenGL game",
        APIVersion = new Version(3, 3),
        Profile = ContextProfile.Core,
        Flags = ContextFlags.ForwardCompatible
    }) => this.options = options;

    protected override void OnLoad()
    {
        base.OnLoad();
        VSync = VSyncMode.On;
        Console.WriteLine($"OpenGL {GL.GetString(StringName.Version)} / {GL.GetString(StringName.Renderer)}");
        world = new(expedition.Terrain);
        ui = new(options.AssetsPath, Transition, Restart, expedition.GuideToNext, Close);
        ui.Resize(ClientSize.X, ClientSize.Y);
        camera.Reset(expedition.Flight.Position);
        Transition(options.Play ? GameMode.Flying : GameMode.Menu);
        camera.Update(expedition.Flight.Position, 1, ClientSize.X / (float)ClientSize.Y);
    }

    private void Transition(GameMode next)
    {
        if (next == GameMode.Flying && mode == GameMode.Menu)
        {
            expedition.Reset();
            camera.Reset(expedition.Flight.Position);
        }
        mode = next;
        ui.SetMode(mode);
    }

    private void Restart()
    {
        expedition.Reset();
        camera.Reset(expedition.Flight.Position);
        mode = GameMode.Flying;
        ui.SetMode(mode);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        var dt = Math.Min((float)args.Time, .05f);
        if (mode == GameMode.Flying)
        {
            expedition.Update(dt);
            if (expedition.Complete) Transition(GameMode.Complete);
        }
        camera.Update(expedition.Flight.Position, dt, ClientSize.X / (float)Math.Max(1, ClientSize.Y));
        ui.Update(expedition, dt);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        world.Render(expedition, camera, FramebufferSize.X, FramebufferSize.Y);
        ui.Render(FramebufferSize.X, FramebufferSize.Y);
        if (capture)
        {
            capture = false;
            FrameCapture.Save(FramebufferSize.X, FramebufferSize.Y);
        }
        if (++frames == 3 && options.CapturePath != null)
        {
            FrameCapture.Save(FramebufferSize.X, FramebufferSize.Y, options.CapturePath);
            Close();
        }
        SwapBuffers();
    }

    protected override void OnMouseDown(MouseButtonEventArgs args)
    {
        base.OnMouseDown(args);
        if (args.Button != MouseButton.Left || ui.PointerDown(MousePosition.X, MousePosition.Y)) return;
        if (mode == GameMode.Flying && camera.PickTerrain(MousePosition.X, MousePosition.Y, ClientSize.X, ClientSize.Y, expedition.Terrain) is { } target)
        {
            expedition.Flight.SetDestination(target);
            Console.WriteLine($"Terrain course: {target.X:0.0}, {target.Y:0.0}");
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs args)
    {
        base.OnMouseUp(args);
        if (args.Button == MouseButton.Left) ui.PointerUp(MousePosition.X, MousePosition.Y);
    }

    protected override void OnMouseMove(MouseMoveEventArgs args)
    {
        base.OnMouseMove(args);
        ui?.PointerMove(args.X, args.Y);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs args)
    {
        base.OnMouseWheel(args);
        if (mode == GameMode.Flying && !ui.PointerMove(MousePosition.X, MousePosition.Y))
            camera.AdjustZoom(args.OffsetY);
    }

    protected override void OnResize(ResizeEventArgs args)
    {
        base.OnResize(args);
        ui?.Resize(ClientSize.X, ClientSize.Y);
    }

    protected override void OnFocusedChanged(FocusedChangedEventArgs args)
    {
        base.OnFocusedChanged(args);
        if (args.IsFocused) return;
        ui?.Cancel();
        if (ui != null && mode == GameMode.Flying) Transition(GameMode.Paused);
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs args)
    {
        base.OnKeyDown(args);
        if (args.Key == Keys.F12)
        {
            capture = true;
            return;
        }
        if (args.Key == Keys.Escape && !args.IsRepeat)
        {
            if (mode == GameMode.Flying) Transition(GameMode.Paused);
            else if (mode == GameMode.Paused) Transition(GameMode.Flying);
            return;
        }
        if (MapKey(args.Key) is { } key) ui.KeyDown(key, args.Shift, args.IsRepeat);
    }
    protected override void OnKeyUp(KeyboardKeyEventArgs args)
    {
        base.OnKeyUp(args);
        if (MapKey(args.Key) is { } key) ui.KeyUp(key);
    }

    private static UiKey? MapKey(Keys key) => key switch
    {
        Keys.Tab => UiKey.Tab,
        Keys.Enter or Keys.KeyPadEnter => UiKey.Enter,
        Keys.Space => UiKey.Space,
        _ => null
    };

    protected override void OnUnload()
    {
        ui?.Dispose();
        world?.Dispose();
        base.OnUnload();
    }
}
