namespace GuiShark;

public enum UiTextDirection { LeftToRight, RightToLeft, Auto }

/// <summary>A visual line's logical range within the original text, preserving paragraph bidi context.</summary>
public readonly record struct TextLineContext
{
    public TextLineContext(string text, int start, int length, UiTextDirection direction = UiTextDirection.LeftToRight)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (start < 0 || length < 0 || start > text.Length - length) throw new ArgumentOutOfRangeException(nameof(start));
        Text = text; Start = start; Length = length; Direction = direction;
    }
    public string Text { get; }
    public int Start { get; }
    public int Length { get; }
    public UiTextDirection Direction { get; }
    public string Line => Text.Substring(Start, Length);
    public static TextLineContext Whole(string text, UiTextDirection direction = UiTextDirection.LeftToRight) => new(text, 0, text.Length, direction);
}
