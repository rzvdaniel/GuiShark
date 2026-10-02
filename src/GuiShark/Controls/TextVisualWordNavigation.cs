namespace GuiShark;

internal static class TextVisualWordNavigation
{
    public static TextCaretPosition Move(TextCaretMap map, string text, TextCaretPosition position, bool right)
    {
        var words = new TextWordBoundaries(text);
        (int Start, int End)? first = null;
        var spaces = false;
        bool? forward = null;
        for (var i = 0; i < text.Length * 2 + 2; i++)
        {
            var next = map.MoveVisual(position, right);
            if (next == position) break;
            var x = map.X(position);
            var nextX = map.X(next);
            if (Math.Abs(x - nextX) > .001f)
            {
                var cell = map.CharacterAt((x + nextX) / 2);
                forward ??= next.Index > position.Index;
                var word = words.At(cell);
                var space = char.IsWhiteSpace(text, cell);
                if (ShouldStop(space, spaces, first, word, forward.Value)) break;
                if (space) spaces = true;
                else first ??= word;
            }
            position = next;
        }
        return position;
    }
    private static bool ShouldStop(bool space, bool spaces, (int Start, int End)? first, (int Start, int End) word, bool forward)
    {
        if (!forward) return first.HasValue && (space || word != first.Value);
        return !space && (spaces || first.HasValue && word != first.Value);
    }
}
