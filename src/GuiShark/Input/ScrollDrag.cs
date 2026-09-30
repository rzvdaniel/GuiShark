namespace GuiShark;

internal sealed class ScrollDrag
{
    private UiScroll? scroll;
    private UiElement? owner;
    private float grab;
    public bool Active => scroll != null;
    public bool IsVisible
    {
        get
        {
            for (var node = owner; node != null; node = node.Parent)
                if (node.Style.Hidden || node.Disabled) return false;
            return owner?.Style.ScrollY == true && owner.Clip.Width > 0 && owner.Clip.Height > 0;
        }
    }
    public bool Begin(UiElement? hit, float x, float y)
    {
        for (var node = hit; node != null; node = node.Parent)
        {
            if (!node.Style.ScrollY || node.Scroll.Maximum <= 0 || !node.Scroll.Track.Contains(x, y)) continue;
            owner = node;
            scroll = node.Scroll;
            grab = scroll.Thumb.Contains(x, y) ? y - scroll.Thumb.Y : scroll.Thumb.Height / 2;
            Move(y);
            return true;
        }
        return false;
    }
    public void Move(float y)
    {
        if (scroll == null) return;
        var distance = scroll.Track.Height - scroll.Thumb.Height;
        if (distance > 0) scroll.Offset = (y - scroll.Track.Y - grab) / distance * scroll.Maximum;
    }
    public void Cancel() { scroll = null; owner = null; }
}
