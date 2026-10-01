namespace GuiShark;

internal readonly record struct TextEditPosition(int Index, bool Upstream);

/// <summary>Editor coordinates, wrapping and caret visibility, without GPU dependencies.</summary>
internal sealed class TextInputGeometry(UiElement owner, UiTextInput input)
{
    private readonly TextEditLayout layout = new();
    private float horizontal;
    public UiRect TextBounds { get; private set; }
    public UiRect CaretBounds { get; private set; }
    public IReadOnlyList<UiRect> SelectionRects { get; private set; } = [];
    public IReadOnlyList<string> DisplayLines { get; private set; } = [""];
    private float Measure(string text, ITextMetrics metrics) => metrics.MeasureWidth(input.Mask(text), owner.Style.FontSize, owner.Style.Bold, owner.Style.FontFamily);
    private int Row(int position)
    {
        var row = layout.Row(position);
        return input.CaretUpstream && position == input.Caret && row > 0 && layout.Lines[row - 1].End == position ? row - 1 : row;
    }
    public TextEditLine LineAt(int position) => layout.Lines[Row(position)];
    public float CaretX(int position, ITextMetrics metrics)
    {
        var line = LineAt(position);
        return Measure(input.Value[line.Start..Math.Min(position, line.End)], metrics);
    }
    private TextEditPosition Position(int row, float x, ITextMetrics metrics)
    {
        row = Math.Clamp(row, 0, layout.Lines.Count - 1);
        var index = layout.Nearest(row, x, text => Measure(text, metrics));
        return new(index, row + 1 < layout.Lines.Count && index == layout.Lines[row + 1].Start);
    }
    public TextEditPosition Vertical(int position, int rows, float x, ITextMetrics metrics) => Position(Row(position) + rows, x, metrics);
    public TextEditPosition Hit(float x, float y, ITextMetrics metrics)
    {
        var row = input.IsMultiline ? (int)MathF.Floor((y - owner.ContentBounds.Y + owner.Scroll.Offset) / owner.Style.LineHeight) : 0;
        return Position(row, x - owner.ContentBounds.X + horizontal, metrics);
    }
    public int HitWord(float x, float y, ITextMetrics metrics)
    {
        var row = input.IsMultiline ? (int)MathF.Floor((y - owner.ContentBounds.Y + owner.Scroll.Offset) / owner.Style.LineHeight) : 0;
        var line = layout.Lines[Math.Clamp(row, 0, layout.Lines.Count - 1)];
        var local = x - owner.ContentBounds.X + horizontal;
        var index = layout.Nearest(row, local, text => Measure(text, metrics));
        if (index > line.Start && (index == line.End || local < Measure(input.Value[line.Start..index], metrics)))
            index = line.Start + System.Globalization.StringInfo.ParseCombiningCharacters(line.Text).Last(i => line.Start + i < index);
        return index;
    }
    public void Arrange(ITextMetrics metrics, bool reveal)
    {
        var content = owner.ContentBounds;
        layout.Arrange(input.Value, Math.Max(1, content.Width - 2), input.IsMultiline, text => Measure(text, metrics));
        var display = layout;
        if (input.IsPlaceholder)
        {
            display = new();
            display.Arrange(owner.Text, Math.Max(1, content.Width - 2), input.IsMultiline, text => metrics.MeasureWidth(text, owner.Style.FontSize, owner.Style.Bold, owner.Style.FontFamily));
        }
        DisplayLines = input.IsMasked && !input.IsPlaceholder ? [owner.Text] : display.Lines.Select(l => l.Text).ToArray();
        var row = Row(input.Caret);
        var x = CaretX(input.Caret, metrics);
        var height = owner.Style.LineHeight;
        ArrangeViewport(content, row, x, height, reveal, metrics);
        CaretBounds = new(content.X + x - horizontal, TextBounds.Y + row * height, 1, Math.Min(height, content.Height));
        SelectionRects = Selection(metrics, height);
    }
    private void ArrangeViewport(UiRect content, int row, float x, float height, bool reveal, ITextMetrics metrics)
    {
        if (input.IsMultiline) ArrangeVerticalViewport(content, row, height, reveal);
        else ArrangeHorizontalViewport(content, x, reveal, metrics);
    }

    private void ArrangeVerticalViewport(UiRect content, int row, float height, bool reveal)
    {
        horizontal = 0;
        owner.Scroll.Arrange(DisplayLines.Count * height);
        if (reveal)
        {
            if (row * height < owner.Scroll.Offset) owner.Scroll.Offset = row * height;
            if ((row + 1) * height > owner.Scroll.Offset + content.Height) owner.Scroll.Offset = (row + 1) * height - content.Height;
        }
        owner.Scroll.Arrange(DisplayLines.Count * height);
        TextBounds = content with { Y = content.Y - owner.Scroll.Offset };
    }

    private void ArrangeHorizontalViewport(UiRect content, float x, bool reveal, ITextMetrics metrics)
    {
        var width = metrics.MeasureWidth(owner.Text, owner.Style.FontSize, owner.Style.Bold, owner.Style.FontFamily);
        if (reveal)
        {
            if (x < horizontal) horizontal = x;
            if (x > horizontal + Math.Max(0, content.Width - 2)) horizontal = x - Math.Max(0, content.Width - 2);
        }
        horizontal = Math.Clamp(horizontal, 0, Math.Max(0, width + 2 - content.Width));
        TextBounds = content with { X = content.X - horizontal, Width = Math.Max(content.Width, width + 2) };
    }

    private IReadOnlyList<UiRect> Selection(ITextMetrics metrics, float height)
    {
        if (input.SelectionLength == 0) return [];
        var rects = new List<UiRect>();
        var end = input.SelectionStart + input.SelectionLength;
        for (var row = 0; row < layout.Lines.Count; row++)
        {
            var line = layout.Lines[row];
            var start = Math.Max(input.SelectionStart, line.Start);
            var stop = Math.Min(end, line.End);
            if (stop < start || start > end || line.Start >= end) continue;
            var left = Measure(input.Value[line.Start..start], metrics);
            var right = Measure(input.Value[line.Start..stop], metrics);
            if (end > line.End && line.End < input.Value.Length && input.Value[line.End] == '\n') right += 6;
            rects.Add(new(TextBounds.X + left, TextBounds.Y + row * height, right - left, height));
        }
        return rects;
    }
}
