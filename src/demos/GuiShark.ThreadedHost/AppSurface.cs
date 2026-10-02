using GuiShark.OpenGL;
using OpenTK.Graphics.OpenGL4;

namespace GuiShark.ThreadedHost;

// All methods run on the app's GL thread. Resize replaces only its own targets.
internal sealed class AppSurface : IDisposable
{
    private readonly UiView view;
    private readonly OpenGlUiRenderer renderer;
    private readonly SharedFrames shared;
    private WorkerTargets targets;

    public AppSize Size { get; private set; }

    public AppSurface(UiView view, OpenGlUiRenderer renderer, SharedFrames shared, AppSize size)
    {
        this.view = view;
        this.renderer = renderer;
        this.shared = shared;
        Size = size;
        view.Resize(size.LogicalWidth, size.LogicalHeight);
        targets = new WorkerTargets(shared, size);
    }

    public void Resize(AppSize next)
    {
        if (Size == next) return;
        targets.Dispose();
        view.Resize(next.LogicalWidth, next.LogicalHeight);
        targets = new WorkerTargets(shared, next);
        Size = next;
        Console.WriteLine($"App target: {next.PixelWidth}x{next.PixelHeight} pixels for {next.LogicalWidth:F0}x{next.LogicalHeight:F0} UI units");
    }

    public bool Render()
    {
        var slot = shared.AcquireWriteSlot();
        if (slot < 0) return false;
        targets.Bind(slot);
        GL.Viewport(0, 0, Size.PixelWidth, Size.PixelHeight);
        GL.ClearColor(.06f, .20f, .31f, 1);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        renderer.Render(Size.PixelWidth, Size.PixelHeight);
        GL.Finish(); // The host sees only completed GPU work.
        shared.Publish(slot);
        return true;
    }

    public void Dispose() => targets.Dispose();
}
