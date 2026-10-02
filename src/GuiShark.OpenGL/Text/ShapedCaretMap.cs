using System.Globalization;
using HarfBuzzSharp;

namespace GuiShark.OpenGL;

/// <summary>Maps UTF-16 HarfBuzz clusters to grapheme caret stops, using pen advances rather than glyph offsets.</summary>
internal static class ShapedCaretMap
{
    private readonly record struct Cluster(int Start, float Left, float Right);

    public static TextCaretMap Create(string text, HarfBuzzSharp.Buffer buffer, float width)
    {
        var boundaries = StringInfo.ParseCombiningCharacters(text).Append(text.Length).ToArray();
        var clusters = Collect(buffer, width, boundaries);
        var coordinates = new float[boundaries.Length];
        var rtl = buffer.Direction == Direction.RightToLeft;
        for (var i = 0; i < clusters.Length; i++)
        {
            var cluster = clusters[i];
            var end = i + 1 < clusters.Length ? clusters[i + 1].Start : text.Length;
            var stops = Enumerable.Range(0, boundaries.Length).Where(j => boundaries[j] >= cluster.Start && boundaries[j] <= end).ToArray();
            Fill(coordinates, stops, cluster, rtl);
        }
        return new(text, coordinates);
    }

    private static void Fill(float[] coordinates, int[] stops, Cluster cluster, bool rtl)
    {
        var start = rtl ? cluster.Right : cluster.Left;
        var end = rtl ? cluster.Left : cluster.Right;
        for (var i = 0; i < stops.Length; i++)
            coordinates[stops[i]] = start + (end - start) * i / Math.Max(1, stops.Length - 1);
    }

    private static Cluster[] Collect(HarfBuzzSharp.Buffer buffer, float width, int[] boundaries)
    {
        var info = buffer.GetGlyphInfoSpan();
        var advances = buffer.GetGlyphPositionSpan();
        var total = 0f;
        foreach (var advance in advances) total += advance.XAdvance;
        var scale = total == 0 ? 0 : width / total;
        var pen = 0f;
        var clusters = new Dictionary<int, Cluster>();
        for (var i = 0; i < info.Length; i++)
        {
            var index = checked((int)info[i].Cluster);
            var start = boundaries.Last(at => at <= index);
            var next = pen + advances[i].XAdvance * scale;
            var prior = clusters.GetValueOrDefault(start, new(start, pen, pen));
            clusters[start] = new(start, Math.Min(prior.Left, next), Math.Max(prior.Right, next));
            pen = next;
        }
        return clusters.Values.OrderBy(c => c.Start).ToArray();
    }
}
