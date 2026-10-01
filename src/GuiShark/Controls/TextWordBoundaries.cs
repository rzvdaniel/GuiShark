using System.Globalization;
using System.Text;

namespace GuiShark;

/// <summary>Word runs in UTF-16 coordinates, with every boundary aligned to a complete grapheme.</summary>
internal sealed class TextWordBoundaries
{
    private enum Kind { Word, Space, Punctuation, Symbol }
    private readonly string text;
    private readonly int[] starts;
    private readonly Kind[] kinds;
    public TextWordBoundaries(string text)
    {
        this.text = text;
        starts = StringInfo.ParseCombiningCharacters(text).Append(text.Length).ToArray();
        kinds = starts.Take(starts.Length - 1).Select(Classify).ToArray();
        for (var i = 1; i + 1 < kinds.Length; i++)
            if ((text[starts[i]] is '\'' or '’') && kinds[i - 1] == Kind.Word && kinds[i + 1] == Kind.Word) kinds[i] = Kind.Word;
    }
    private Kind Classify(int index)
    {
        var rune = Rune.GetRuneAt(text, index);
        if (Rune.IsWhiteSpace(rune)) return Kind.Space;
        if (Rune.IsLetterOrDigit(rune) || rune.Value == '_' || Rune.GetUnicodeCategory(rune) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark) return Kind.Word;
        return Rune.IsPunctuation(rune) ? Kind.Punctuation : Kind.Symbol;
    }
    private int Cell(int position)
    {
        var found = Array.BinarySearch(starts, position);
        return Math.Max(0, found >= 0 ? found : ~found - 1);
    }
    private bool Joins(int a, int b) => kinds[a] == kinds[b] && kinds[a] != Kind.Symbol;
    public (int Start, int End) At(int position)
    {
        if (kinds.Length == 0) return (0, 0);
        var first = Math.Min(Cell(position), kinds.Length - 1);
        var last = first;
        while (first > 0 && Joins(first - 1, first)) first--;
        while (last + 1 < kinds.Length && Joins(last, last + 1)) last++;
        return (starts[first], starts[last + 1]);
    }
    public int Previous(int position)
    {
        var cell = Math.Min(Cell(position) - 1, kinds.Length - 1);
        while (cell >= 0 && kinds[cell] == Kind.Space) cell--;
        if (cell < 0) return 0;
        while (cell > 0 && Joins(cell - 1, cell)) cell--;
        return starts[cell];
    }
    public int Next(int position)
    {
        var cell = Cell(position);
        if (cell >= kinds.Length) return text.Length;
        var first = cell++;
        while (cell < kinds.Length && Joins(first, cell)) cell++;
        while (cell < kinds.Length && kinds[cell] == Kind.Space) cell++;
        return starts[cell];
    }
}
