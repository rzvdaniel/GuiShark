using Unicode.Bidi;

namespace GuiShark.OpenGL;

internal readonly record struct VisualFontRun(int Start, string Text, FontAsset Font, bool RightToLeft);

/// <summary>Resolves paragraph levels before wrapping, then intersects visual runs with local fallback fonts.</summary>
internal sealed class BidiFontRuns(FontBook fonts)
{
    private readonly Dictionary<(string Text, UiTextDirection Direction), BidiInfo> paragraphs = new();
    private BidiInfo Get(TextLineContext context)
    {
        var key = (context.Text, context.Direction);
        if (paragraphs.TryGetValue(key, out var info)) return info;
        Level? level = context.Direction switch
        {
            UiTextDirection.LeftToRight => Level.Ltr(), UiTextDirection.RightToLeft => Level.Rtl(), _ => null
        };
        info = BidiInfo.Create(context.Text, level);
        if (paragraphs.Count >= 128) paragraphs.Remove(paragraphs.First().Key);
        paragraphs[key] = info;
        return info;
    }
    public bool IsRightToLeft(TextLineContext context)
    {
        var info = Get(context);
        return info.Paragraphs.FirstOrDefault(p => context.Start >= p.Range.Start && context.Start < p.Range.End)?.Level.IsRtl()
            ?? context.Direction == UiTextDirection.RightToLeft;
    }
    public IReadOnlyList<VisualFontRun> Resolve(TextLineContext context, FontRunStyle style)
    {
        // Validate the family and borrowed book even when the paragraph has no glyphs.
        fonts.Resolve(style.Family, style.Bold);
        var result = new List<VisualFontRun>();
        var info = Get(context);
        foreach (var paragraph in info.Paragraphs)
        {
            var start = Math.Max(context.Start, paragraph.Range.Start);
            var end = Math.Min(context.Start + context.Length, paragraph.Range.End);
            if (end <= start) continue;
            var (levels, ranges) = info.VisualRuns(paragraph, new(start, end));
            foreach (var range in ranges) AddRuns(result, context, style, range, levels[range.Start].IsRtl());
        }
        return result;
    }
    private void AddRuns(List<VisualFontRun> result, TextLineContext context, FontRunStyle style, TextRange range, bool rtl)
    {
        var text = context.Text[range.Start..range.End];
        IEnumerable<FontRun> runs = fonts.Runs(text, style.Family, style.Bold);
        if (rtl) runs = runs.Reverse();
        foreach (var run in runs) result.Add(new(range.Start - context.Start + run.Start, run.Text, run.Font, rtl));
    }
    public void Clear() => paragraphs.Clear();
}
