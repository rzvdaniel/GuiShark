namespace GuiShark;

internal sealed class LayoutEngine(ITextMetrics text)
{
    private readonly record struct Size(float Width, float Height);

    public void Layout(UiElement root, float width, float height) =>
        Arrange(root, new(0, 0, width, height), new(0, 0, width, height));

    private Size Measure(UiElement element, float availableWidth, float availableHeight, bool stretch, float? forcedWidth = null)
    {
        var s = element.Style;
        var width = forcedWidth ?? s.Width.Resolve(availableWidth, stretch ? availableWidth : NaturalWidth(element, availableWidth));
        width = Math.Clamp(width, 0, Math.Max(0, s.MaxWidth.Resolve(availableWidth, availableWidth)));
        var innerWidth = Math.Max(0, width - s.Padding.Horizontal - 2 * s.BorderWidth);
        var children = VisibleChildren(element);
        var height = TextLayout.Wrap(element.Text, innerWidth, s, text).Count * s.LineHeight;
        if (children.Length > 0)
        {
            var sizes = MeasureChildren(element, innerWidth, availableHeight);
            height = s.Direction == FlowDirection.Row
                ? sizes.Select((size, i) => size.Height + children[i].Style.Margin.Vertical).Max()
                : sizes.Select((size, i) => size.Height + children[i].Style.Margin.Vertical).Sum() + s.Gap * (children.Length - 1);
        }
        if (element.Tag == "img") height = 48;
        return new(width, Math.Max(0, s.Height.Resolve(availableHeight, height + s.Padding.Vertical + 2 * s.BorderWidth)));
    }

    private float NaturalWidth(UiElement element, float available)
    {
        var s = element.Style;
        var children = VisibleChildren(element);
        var width = text.MeasureWidth(element.Text, s.FontSize, s.Bold);
        if (children.Length > 0)
        {
            var widths = children.Select(c => c.Style.Width.Resolve(available, NaturalWidth(c, available)) + c.Style.Margin.Horizontal);
            width = s.Direction == FlowDirection.Row ? widths.Sum() + s.Gap * (children.Length - 1) : widths.Max();
        }
        if (element.Tag == "img") width = 48;
        return width + s.Padding.Horizontal + s.BorderWidth * 2;
    }

    private Size[] MeasureChildren(UiElement parent, float width, float height)
    {
        var children = VisibleChildren(parent);
        var row = parent.Style.Direction == FlowDirection.Row;
        var sizes = children.Select(c => Measure(c, Math.Max(0, width - c.Style.Margin.Horizontal), height,
            !row && parent.Style.Align == CrossAlignment.Stretch)).ToArray();
        if (!row) return sizes;
        var occupied = sizes.Select((size, i) => size.Width + children[i].Style.Margin.Horizontal).Sum();
        var remaining = Math.Max(0, width - occupied - parent.Style.Gap * Math.Max(0, children.Length - 1));
        var grow = children.Sum(c => c.Style.Grow);
        if (grow > 0)
            for (var i = 0; i < children.Length; i++)
                sizes[i] = Measure(children[i], width, height, false, sizes[i].Width + remaining * children[i].Style.Grow / grow);
        return sizes;
    }

    private void Arrange(UiElement element, UiRect bounds, UiRect parentClip)
    {
        element.Bounds = bounds;
        element.Clip = bounds.Intersect(parentClip);
        if (element.Style.Hidden) { element.Clip = default; return; }
        var content = element.ContentBounds;
        var children = VisibleChildren(element);
        var sizes = MeasureChildren(element, content.Width, content.Height);
        var row = element.Style.Direction == FlowDirection.Row;
        var occupied = sizes.Select((size, i) => row ? size.Width + children[i].Style.Margin.Horizontal : size.Height + children[i].Style.Margin.Vertical).Sum();
        var gap = element.Style.Gap;
        var free = Math.Max(0, (row ? content.Width : content.Height) - occupied - gap * Math.Max(0, children.Length - 1));
        var offset = element.Style.Justify switch { MainAlignment.Center => free / 2, MainAlignment.End => free, _ => 0 };
        if (element.Style.Justify == MainAlignment.SpaceBetween && children.Length > 1) gap += free / (children.Length - 1);
        var cursor = (row ? content.X : content.Y) + offset;
        for (var i = 0; i < children.Length; i++)
        {
            var child = children[i];
            var margin = child.Style.Margin;
            var size = sizes[i];
            if (row && element.Style.Align == CrossAlignment.Stretch && child.Style.Height.IsAuto)
                size = size with { Height = Math.Max(0, content.Height - margin.Vertical) };
            var crossFree = row ? content.Height - size.Height - margin.Vertical : content.Width - size.Width - margin.Horizontal;
            var cross = element.Style.Align switch { CrossAlignment.Center => crossFree / 2, CrossAlignment.End => crossFree, _ => 0 };
            var x = row ? cursor + margin.Left : content.X + margin.Left + cross;
            var y = row ? content.Y + margin.Top + cross : cursor + margin.Top;
            Arrange(child, new(x, y, size.Width, size.Height), element.Clip.Intersect(content));
            cursor += (row ? size.Width + margin.Horizontal : size.Height + margin.Vertical) + gap;
        }
    }

    private static UiElement[] VisibleChildren(UiElement element) => element.Children.Where(c => !c.Style.Hidden).ToArray();
}
