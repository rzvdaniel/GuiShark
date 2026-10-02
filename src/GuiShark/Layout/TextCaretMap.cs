using System.Globalization;

namespace GuiShark;

/// <summary>A logical boundary's visual affinity: following leading edge or preceding trailing edge.</summary>
public readonly record struct TextCaretPosition(int Index, bool Trailing = false);
public readonly record struct TextVisualSpan(float X, float Width) { public float Right => X + Width; }

/// <summary>Visual grapheme edges, caret stops and selection spans for one complete line.</summary>
public sealed class TextCaretMap
{
    private readonly int[] boundaries;
    private readonly float[] leading;
    private readonly float[] trailing;
    private readonly Stop[] stops;
    private readonly record struct Stop(TextCaretPosition Position, float X);
    public bool RightToLeft { get; }
    public float Width => stops.Max(s => s.X);

    public TextCaretMap(string text, IReadOnlyList<float> coordinates)
        : this(text, coordinates.Take(coordinates.Count - 1).ToArray(), coordinates.Skip(1).ToArray())
    {
        if (coordinates.Any(x => !float.IsFinite(x)) || coordinates.Count != boundaries.Length) throw new ArgumentException("Supply one coordinate per boundary, including the end.", nameof(coordinates));
        if (leading.Length == 0) stops = [new(new(0), coordinates[0])];
    }

    public TextCaretMap(string text, IReadOnlyList<float> leadingEdges, IReadOnlyList<float> trailingEdges, bool rightToLeft = false)
    {
        boundaries = StringInfo.ParseCombiningCharacters(text).Append(text.Length).ToArray();
        Validate(leadingEdges, nameof(leadingEdges)); Validate(trailingEdges, nameof(trailingEdges));
        leading = leadingEdges.ToArray(); trailing = trailingEdges.ToArray(); RightToLeft = rightToLeft;
        stops = BuildStops();
    }
    private void Validate(IReadOnlyList<float> coordinates, string name)
    {
        if (coordinates.Count != boundaries.Length - 1 || coordinates.Any(x => !float.IsFinite(x)))
            throw new ArgumentException("Supply one finite edge coordinate per grapheme.", name);
    }
    private Stop[] BuildStops()
    {
        if (leading.Length == 0) return [new(new(0), 0)];
        return Enumerable.Range(0, leading.Length).SelectMany(i => new[]
        {
            new Stop(new(boundaries[i]), leading[i]), new Stop(new(boundaries[i + 1], true), trailing[i])
        }).GroupBy(s => (s.Position.Index, s.X)).Select(g => g.OrderBy(s => s.Position.Trailing).First())
            .OrderBy(s => s.X).ToArray();
    }

    /// <summary>Compatibility path for metrics implementations without full-line shaping.</summary>
    public static TextCaretMap Measure(string text, Func<string, float> measure) =>
        new(text, StringInfo.ParseCombiningCharacters(text).Append(text.Length).Select(i => measure(text[..i])).ToArray());

    /// <summary>Returns the coordinate at or before a UTF-16 index, never inside a grapheme.</summary>
    public float X(int index) => X(new TextCaretPosition(index));
    public float X(TextCaretPosition position)
    {
        if (leading.Length == 0) return stops[0].X;
        var found = Array.BinarySearch(boundaries, position.Index);
        var at = found >= 0 ? found : Math.Clamp(~found - 1, 0, boundaries.Length - 1);
        if (at == leading.Length || position.Trailing && at > 0) return trailing[at - 1];
        return leading[at];
    }

    public TextCaretPosition NearestPosition(float x) => stops.MinBy(s => Math.Abs(s.X - x)).Position;
    public int Nearest(float x) => NearestPosition(x).Index;
    public TextCaretPosition Edge(bool right) => (right ? stops[^1] : stops[0]).Position;
    public TextCaretPosition SelectionEdge(int start, int length, bool right)
    {
        if (length == 0 || leading.Length == 0) return Edge(right);
        var edges = Enumerable.Range(0, leading.Length).Where(i => boundaries[i] < start + length && boundaries[i + 1] > start)
            .SelectMany(i => new[] { new Stop(new(boundaries[i]), leading[i]), new Stop(new(boundaries[i + 1], true), trailing[i]) });
        var edge = right ? edges.MaxBy(s => s.X) : edges.MinBy(s => s.X);
        return edge.Position;
    }
    public TextCaretPosition MoveVisual(TextCaretPosition position, bool right)
    {
        var at = Array.FindIndex(stops, s => s.Position == position);
        if (at < 0) at = Array.FindIndex(stops, s => s.Position.Index == position.Index && Math.Abs(s.X - X(position)) < .001f);
        if (at < 0) at = 0;
        return stops[Math.Clamp(at + (right ? 1 : -1), 0, stops.Length - 1)].Position;
    }
    public int CharacterAt(float x)
    {
        if (leading.Length == 0) return 0;
        var cell = Enumerable.Range(0, leading.Length)
            .OrderBy(i => Math.Max(0, Math.Max(Math.Min(leading[i], trailing[i]) - x, x - Math.Max(leading[i], trailing[i]))))
            .ThenBy(i => Math.Abs((leading[i] + trailing[i]) / 2 - x)).First();
        return boundaries[cell];
    }
    public IReadOnlyList<TextVisualSpan> Selection(int start, int length)
    {
        if (length == 0) return [];
        var spans = Enumerable.Range(0, leading.Length).Where(i => boundaries[i] < start + length && boundaries[i + 1] > start)
            .Select(i => new TextVisualSpan(Math.Min(leading[i], trailing[i]), Math.Abs(trailing[i] - leading[i])))
            .OrderBy(s => s.X);
        var result = new List<TextVisualSpan>();
        foreach (var span in spans) AddSpan(result, span);
        return result;
    }
    private static void AddSpan(List<TextVisualSpan> result, TextVisualSpan span)
    {
        if (result.Count > 0 && span.X <= result[^1].Right + .001f)
        {
            var last = result[^1];
            result[^1] = last with { Width = Math.Max(last.Right, span.Right) - last.X };
        }
        else result.Add(span);
    }

    /// <summary>Retains coordinates while mapping a masked line's bullets back to the source graphemes.</summary>
    public TextCaretMap Remap(string text) => leading.Length == 0 ? new(text, [stops[0].X]) : new(text, leading, trailing, RightToLeft);
}
