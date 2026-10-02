using System.Globalization;
using System.Text;

namespace GuiShark.OpenGL;

internal readonly record struct FontRun(int Start, string Text, FontAsset Font);

/// <summary>Selects complete graphemes and coalesces adjacent text using the same local typeface.</summary>
internal static class FontFallback
{
    public static IReadOnlyList<FontRun> Select(string text, IReadOnlyList<FontAsset> candidates)
    {
        var boundaries = StringInfo.ParseCombiningCharacters(text).Append(text.Length).ToArray();
        var runs = new List<FontRun>();
        FontAsset? previous = null;
        var start = 0;
        for (var i = 0; i < boundaries.Length - 1; i++)
        {
            var at = boundaries[i];
            var grapheme = text[at..boundaries[i + 1]];
            var next = Choose(grapheme, candidates, previous);
            if (previous != null && previous != next)
            {
                runs.Add(new(start, text[start..at], previous));
                start = at;
            }
            previous = next;
        }
        if (previous != null) runs.Add(new(start, text[start..], previous));
        return runs;
    }

    private static FontAsset Choose(string grapheme, IReadOnlyList<FontAsset> candidates, FontAsset? previous)
    {
        if (previous != null && grapheme.EnumerateRunes().All(r => Rune.IsWhiteSpace(r) || Rune.IsPunctuation(r)) && previous.Covers(grapheme))
            return previous;
        return candidates.FirstOrDefault(face => face.Covers(grapheme)) ?? candidates[0];
    }
}
