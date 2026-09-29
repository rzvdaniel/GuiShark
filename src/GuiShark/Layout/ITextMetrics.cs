namespace GuiShark;

public interface ITextMetrics
{
    float MeasureWidth(string text, float fontSize, bool bold);
}
