using System.Globalization;

namespace GuiShark;

/// <summary>Horizontal caret coordinates for every Unicode grapheme boundary in one complete line.</summary>
public sealed class TextCaretMap
{
    private readonly int[] boundaries;
    private readonly float[] positions;

    public TextCaretMap(string text, IReadOnlyList<float> coordinates)
    {
        boundaries = StringInfo.ParseCombiningCharacters(text).Append(text.Length).ToArray();
        if (coordinates.Count != boundaries.Length || coordinates.Any(x => !float.IsFinite(x)))
            throw new ArgumentException("Supply one finite coordinate per grapheme boundary, including the end.", nameof(coordinates));
        positions = coordinates.ToArray();
    }

    /// <summary>Compatibility path for metrics implementations without full-line shaping.</summary>
    public static TextCaretMap Measure(string text, Func<string, float> measure) =>
        new(text, StringInfo.ParseCombiningCharacters(text).Append(text.Length).Select(i => measure(text[..i])).ToArray());

    /// <summary>Returns the coordinate at or before a UTF-16 index, never inside a grapheme.</summary>
    public float X(int index)
    {
        var at = Array.BinarySearch(boundaries, index);
        return positions[at >= 0 ? at : Math.Clamp(~at - 1, 0, positions.Length - 1)];
    }

    public int Nearest(float x) => boundaries[Enumerable.Range(0, positions.Length).MinBy(i => Math.Abs(positions[i] - x))];

    /// <summary>Retains coordinates while mapping a masked line's bullets back to the source graphemes.</summary>
    public TextCaretMap Remap(string text) => new(text, positions);
}
