using System.Diagnostics;

namespace GuiShark;

/// <summary>Delayed, noninteractive HTML or title tooltips, positioned within the view.</summary>
public sealed class UiTooltipLayer
{
    private readonly UiView view;
    private readonly UiElement plain = new("div", "", "", "guishark-tooltip", false) { Role = "tooltip" };
    private UiElement? owner;
    private long started;
    private float x, y;
    private TimeSpan delay = TimeSpan.FromMilliseconds(450);
    public UiElement? VisibleElement { get; private set; }
    public TimeSpan Delay
    {
        get => delay;
        set { if (value < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(value)); delay = value; }
    }
    internal UiTooltipLayer(UiView view) => this.view = view;
    internal void Track(UiElement? hit, float pointerX, float pointerY)
    {
        var target = hit;
        while (target != null && target.TooltipTarget == null && string.IsNullOrWhiteSpace(target.TooltipText)) target = target.Parent;
        x = pointerX; y = pointerY;
        if (target != owner)
        {
            Hide();
            owner = target;
            started = Stopwatch.GetTimestamp();
        }
        if (VisibleElement != null) view.Invalidate();
    }
    public void Hide()
    {
        owner = null;
        if (VisibleElement == null) return;
        VisibleElement = null;
        view.Invalidate();
    }
    internal void UpdateTimer()
    {
        if (owner == null) return;
        if (view.Popup.IsOpen || !view.Modal.Contains(owner) || !owner.Clip.Contains(x, y) ||
            IsHidden(owner))
        {
            Hide();
            return;
        }
        if (VisibleElement == plain && plain.Text != owner.TooltipText)
        {
            plain.Text = owner.TooltipText ?? "";
            view.Invalidate();
        }
        if (owner.TooltipTarget == null && string.IsNullOrWhiteSpace(owner.TooltipText)) { Hide(); return; }
        if (VisibleElement != null || Stopwatch.GetElapsedTime(started) < Delay) return;
        if (owner.TooltipTarget is { } rich) VisibleElement = rich;
        else
        {
            plain.Parent = owner;
            plain.Text = owner.TooltipText ?? "";
            VisibleElement = plain;
        }
        view.Invalidate();
    }
    private static bool IsHidden(UiElement element)
    {
        for (var node = element; node != null; node = node.Parent)
            if (node.Hidden || node.Style.Hidden) return true;
        return false;
    }
    internal void Arrange(LayoutEngine layout)
    {
        if (VisibleElement == null || owner == null) return;
        if (VisibleElement == plain) view.Document.Styles.Resolve(plain, owner.Style);
        layout.LayoutOverlay(VisibleElement, view.Width, view.Height, new(x, y, 0, 0));
    }
}
