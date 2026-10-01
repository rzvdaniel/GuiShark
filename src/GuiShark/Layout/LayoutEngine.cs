namespace GuiShark;

internal sealed class LayoutEngine(ITextMetrics text)
{
    private readonly record struct Size(float Width, float Height);

    public void Layout(UiElement root, float width, float height) =>
        Arrange(root, new(0, 0, width, height), new(0, 0, width, height));

    public void LayoutOverlay(UiElement element, float width, float height, UiRect? anchor = null)
    {
        var viewport = new UiRect(8, 8, Math.Max(0, width - 16), Math.Max(0, height - 16));
        var size = Measure(element, viewport.Width, viewport.Height, false);
        size = new(Math.Min(size.Width, viewport.Width), Math.Min(size.Height, viewport.Height));
        var x = anchor?.X + 16 ?? viewport.X + (viewport.Width - size.Width) / 2;
        var y = anchor?.Bottom + 20 ?? viewport.Y + (viewport.Height - size.Height) / 2;
        if (anchor is { } point && y + size.Height > viewport.Bottom) y = point.Y - size.Height - 12;
        x = Math.Clamp(x, viewport.X, Math.Max(viewport.X, viewport.Right - size.Width));
        y = Math.Clamp(y, viewport.Y, Math.Max(viewport.Y, viewport.Bottom - size.Height));
        Arrange(element, new(x, y, size.Width, size.Height), viewport);
    }

    private Size Measure(UiElement element, float availableWidth, float availableHeight, bool stretch, float? forcedWidth = null)
    {
        var s = element.Style;
        var width = forcedWidth ?? s.Width.Resolve(availableWidth, stretch ? availableWidth : NaturalWidth(element, availableWidth));
        width = Math.Clamp(width, 0, Math.Max(0, s.MaxWidth.Resolve(availableWidth, availableWidth)));
        var innerWidth = Math.Max(0, width - s.Padding.Horizontal - 2 * s.BorderWidth - (s.ScrollY ? 12 : 0));
        var children = VisibleChildren(element);
        var height = element.TextInput != null ? s.LineHeight * (element.TextInput.IsMultiline ? element.TextInput.Rows : 1) : TextLayout.Wrap(element.Text, innerWidth, s, text).Count * s.LineHeight;
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
        var width = text.MeasureWidth(element.Text, s.FontSize, s.Bold, s.FontFamily);
        if (children.Length > 0)
        {
            var widths = children.Select(c => c.Style.Width.Resolve(available, NaturalWidth(c, available)) + c.Style.Margin.Horizontal);
            width = s.Direction == FlowDirection.Row ? widths.Sum() + s.Gap * (children.Length - 1) : widths.Max();
        }
        if (element.Tag == "img") width = 48;
        if (element.TextInput != null) width = 200;
        if (element.Control?.Kind is UiControlKind.Range or UiControlKind.Progress) width = 160;
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
        var contentHeight = row ? sizes.Select((size, i) => size.Height + children[i].Style.Margin.Vertical).DefaultIfEmpty(0).Max()
            : occupied + gap * Math.Max(0, children.Length - 1);
        if (element.TextInput == null) element.Scroll.Arrange(contentHeight);
        var cursor = (row ? content.X : content.Y - element.Scroll.Offset) + offset;
        for (var i = 0; i < children.Length; i++)
        {
            var child = children[i];
            ArrangeFlowChild(element, child, sizes[i], content, row, cursor);
            cursor += (row ? sizes[i].Width + child.Style.Margin.Horizontal : sizes[i].Height + child.Style.Margin.Vertical) + gap;
        }
        foreach (var child in element.Children.Where(c => !c.IsOverlay && !c.Style.Hidden && c.Style.Position == ElementPosition.Absolute))
            ArrangeAbsolute(child, content, element.Clip.Intersect(content));
    }

    private void ArrangeFlowChild(UiElement element, UiElement child, Size size, UiRect content, bool row, float cursor)
    {
        var margin = child.Style.Margin;
        if (row && element.Style.Align == CrossAlignment.Stretch && child.Style.Height.IsAuto)
            size = size with { Height = Math.Max(0, content.Height - margin.Vertical) };
        var crossFree = row ? content.Height - size.Height - margin.Vertical : content.Width - size.Width - margin.Horizontal;
        var cross = element.Style.Align switch { CrossAlignment.Center => crossFree / 2, CrossAlignment.End => crossFree, _ => 0 };
        var x = row ? cursor + margin.Left : content.X + margin.Left + cross;
        var y = row ? content.Y + margin.Top + cross : cursor + margin.Top;
        Arrange(child, new(x, y, size.Width, size.Height), element.Clip.Intersect(content));
    }

    private void ArrangeAbsolute(UiElement element, UiRect parent, UiRect clip)
    {
        var style = element.Style;
        var left = style.Left.Resolve(parent.Width, 0);
        var right = style.Right.Resolve(parent.Width, 0);
        float? width = style.Width.IsAuto && !style.Left.IsAuto && !style.Right.IsAuto
            ? Math.Max(0, parent.Width - left - right) : null;
        var size = Measure(element, parent.Width, parent.Height, false, width);
        var top = style.Top.Resolve(parent.Height, 0);
        var bottom = style.Bottom.Resolve(parent.Height, 0);
        if (style.Height.IsAuto && !style.Top.IsAuto && !style.Bottom.IsAuto)
            size = size with { Height = Math.Max(0, parent.Height - top - bottom) };
        var x = style.Left.IsAuto && !style.Right.IsAuto ? parent.Right - right - size.Width : parent.X + left;
        var y = style.Top.IsAuto && !style.Bottom.IsAuto ? parent.Bottom - bottom - size.Height : parent.Y + top;
        Arrange(element, new(x, y, size.Width, size.Height), clip);
    }

    private static UiElement[] VisibleChildren(UiElement element) => (element.Select != null ? Array.Empty<UiElement>() : element.Children)
        .Where(c => !c.IsOverlay && !c.Style.Hidden && c.Style.Position == ElementPosition.Flow).ToArray();
}
