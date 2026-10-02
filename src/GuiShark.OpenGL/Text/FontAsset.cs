using System.Globalization;
using System.Text;
using SkiaSharp;

namespace GuiShark.OpenGL;

/// <summary>Owns a loaded typeface and its bounded coverage cache; never consults OS fonts.</summary>
internal sealed class FontAsset(byte[] bytes, SKTypeface typeface) : IDisposable
{
    public byte[] Bytes { get; } = bytes;
    public SKTypeface Typeface { get; } = typeface;
    private readonly SKFont coverageFont = new(typeface);
    private readonly Dictionary<string, bool> coverage = new();

    public bool Covers(string grapheme)
    {
        if (coverage.TryGetValue(grapheme, out var result)) return result;
        var visible = string.Concat(grapheme.EnumerateRunes().Where(r => !IsIgnorable(r)).Select(r => r.ToString()));
        result = coverageFont.ContainsGlyphs(visible);
        if (coverage.Count >= 1024) coverage.Remove(coverage.First().Key);
        coverage[grapheme] = result;
        return result;
    }

    private static bool IsIgnorable(Rune rune) => Rune.GetUnicodeCategory(rune) == UnicodeCategory.Format
        || rune.Value is >= 0xfe00 and <= 0xfe0f or >= 0xe0100 and <= 0xe01ef;

    public void Dispose() { coverageFont.Dispose(); Typeface.Dispose(); }
}
