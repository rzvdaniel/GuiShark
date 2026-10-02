namespace GuiShark;

/// <summary>Shared wrapping for measurement and painting. Whitespace is collapsed by the HTML loader.</summary>
public static class TextLayout
{
    public static IReadOnlyList<string> Lines(UiElement element, float width, ITextMetrics metrics) =>
        Contexts(element, width, metrics).Select(c => c.Line).ToArray();
    public static IReadOnlyList<TextLineContext> Contexts(UiElement element, float width, ITextMetrics metrics) =>
        element.TextInput != null ? element.TextInput.DisplayContexts : WrapContexts(element.Text, width, element.Style, metrics, element.TextDirection);

    public static IReadOnlyList<string> Wrap(string text, float width, UiStyle style, ITextMetrics metrics)
        => WrapContexts(text, width, style, metrics, UiTextDirection.LeftToRight).Select(c => c.Line).ToArray();

    private static IReadOnlyList<TextLineContext> WrapContexts(string text, float width, UiStyle style, ITextMetrics metrics, UiTextDirection direction)
    {
        if (string.IsNullOrEmpty(text) || width <= 0) return [];
        var lines = new List<TextLineContext>();
        var line = "";
        var start = 0;
        var at = 0;
        foreach (var word in text.Split(' '))
        {
            if (line.Length == 0) start = at;
            var candidate = line.Length == 0 ? word : line + " " + word;
            if (line.Length > 0 && metrics.MeasureWidth(new TextLineContext(text, start, candidate.Length, direction), style.FontSize, style.Bold, style.FontFamily) > width)
            {
                lines.Add(new(text, start, line.Length, direction));
                line = word;
                start = at;
            }
            else line = candidate;
            at += word.Length + 1;
        }
        if (line.Length > 0) lines.Add(new(text, start, line.Length, direction));
        return lines;
    }
}
