namespace GuiShark;

/// <summary>Shared wrapping for measurement and painting. Whitespace is collapsed by the HTML loader.</summary>
public static class TextLayout
{
    public static IReadOnlyList<string> Wrap(string text, float width, UiStyle style, ITextMetrics metrics)
    {
        if (string.IsNullOrEmpty(text) || width <= 0) return [];
        var lines = new List<string>();
        var line = "";
        foreach (var word in text.Split(' '))
        {
            var candidate = line.Length == 0 ? word : line + " " + word;
            if (line.Length > 0 && metrics.MeasureWidth(candidate, style.FontSize, style.Bold) > width)
            {
                lines.Add(line);
                line = word;
            }
            else line = candidate;
        }
        if (line.Length > 0) lines.Add(line);
        return lines;
    }
}
