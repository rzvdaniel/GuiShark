using System.Diagnostics;
using GuiShark.OpenGL;

namespace GuiShark.TextDemo;

internal sealed class TextPane : IDisposable
{
    private readonly UiView view;
    private readonly OpenGlUiRenderer renderer;
    private readonly RenderTarget target;
    private readonly float density;
    public int Index { get; }
    public UiRect Bounds { get; }
    public int Texture => target.Texture;
    public double Milliseconds { get; private set; }
    public TextRenderStatistics Statistics => renderer.TextStatistics;

    public TextPane(int index, UiRect bounds, float density, LabSettings settings, IAssetSource assets, BackendCatalog catalog)
    {
        Index = index; Bounds = bounds; this.density = density;
        var backend = catalog.Create(index);
        try
        {
            var document = SampleDocument.Create(assets, settings, density, bounds.Height / density);
            view = new(document, backend);
            view.Resize(bounds.Width / density, bounds.Height / density);
            renderer = new(view, backend, true) { TextOptions = settings.TextOptions };
            target = new((int)bounds.Width, (int)bounds.Height);
        }
        catch { backend.Dispose(); throw; }
    }
    public void Render(ScreenPainter painter, LabSettings settings, float time)
    {
        target.Bind();
        painter.Background(target.Width, target.Height, settings.Background, time);
        var start = Stopwatch.GetTimestamp();
        renderer.Render(target.Width, target.Height);
        Milliseconds = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    }
    public UiRect MagnifiedSource(int destinationWidth, int destinationHeight, int zoom)
    {
        var element = view.Document.GetElement("inspect");
        var inspect = element.Bounds;
        var cropHeight = destinationHeight / zoom;
        var y = Math.Max(0, inspect.Y * density + element.Style.LineHeight * density / 2 - cropHeight / 2f);
        // Sample integer physical pixels and enlarge them with nearest filtering.
        return new(MathF.Floor(inspect.X * density) / target.Width, MathF.Floor(y) / target.Height,
            (float)(destinationWidth / zoom) / target.Width, (float)(destinationHeight / zoom) / target.Height);
    }
    public void Dispose() { renderer.Dispose(); view.Dispose(); target.Dispose(); }
}
