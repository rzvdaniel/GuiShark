namespace GuiShark;

internal readonly record struct TextEditPosition(int Index, bool Upstream, bool Trailing = false);

/// <summary>Editor coordinates, wrapping and caret visibility, without GPU dependencies.</summary>
internal sealed class TextInputGeometry(UiElement owner, UiTextInput input)
{
    private readonly TextEditLayout layout = new();
    private float horizontal;
    private TextCaretMap[] maps = [new("", [0])];
    private float[] offsets = [0];
    public UiRect TextBounds { get; private set; }
    public UiRect CaretBounds { get; private set; }
    public IReadOnlyList<UiRect> SelectionRects { get; private set; } = [];
    public IReadOnlyList<UiRect> CompositionRects { get; private set; } = [];
    public IReadOnlyList<string> DisplayLines { get; private set; } = [""];
    public IReadOnlyList<TextLineContext> DisplayContexts { get; private set; } = [TextLineContext.Whole("")];
    private int Row(int position)
    {
        var row = layout.Row(position);
        return input.CaretUpstream && position == input.Caret && row > 0 && layout.Lines[row - 1].End == position ? row - 1 : row;
    }
    public TextEditLine LineAt(int position) => layout.Lines[Row(position)];
    public float CaretX(int position)
    {
        var line = LineAt(position);
        var row = Row(position);
        return offsets[row] + maps[row].X(new TextCaretPosition(Math.Min(position, line.End) - line.Start,
            position == input.DisplayCaret && input.DisplayCaretTrailing));
    }
    private TextEditPosition Position(int row, float x)
    {
        row = Math.Clamp(row, 0, layout.Lines.Count - 1);
        var at = maps[row].NearestPosition(x - offsets[row]);
        return InLine(row, at);
    }
    private TextEditPosition InLine(int row, TextCaretPosition position)
    {
        var index = layout.Lines[row].Start + position.Index;
        return new(index, row + 1 < layout.Lines.Count && index == layout.Lines[row + 1].Start, position.Trailing);
    }
    public TextEditPosition Edge(int position, bool right) => InLine(Row(position), maps[Row(position)].Edge(right));
    public TextEditPosition Horizontal(TextEditPosition position, bool right)
    {
        var row = layout.Row(position.Index);
        if (position.Upstream && row > 0 && layout.Lines[row - 1].End == position.Index) row--;
        var local = new TextCaretPosition(position.Index - layout.Lines[row].Start, position.Trailing);
        var next = maps[row].MoveVisual(local, right);
        if (next.Index != local.Index || Math.Abs(maps[row].X(next) - maps[row].X(local)) > .001f) return InLine(row, next);
        var nextRow = row + (right == maps[row].RightToLeft ? -1 : 1);
        if (nextRow < 0 || nextRow >= maps.Length) return InLine(row, next);
        var enterRight = nextRow > row ? maps[nextRow].RightToLeft : !maps[nextRow].RightToLeft;
        return InLine(nextRow, maps[nextRow].Edge(enterRight));
    }
    public TextEditPosition Word(TextEditPosition position, bool right)
    {
        var row = layout.Row(position.Index);
        if (position.Upstream && row > 0 && layout.Lines[row - 1].End == position.Index) row--;
        var line = layout.Lines[row];
        var local = new TextCaretPosition(position.Index - line.Start, position.Trailing);
        var next = TextVisualWordNavigation.Move(maps[row], line.Text, local, right);
        return next == local ? Horizontal(position, right) : InLine(row, next);
    }
    public TextEditPosition SelectionEdge(bool right)
    {
        var firstRow = layout.Row(input.SelectionStart);
        var end = input.SelectionStart + input.SelectionLength;
        var lastRow = layout.Row(end);
        if (lastRow > firstRow && layout.Lines[lastRow].Start == end) lastRow--;
        var row = right == maps[firstRow].RightToLeft ? firstRow : lastRow;
        var line = layout.Lines[row];
        var start = Math.Max(input.SelectionStart, line.Start) - line.Start;
        var stop = Math.Min(end, line.End) - line.Start;
        return InLine(row, maps[row].SelectionEdge(start, Math.Max(0, stop - start), right));
    }
    public TextEditPosition Vertical(int position, int rows, float x) => Position(Row(position) + rows, x);
    public TextEditPosition Hit(float x, float y)
    {
        var row = input.IsMultiline ? (int)MathF.Floor((y - owner.ContentBounds.Y + owner.Scroll.Offset) / owner.Style.LineHeight) : 0;
        return Position(row, x - owner.ContentBounds.X + horizontal);
    }
    public int HitWord(float x, float y)
    {
        var row = input.IsMultiline ? (int)MathF.Floor((y - owner.ContentBounds.Y + owner.Scroll.Offset) / owner.Style.LineHeight) : 0;
        row = Math.Clamp(row, 0, layout.Lines.Count - 1);
        var line = layout.Lines[row];
        var local = x - owner.ContentBounds.X + horizontal;
        return line.Start + maps[row].CharacterAt(local - offsets[row]);
    }
    public void Arrange(ITextMetrics metrics, bool reveal)
    {
        var content = owner.ContentBounds;
        var value = input.DisplayValue;
        layout.Arrange(value, Math.Max(1, content.Width - 2), input.IsMultiline,
            (start, length) => Measure(new(value, start, length, owner.TextDirection), metrics));
        maps = layout.Lines.Select(line => CreateMap(line, metrics)).ToArray();
        offsets = maps.Select(map => TextAlignmentLayout.Offset(Math.Max(content.Width, map.Width), map.Width, owner.Style.TextAlign, map.RightToLeft)).ToArray();
        var display = layout;
        var displayedValue = value;
        if (input.IsPlaceholder)
        {
            display = new();
            displayedValue = owner.Text;
            display.Arrange(owner.Text, Math.Max(1, content.Width - 2), input.IsMultiline,
                (start, length) => Measure(new(owner.Text, start, length, owner.TextDirection), metrics));
        }
        DisplayContexts = input.IsMasked && !input.IsPlaceholder ? [TextLineContext.Whole(owner.Text)]
            : display.Lines.Select(line => new TextLineContext(displayedValue, line.Start, line.End - line.Start, owner.TextDirection)).ToArray();
        DisplayLines = DisplayContexts.Select(context => context.Line).ToArray();
        var row = Row(input.DisplayCaret);
        var x = CaretX(input.DisplayCaret);
        var height = owner.Style.LineHeight;
        ArrangeViewport(content, row, x, height, reveal, metrics);
        CaretBounds = new(content.X + x - horizontal, TextBounds.Y + row * height, 1, Math.Min(height, content.Height));
        SelectionRects = input.Composition == null ? Range(input.SelectionStart, input.SelectionLength, height) : [];
        CompositionRects = input.Composition is { } composition ? Range(input.CompositionStart, composition.Text.Length, height) : [];
    }
    private float Measure(TextLineContext context, ITextMetrics metrics) => metrics.MeasureWidth(context, owner.Style.FontSize, owner.Style.Bold, owner.Style.FontFamily);
    private TextCaretMap CreateMap(TextEditLine line, ITextMetrics metrics)
    {
        var context = input.IsMasked ? TextLineContext.Whole(input.Mask(line.Text))
            : new TextLineContext(input.DisplayValue, line.Start, line.End - line.Start, owner.TextDirection);
        var map = metrics.CreateCaretMap(context, owner.Style.FontSize, owner.Style.Bold, owner.Style.FontFamily);
        return input.IsMasked ? map.Remap(line.Text) : map;
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
        var width = input.IsPlaceholder ? Measure(TextLineContext.Whole(owner.Text, owner.TextDirection), metrics) : maps[0].Width;
        if (reveal)
        {
            if (x < horizontal) horizontal = x;
            if (x > horizontal + Math.Max(0, content.Width - 2)) horizontal = x - Math.Max(0, content.Width - 2);
        }
        horizontal = Math.Clamp(horizontal, 0, Math.Max(0, width + 2 - content.Width));
        TextBounds = content with { X = content.X - horizontal, Width = Math.Max(content.Width, width + 2) };
    }

    private IReadOnlyList<UiRect> Range(int selectionStart, int selectionLength, float height)
    {
        if (selectionLength == 0) return [];
        var rects = new List<UiRect>();
        var end = selectionStart + selectionLength;
        for (var row = 0; row < layout.Lines.Count; row++)
        {
            var line = layout.Lines[row];
            var start = Math.Max(selectionStart, line.Start);
            var stop = Math.Min(end, line.End);
            if (stop < start || start > end || line.Start >= end) continue;
            foreach (var span in maps[row].Selection(start - line.Start, stop - start))
                rects.Add(new(TextBounds.X + offsets[row] + span.X, TextBounds.Y + row * height, span.Width, height));
            AddNewline(rects, row, end, height);
        }
        return rects;
    }
    private void AddNewline(List<UiRect> rects, int row, int selectionEnd, float height)
    {
        var line = layout.Lines[row];
        if (selectionEnd <= line.End || line.End >= input.DisplayValue.Length || input.DisplayValue[line.End] != '\n') return;
        var x = offsets[row] + maps[row].X(new TextCaretPosition(line.End - line.Start, true));
        rects.Add(new(TextBounds.X + x - (maps[row].RightToLeft ? 6 : 0), TextBounds.Y + row * height, 6, height));
    }
}
