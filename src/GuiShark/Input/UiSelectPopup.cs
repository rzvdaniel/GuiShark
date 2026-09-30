namespace GuiShark;

/// <summary>One view-level dropdown, drawn above ordinary content and its clips.</summary>
public sealed class UiSelectPopup
{
    private readonly UiView view;
    private int first;
    private int visibleCount;
    private float wheelRemainder;
    public UiRect ScrollIndicator { get; private set; }
    public UiElement? Owner { get; private set; }
    public UiRect Bounds { get; private set; }
    public int HighlightedIndex { get; private set; } = -1;
    public IReadOnlyList<UiElement> VisibleOptions { get; private set; } = [];
    public bool IsOpen => Owner != null;
    internal UiSelectPopup(UiView view) => this.view = view;

    public void Open(UiElement owner)
    {
        view.Update();
        if (owner.Select == null || !view.Document.Root.DescendantsAndSelf().Contains(owner))
            throw new ArgumentException("Dropdown must belong to this view.", nameof(owner));
        if (!HitTester.CanActivate(owner) || !view.Input.CanFocus(owner)) return;
        view.Tooltips.Hide();
        Close();
        Owner = owner;
        wheelRemainder = 0;
        HighlightedIndex = owner.Select!.SelectedIndex;
        if (!Available(HighlightedIndex)) HighlightedIndex = Next(-1, 1);
        first = Math.Max(0, HighlightedIndex - 3);
        Highlight(HighlightedIndex);
        view.Invalidate();
    }

    public void Close()
    {
        if (Owner == null) return;
        foreach (var option in Owner.Select!.Options) option.IsHovered = false;
        Owner = null;
        HighlightedIndex = -1;
        VisibleOptions = [];
        ScrollIndicator = default;
        view.Invalidate();
    }

    internal void Arrange()
    {
        if (Owner == null) return;
        if (!HitTester.CanActivate(Owner)) { Close(); return; }
        var rowHeight = Math.Max(32, Owner.Style.LineHeight + 16);
        var below = Math.Max(0, view.Height - Owner.Bounds.Bottom - 4);
        var above = Math.Max(0, Owner.Bounds.Y - 4);
        var down = below >= rowHeight * Math.Min(8, Owner.Select!.Options.Count) || below >= above;
        var room = down ? below : above;
        visibleCount = Math.Min(Owner.Select!.Options.Count, Math.Max(1, Math.Min(8, (int)(room / rowHeight))));
        first = Math.Clamp(first, 0, Math.Max(0, Owner.Select.Options.Count - visibleCount));
        var height = rowHeight * visibleCount;
        var width = Math.Min(view.Width, Owner.Bounds.Width);
        var x = Math.Clamp(Owner.Bounds.X, 0, Math.Max(0, view.Width - width));
        var y = down ? Owner.Bounds.Bottom + 4 : Owner.Bounds.Y - height - 4;
        Bounds = new UiRect(x, y, width, height).Intersect(new(0, 0, view.Width, view.Height));
        var thumbHeight = Owner.Select.Options.Count > 0 ? Bounds.Height * visibleCount / Owner.Select.Options.Count : 0;
        ScrollIndicator = Owner.Select.Options.Count > visibleCount
            ? new(Bounds.Right - 6, Bounds.Y + (Bounds.Height - thumbHeight) * first / (Owner.Select.Options.Count - visibleCount), 4, thumbHeight)
            : default;
        VisibleOptions = Owner.Select.Options.Skip(first).Take(visibleCount).ToArray();
        for (var i = 0; i < VisibleOptions.Count; i++)
        {
            var option = VisibleOptions[i];
            option.Bounds = new(x, y + i * rowHeight, width, rowHeight);
            option.Clip = option.Bounds.Intersect(Bounds);
        }
    }

    internal UiElement? Hit(float x, float y) => VisibleOptions.FirstOrDefault(e => e.Clip.Contains(x, y));
    internal void Hover(float x, float y)
    {
        var option = Hit(x, y);
        if (option != null && !option.Disabled && !option.Style.Hidden)
            Highlight(Owner!.Select!.Options.ToList().IndexOf(option));
    }
    internal void Commit(UiElement? option)
    {
        if (Owner == null || option == null || option.Disabled || option.Style.Hidden || !option.Style.PointerEvents) return;
        var select = Owner.Select!;
        var index = select.Options.ToList().IndexOf(option);
        Close();
        select.SelectedIndex = index;
    }
    internal bool Key(UiKey key)
    {
        if (!IsOpen) return false;
        if (key == UiKey.Escape) { Close(); return true; }
        if (key is UiKey.Enter or UiKey.Space)
        {
            if (Available(HighlightedIndex)) Commit(Owner!.Select!.Options[HighlightedIndex]);
            else Close();
            return true;
        }
        var index = key switch
        {
            UiKey.Down or UiKey.Right => Next(HighlightedIndex, 1),
            UiKey.Up or UiKey.Left => Next(HighlightedIndex, -1),
            UiKey.Home => Next(-1, 1),
            UiKey.End => Next(Owner!.Select!.Options.Count, -1),
            _ => HighlightedIndex
        };
        Highlight(index);
        if (index < first) first = Math.Max(0, index);
        if (index >= first + visibleCount) first = index - visibleCount + 1;
        view.Invalidate();
        return true;
    }
    internal void Wheel(float delta)
    {
        wheelRemainder += delta * 3;
        var rows = (int)wheelRemainder;
        wheelRemainder -= rows;
        first = Math.Clamp(first - rows, 0, Math.Max(0, Owner!.Select!.Options.Count - visibleCount));
        view.Invalidate();
    }
    private bool Available(int index) => Owner != null && index >= 0 && index < Owner.Select!.Options.Count &&
        !Owner.Select.Options[index].Disabled && !Owner.Select.Options[index].Style.Hidden && Owner.Select.Options[index].Style.PointerEvents;
    private int Next(int index, int direction)
    {
        for (var i = index + direction; i >= 0 && i < Owner!.Select!.Options.Count; i += direction)
            if (Available(i)) return i;
        return Available(index) ? index : -1;
    }
    private void Highlight(int index)
    {
        HighlightedIndex = index;
        if (Owner == null) return;
        for (var i = 0; i < Owner.Select!.Options.Count; i++) Owner.Select.Options[i].IsHovered = i == index;
        view.Invalidate();
    }
}
