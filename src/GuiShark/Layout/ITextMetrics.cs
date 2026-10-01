namespace GuiShark;

public interface ITextMetrics
{
    float MeasureWidth(string text, float fontSize, bool bold);
    float MeasureWidth(string text, float fontSize, bool bold, string family) => MeasureWidth(text, fontSize, bold);
    TextCaretMap CreateCaretMap(string text, float fontSize, bool bold, string family) =>
        TextCaretMap.Measure(text, prefix => MeasureWidth(prefix, fontSize, bold, family));
}
