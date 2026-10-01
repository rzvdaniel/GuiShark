namespace GuiShark;

/// <summary>Uncommitted IME text. Selection offsets are UTF-16 positions within Text.</summary>
public sealed record UiComposition(string Text, int SelectionStart, int SelectionLength);
