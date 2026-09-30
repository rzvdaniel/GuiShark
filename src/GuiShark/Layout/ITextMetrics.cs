namespace GuiShark;

public interface ITextMetrics
{
    float MeasureWidth(string text, float fontSize, bool bold);
    float MeasureWidth(string text, float fontSize, bool bold, string family) => MeasureWidth(text, fontSize, bold);
}
