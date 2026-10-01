using System.Globalization;

namespace GuiShark;

internal readonly record struct TextEditLine(int Start, int End, string Text);

/// <summary>Whitespace-preserving visual lines shared by editing and all rendering backends.</summary>
internal sealed class TextEditLayout
{
    public IReadOnlyList<TextEditLine> Lines { get; private set; } = [new(0, 0, "")];
    public void Arrange(string value, float width, bool multiline, Func<string, float> measure)
    {
        if (!multiline) { Lines = [new(0, value.Length, value)]; return; }
        var lines = new List<TextEditLine>();
        var start = 0;
        var end = 0;
        var breakAt = -1;
        var positions = StringInfo.ParseCombiningCharacters(value).Append(value.Length).ToArray();
        for (var i = 0; i < positions.Length - 1; i++)
        {
            var at = positions[i];
            var next = positions[i + 1];
            if (value[at] == '\n')
            {
                lines.Add(new(start, at, value[start..at])); start = end = next; breakAt = -1; continue;
            }
            if (end > start && measure(value[start..next]) > Math.Max(1, width))
            {
                var split = breakAt > start ? breakAt : at;
                lines.Add(new(start, split, value[start..split]));
                start = split; breakAt = -1;
                // Re-evaluate the remainder so long words after a space also wrap.
                i = Array.BinarySearch(positions, start) - 1; end = start; continue;
            }
            end = next;
            if (value[at] == ' ') breakAt = next;
        }
        lines.Add(new(start, value.Length, value[start..]));
        Lines = lines;
    }
    public int Row(int position)
    {
        for (var i = Lines.Count - 1; i >= 0; i--) if (position >= Lines[i].Start) return i;
        return 0;
    }
    public int Nearest(int row, float x, Func<string, float> measure)
    {
        var line = Lines[Math.Clamp(row, 0, Lines.Count - 1)];
        return line.Start + StringInfo.ParseCombiningCharacters(line.Text).Append(line.Text.Length)
            .MinBy(i => Math.Abs(measure(line.Text[..i]) - x));
    }
}
