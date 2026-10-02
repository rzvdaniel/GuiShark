namespace GuiShark;

internal readonly record struct TextEditState(string Value, int Anchor, int Caret, bool Upstream, bool Trailing);

/// <summary>Bounded edit snapshots; selection and scroll alone do not create history entries.</summary>
internal sealed class TextEditHistory
{
    private readonly List<TextEditState> undo = [];
    private readonly Stack<TextEditState> redo = new();
    public bool CanUndo => undo.Count > 0;
    public bool CanRedo => redo.Count > 0;
    public void Record(TextEditState state)
    {
        if (undo.Count == 100) undo.RemoveAt(0);
        undo.Add(state);
        redo.Clear();
    }
    public TextEditState Undo(TextEditState current)
    {
        redo.Push(current);
        var result = undo[^1]; undo.RemoveAt(undo.Count - 1); return result;
    }
    public TextEditState Redo(TextEditState current)
    {
        undo.Add(current); return redo.Pop();
    }
    public void Clear() { undo.Clear(); redo.Clear(); }
}
