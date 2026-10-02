namespace GuiShark;

/// <summary>Shared logical alignment for painting and editable text geometry.</summary>
public static class TextAlignmentLayout
{
    public static float Offset(float width, float advance, TextAlignment alignment, bool rtl) => alignment switch
    {
        TextAlignment.Center => (width - advance) / 2,
        TextAlignment.Right => width - advance,
        TextAlignment.Start when rtl => width - advance,
        TextAlignment.End when !rtl => width - advance,
        _ => 0
    };
}
