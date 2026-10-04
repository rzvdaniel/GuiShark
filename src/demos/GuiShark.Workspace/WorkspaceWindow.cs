using System.Diagnostics;
using GuiShark.OpenGL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace GuiShark.Workspace;

internal sealed class WorkspaceWindow(WorkspaceOptions options) : GameWindow(new GameWindowSettings { UpdateFrequency = 60 },
    new NativeWindowSettings { ClientSize = new Vector2i(1440, 960), MinimumClientSize = new Vector2i(1060, 650),
        Title = "GuiShark Workspace", APIVersion = new Version(3, 3), Profile = ContextProfile.Core, Flags = ContextFlags.ForwardCompatible })
{
    private readonly WorkspaceLayout layout = new();
    private readonly PaneSessions sessions = new(options.SharedFrames, options.Fps);
    private FontBook fonts = null!;
    private WorkspaceChrome chrome = null!;
    private WorkspaceController controller = null!;
    private WorkspaceStore store = null!;
    private WorkspaceInput input = null!;
    private WorkspaceVerification? verification;
    private WorkspaceBenchmark? benchmark;
    private double elapsed;
    private double lastSave;
    public long Frames { get; private set; }

    protected override void OnLoad()
    {
        base.OnLoad();
        VSync = VSyncMode.On;
        store = new WorkspaceStore(options.StatePath);
        var state = store.Load(() => WorkspaceDefaults.Create(options));
        controller = new WorkspaceController(state, options);
        if (store.RestoreError is not null) controller.Notice = store.RestoreError;
        var assetsPath = Path.Combine(AppContext.BaseDirectory, "Assets");
        fonts = new FontBook(Path.Combine(assetsPath, "fonts/Lato-Regular.ttf"), Path.Combine(assetsPath, "fonts/Lato-Bold.ttf"));
        chrome = new WorkspaceChrome(fonts, new DirectoryAssetSource(assetsPath));
        Rebuild();
        input = new WorkspaceInput(controller, layout, chrome, sessions.Items, SessionCommand);
        sessions.Synchronize(controller.State);
        if (options.Benchmark) benchmark = new WorkspaceBenchmark(options.SharedFrames, options.Fps, Close);
        if (options.Verify) verification = new WorkspaceVerification(controller, layout, sessions.Items, store, input, chrome, Close);
        Console.WriteLine($"Workspace layout: {store.Path}. Close the window to stop its apps.");
    }
    private void Rebuild()
    {
        layout.Arrange(controller.State.ActiveTab, ClientSize.X, ClientSize.Y);
        chrome.Rebuild(new ChromeMarkup(controller, layout, SessionCommand, sessions.Items), ClientSize.X, ClientSize.Y, controller);
        controller.Dirty = false;
    }
    private void SessionCommand(string command)
    {
        if (command == "confirm")
        {
            controller.Accept(chrome.View.Document.GetElement("prompt-value").TextInput!.Value);
            return;
        }
        sessions.Command(controller.State.ActivePane.Id, command);
    }
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        var frameStarted = Stopwatch.GetTimestamp();
        elapsed += args.Time;
        Frames++;
        if (controller.Dirty) { sessions.Synchronize(controller.State); Rebuild(); }
        sessions.SetVisible(layout);
        if (sessions.Update()) Rebuild();
        chrome.UpdateStatus(layout, sessions.Items, controller);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        GL.ClearColor(.06f, .09f, .14f, 1);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        chrome.Render(FramebufferSize.X, FramebufferSize.Y);
        sessions.Render(layout, ClientSize, FramebufferSize);
        chrome.RenderOverlay(FramebufferSize.X, FramebufferSize.Y);
        var workMilliseconds = Stopwatch.GetElapsedTime(frameStarted).TotalMilliseconds;
        SwapBuffers();
        benchmark?.Record(workMilliseconds, elapsed, sessions.Items);
        SaveIfNeeded();
        verification?.Update(elapsed, Frames);
        if (options.Capture && elapsed > 3 && sessions.AllHaveFrames)
        { WorkspaceCapture.Save(FramebufferSize.X, FramebufferSize.Y); Close(); }
        if (options.Capture && elapsed > 20) throw new InvalidOperationException("Timed out waiting for app frames.");
    }
    private void SaveIfNeeded()
    {
        if (!controller.NeedsSave || elapsed - lastSave < 1) return;
        Save();
        lastSave = elapsed;
    }
    private void Save()
    {
        try { store.Save(controller.State); controller.NeedsSave = false; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { controller.Notice = $"Cannot save layout: {error.Message}"; }
    }
    protected override void OnResize(ResizeEventArgs e) { base.OnResize(e); if (controller is not null) controller.Dirty = true; }
    protected override void OnMouseMove(MouseMoveEventArgs e) { base.OnMouseMove(e); input?.PointerMove(e.X, e.Y); }
    protected override void OnMouseDown(MouseButtonEventArgs e)
    { base.OnMouseDown(e); input?.PointerDown(MousePosition.X, MousePosition.Y, e.Button, ShiftDown()); }
    protected override void OnMouseUp(MouseButtonEventArgs e)
    { base.OnMouseUp(e); input?.PointerUp(MousePosition.X, MousePosition.Y, e.Button, ShiftDown()); }
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    { base.OnMouseWheel(e); input?.Wheel(MousePosition.X, MousePosition.Y, e.OffsetY); }
    private bool ShiftDown() => KeyboardState.IsKeyDown(Keys.LeftShift) || KeyboardState.IsKeyDown(Keys.RightShift);
    protected override void OnTextInput(TextInputEventArgs e) { base.OnTextInput(e); input?.Text(e.AsString); }
    protected override void OnKeyDown(KeyboardKeyEventArgs e) { base.OnKeyDown(e); input?.KeyDown(e); }
    protected override void OnKeyUp(KeyboardKeyEventArgs e) { base.OnKeyUp(e); input?.KeyUp(e.Key); }
    protected override void OnFocusedChanged(FocusedChangedEventArgs e)
    { base.OnFocusedChanged(e); if (!e.IsFocused) input?.ReleaseFocus(); }
    protected override void OnUnload()
    {
        if (controller is not null) Save();
        sessions.Dispose();
        chrome?.Dispose();
        fonts?.Dispose();
        if (options.Verify || options.Capture || options.Benchmark) File.Delete(options.StatePath);
        base.OnUnload();
    }
}
