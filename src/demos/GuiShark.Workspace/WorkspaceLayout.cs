namespace GuiShark.Workspace;

internal sealed record PanePlacement(WorkspacePane Pane, UiRect Bounds)
{
    public UiRect Content => new(Bounds.X + 2, Bounds.Y + 30, Math.Max(0, Bounds.Width - 4), Math.Max(0, Bounds.Height - 54));
}
internal sealed record SplitPlacement(LayoutNode Node, UiRect Bounds, UiRect Divider);

internal sealed class WorkspaceLayout
{
    public List<PanePlacement> Panes { get; } = [];
    public List<SplitPlacement> Splits { get; } = [];
    public void Arrange(WorkspaceTab tab, float width, float height)
    {
        Panes.Clear();
        Splits.Clear();
        var bounds = new UiRect(212, 104, Math.Max(0, width - 224), Math.Max(0, height - 134));
        if (tab.ZoomedPaneId is { } zoom)
        {
            var pane = tab.Root.Panes().First(pane => pane.Id == zoom);
            Panes.Add(new(pane, bounds));
        }
        else ArrangeNode(tab.Root, bounds);
    }
    private void ArrangeNode(LayoutNode node, UiRect bounds)
    {
        if (node.Pane is { } pane) { Panes.Add(new(pane, bounds)); return; }
        const float gap = 6;
        var length = Math.Max(0, (node.Down ? bounds.Height : bounds.Width) - gap);
        var size = length * node.Ratio;
        UiRect first;
        UiRect second;
        UiRect divider;
        if (node.Down)
        {
            first = new(bounds.X, bounds.Y, bounds.Width, size);
            divider = new(bounds.X, bounds.Y + size, bounds.Width, gap);
            second = new(bounds.X, divider.Bottom, bounds.Width, length - size);
        }
        else
        {
            first = new(bounds.X, bounds.Y, size, bounds.Height);
            divider = new(bounds.X + size, bounds.Y, gap, bounds.Height);
            second = new(divider.Right, bounds.Y, length - size, bounds.Height);
        }
        Splits.Add(new(node, bounds, divider));
        ArrangeNode(node.First!, first);
        ArrangeNode(node.Second!, second);
    }
    public static void Resize(SplitPlacement split, float x, float y)
    {
        var length = Math.Max(1, (split.Node.Down ? split.Bounds.Height : split.Bounds.Width) - 6);
        var offset = split.Node.Down ? y - split.Bounds.Y : x - split.Bounds.X;
        // Ratios keep both branches reachable even when the window is unusually small.
        var minimum = Math.Min(.4f, 120 / length);
        split.Node.Ratio = Math.Clamp(offset / length, minimum, 1 - minimum);
    }
}
