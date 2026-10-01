namespace GuiShark;

internal sealed class TextComposition(UiTextInput input)
{
    public UiComposition? State { get; private set; }
    public int Start { get; private set; }
    public int ReplacedLength { get; private set; }
    public bool Upstream { get; private set; }
    public string DisplayValue => State is { } state
        ? input.Value.Remove(Start, ReplacedLength).Insert(Start, state.Text) : input.Value;
    public int DisplayCaret => State is { } state ? Start + state.SelectionStart + state.SelectionLength : input.Caret;
    public void Update(string text, int selectionStart, int selectionLength)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (selectionStart < 0 || selectionLength < 0 || selectionStart > text.Length - selectionLength)
            throw new ArgumentOutOfRangeException(nameof(selectionStart));
        ValidateOffset(text, selectionStart);
        ValidateOffset(text, selectionStart + selectionLength);
        if (State == null)
        {
            Start = input.SelectionStart;
            ReplacedLength = input.SelectionLength;
            Upstream = input.CaretUpstream;
        }
        State = new(text, selectionStart, selectionLength);
    }
    private static void ValidateOffset(string text, int offset)
    {
        if (offset > 0 && offset < text.Length && char.IsHighSurrogate(text[offset - 1]) && char.IsLowSurrogate(text[offset]))
            throw new ArgumentOutOfRangeException(nameof(offset), "Composition offsets must not split a Unicode scalar.");
    }
    public bool Clear()
    {
        if (State == null) return false;
        State = null;
        return true;
    }
}
