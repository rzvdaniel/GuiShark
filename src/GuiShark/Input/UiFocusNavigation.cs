namespace GuiShark;

/// <summary>Focus traversal and visibility, separate from pointer/key capture.</summary>
internal sealed class UiFocusNavigation(UiView view, Func<UiElement?> focused, Action<UiElement?> setFocus)
{
    private UiElement? Focused => focused();
    public bool AdvanceTab(UiKey key)
    {
        if (Focused?.TabGroup is not { } group || key is not (UiKey.Left or UiKey.Right or UiKey.Home or UiKey.End)) return false;
        var tabs = group.Tabs.Where(view.Input.CanFocus).ToList();
        if (tabs.Count == 0) return false;
        var index = tabs.IndexOf(Focused);
        index = key switch { UiKey.Home => 0, UiKey.End => tabs.Count - 1, UiKey.Left => (index + tabs.Count - 1) % tabs.Count, _ => (index + 1) % tabs.Count };
        group.Select(tabs[index]);
        setFocus(tabs[index]);
        return true;
    }

    public bool ScrollPage(UiKey key)
    {
        if (key is not (UiKey.PageUp or UiKey.PageDown)) return false;
        for (var node = Focused; node != null; node = node.Parent)
            if (node.Style.ScrollY)
            {
                node.Scroll.Offset += node.ContentBounds.Height * .9f * (key == UiKey.PageUp ? -1 : 1);
                return true;
            }
        return false;
    }

    public void Reveal(UiElement element)
    {
        view.Update();
        for (var node = element.Parent; node != null; node = node.Parent)
        {
            if (!node.Style.ScrollY) continue;
            var viewport = node.ContentBounds;
            if (element.Bounds.Y < viewport.Y) node.Scroll.Offset -= viewport.Y - element.Bounds.Y;
            else if (element.Bounds.Bottom > viewport.Bottom) node.Scroll.Offset += element.Bounds.Bottom - viewport.Bottom;
            view.Update();
        }
    }

    public bool AdvanceRadio(UiKey key)
    {
        if (Focused?.Control is not { Kind: UiControlKind.Radio } control || control.Name.Length == 0 ||
            key is not (UiKey.Left or UiKey.Right or UiKey.Up or UiKey.Down)) return false;
        var group = view.Document.Root.DescendantsAndSelf().Where(e =>
            e.Control is { Kind: UiControlKind.Radio } radio && radio.Name == control.Name && view.Input.CanFocus(e)).ToList();
        if (group.Count == 0) return false;
        var offset = key is UiKey.Left or UiKey.Up ? group.Count - 1 : 1;
        setFocus(group[(group.IndexOf(Focused) + offset) % group.Count]);
        Focused!.Activate();
        return true;
    }

    public void AdvanceFocus(bool backwards)
    {
        var controls = view.Document.Root.DescendantsAndSelf().Where(e => view.Input.CanFocus(e) && (e.TabGroup == null || e.IsSelected)).ToList();
        if (controls.Count == 0) { setFocus(null); return; }
        var index = Focused == null ? (backwards ? 0 : -1) : controls.IndexOf(Focused);
        setFocus(controls[(index + (backwards ? controls.Count - 1 : 1)) % controls.Count]);
    }

}
