namespace GuiShark;

/// <summary>Vertical scroll position and geometry in logical pixels.</summary>
public sealed class UiScroll
{
    private readonly UiElement element;
    private float offset;
    internal UiScroll(UiElement element) => this.element = element;
    public float Maximum { get; private set; }
    public float Offset
    {
        get => offset;
        set
        {
            if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            var next = Math.Clamp(value, 0, Maximum);
            if (next == offset) return;
            offset = next;
            element.Invalidate();
        }
    }
    public UiRect Track { get; private set; }
    public UiRect Thumb { get; private set; }
    internal void Arrange(float contentHeight)
    {
        var viewport = element.ContentBounds;
        Maximum = element.Style.ScrollY ? Math.Max(0, contentHeight - viewport.Height) : 0;
        offset = Math.Clamp(offset, 0, Maximum);
        Track = new(viewport.Right + 4, viewport.Y, 8, viewport.Height);
        var height = Maximum > 0 ? Math.Min(viewport.Height, Math.Max(24, viewport.Height * viewport.Height / contentHeight)) : viewport.Height;
        Thumb = new(Track.X, Track.Y + (Maximum > 0 ? offset / Maximum * (Track.Height - height) : 0), Track.Width, height);
    }
}
