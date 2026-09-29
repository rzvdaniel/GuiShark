namespace GuiShark;

/// <summary>Owns document layout and input. No window or graphics-context dependencies.</summary>
public sealed class UiView : IDisposable
{
    private readonly LayoutEngine layout;
    private bool dirty = true;
    private bool disposed;
    public UiDocument Document { get; }
    public UiInput Input { get; }
    public float Width { get; private set; }
    public float Height { get; private set; }

    public UiView(UiDocument document, ITextMetrics textMetrics)
    {
        Document = document;
        layout = new(textMetrics);
        Input = new(this);
        foreach (var element in document.Root.DescendantsAndSelf()) element.Changed += Invalidate;
    }

    public void Resize(float width, float height)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!float.IsFinite(width) || !float.IsFinite(height) || width < 0 || height < 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        Width = width;
        Height = height;
        Invalidate();
    }

    public void Update()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!dirty) return;
        Document.Styles.Resolve(Document.Root);
        layout.Layout(Document.Root, Width, Height);
        dirty = false;
        Input.ValidateTargets();
    }

    internal void Invalidate() => dirty = true;

    public void Dispose()
    {
        if (disposed) return;
        Input.Cancel();
        foreach (var element in Document.Root.DescendantsAndSelf()) element.Changed -= Invalidate;
        disposed = true;
    }
}
