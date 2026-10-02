namespace GuiShark;

public interface ITextMetrics
{
    float MeasureWidth(string text, float fontSize, bool bold);
    float MeasureWidth(string text, float fontSize, bool bold, string family) => MeasureWidth(text, fontSize, bold);
    float MeasureWidth(TextLineContext context, float fontSize, bool bold, string family) =>
        MeasureWidth(context.Line, fontSize, bold, family);
    TextCaretMap CreateCaretMap(string text, float fontSize, bool bold, string family) =>
        TextCaretMap.Measure(text, prefix => MeasureWidth(prefix, fontSize, bold, family));
    TextCaretMap CreateCaretMap(TextLineContext context, float fontSize, bool bold, string family) =>
        CreateCaretMap(context.Line, fontSize, bold, family);
}
