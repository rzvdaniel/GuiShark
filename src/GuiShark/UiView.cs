namespace GuiShark;

/// <summary>Owns document layout and input. No window or graphics-context dependencies.</summary>
public sealed class UiView : IDisposable
{
    private LayoutEngine layout;
    internal ITextMetrics TextMetrics { get; private set; }
    private bool dirty = true;
    private bool disposed;
    public UiDocument Document { get; }
    public UiInput Input { get; }
    public UiSelectPopup Popup { get; }
    public UiModalLayer Modal { get; }
    public UiTooltipLayer Tooltips { get; }
    public float Width { get; private set; }
    public float Height { get; private set; }

    public UiView(UiDocument document, ITextMetrics textMetrics)
    {
        Document = document;
        TextMetrics = textMetrics;
        layout = new(textMetrics);
        Modal = new(this);
        Tooltips = new(this);
        Popup = new(this);
        Input = new(this);
        foreach (var element in document.Root.DescendantsAndSelf())
        {
            element.Changed += Invalidate;
            element.Dialog?.Attach(this);
        }
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

    /// <summary>Use the active renderer's metrics for wrapping and alignment.</summary>
    public void SetTextMetrics(ITextMetrics metrics)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        TextMetrics = metrics;
        layout = new(metrics);
        Invalidate();
    }

    public void Update()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        Tooltips.UpdateTimer();
        if (!dirty) return;
        foreach (var group in Document.TabGroups) group.Validate();
        Document.Styles.Resolve(Document.Root);
        layout.Layout(Document.Root, Width, Height);
        if (Modal.Active is { } dialog) layout.LayoutOverlay(dialog.Element, Width, Height);
        dirty = false;
        Modal.Validate();
        Input.ValidateTargets();
        foreach (var element in Document.Root.DescendantsAndSelf()) element.TextInput?.Arrange(TextMetrics);
        Popup.Arrange();
        Tooltips.Arrange(layout);
    }

    internal void Invalidate() => dirty = true;

    public void Dispose()
    {
        if (disposed) return;
        Modal.Clear();
        Input.Cancel();
        foreach (var element in Document.Root.DescendantsAndSelf())
        {
            element.Changed -= Invalidate;
            element.Dialog?.Attach(null);
        }
        disposed = true;
    }
}
