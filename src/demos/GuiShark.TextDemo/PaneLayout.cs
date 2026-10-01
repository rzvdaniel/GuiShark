namespace GuiShark.TextDemo;

internal sealed record PaneLayout(int Index, UiRect Card, UiRect Sample, UiRect Zoom)
{
    public static IReadOnlyList<PaneLayout> Arrange(int width, int height, int mode)
    {
        const int padding = 22, gap = 16, top = 244;
        var columns = mode == 0 ? 2 : 1;
        const int rows = 1;
        var cardWidth = (width - padding * 2 - gap * (columns - 1)) / columns;
        var cardHeight = (height - top - 38 - gap * (rows - 1)) / rows;
        var result = new List<PaneLayout>();
        for (var slot = 0; slot < rows * columns; slot++)
        {
            var x = padding + slot % columns * (cardWidth + gap);
            var y = top + slot / columns * (cardHeight + gap);
            result.Add(new(mode == 0 ? slot : mode - 1, new(x, y, cardWidth, cardHeight),
                new(x + 12, y + 55, cardWidth - 24, cardHeight - 148),
                new(x + 12, y + cardHeight - 69, cardWidth - 24, 42)));
        }
        return result;
    }
}
