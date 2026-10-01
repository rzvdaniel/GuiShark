using System.Globalization;

namespace GuiShark;

/// <summary>Text editing state for input and textarea controls. Selection offsets are UTF-16 indices at grapheme boundaries.</summary>
public sealed class UiTextInput
{
    private readonly UiElement owner;
    private string value = "";
    private string placeholder = "";
    private int anchor;
    private int caret;
    private readonly TextEditHistory history = new();
    private readonly TextInputGeometry geometry;
    private bool revealCaret = true;
    private bool showPassword;
    private float? preferredX;
    private (int Start, int End) pointerWord;
    internal bool CaretUpstream { get; private set; }
    private int maximumLength = int.MaxValue;
    public event Action<UiTextInput>? Changed;
    public event Action<UiTextInput>? Submitted;
    internal UiTextInput(UiElement owner, bool multiline = false, bool password = false)
    {
        this.owner = owner;
        IsMultiline = multiline;
        IsPassword = password;
        geometry = new(owner, this);
    }
    public bool IsMultiline { get; }
    public bool IsPassword { get; }
    public bool ShowPassword
    {
        get => showPassword;
        set { if (showPassword != value) { showPassword = value; Refresh(); } }
    }
    internal bool IsMasked => IsPassword && !ShowPassword;
    internal string Mask(string text) => IsMasked ? new string('•', StringInfo.ParseCombiningCharacters(text).Length) : text;
    public int Rows { get; internal set; } = 4;
    public bool CanUndo => !ReadOnly && history.CanUndo;
    public bool CanRedo => !ReadOnly && history.CanRedo;
    private TextEditState State => new(value, anchor, caret, CaretUpstream);
    public void Undo() { if (CanUndo) Restore(history.Undo(State)); }
    public void Redo() { if (CanRedo) Restore(history.Redo(State)); }
    private void Restore(TextEditState state)
    {
        value = state.Value; anchor = state.Anchor; caret = state.Caret;
        Refresh(); CaretUpstream = state.Upstream; Changed?.Invoke(this);
    }
    public string Value
    {
        get => value;
        set
        {
            var next = Clean(value ?? "");
            if (this.value == next) return;
            history.Clear();
            this.value = next;
            caret = Boundary(Math.Min(caret, next.Length));
            anchor = Boundary(Math.Min(anchor, next.Length));
            Refresh();
            Changed?.Invoke(this);
        }
    }
    public string Placeholder { get => placeholder; set { placeholder = Clean(value ?? "", false); Refresh(); } }
    public bool ReadOnly { get; set; }
    /// <summary>Maximum UTF-16 length; insertion never splits a grapheme.</summary>
    public int MaximumLength
    {
        get => maximumLength;
        set { ArgumentOutOfRangeException.ThrowIfNegative(value); maximumLength = value; history.Clear(); Value = this.value; }
    }
    public int Caret => caret;
    public int SelectionStart => Math.Min(anchor, caret);
    public int SelectionLength => Math.Abs(anchor - caret);
    public string SelectedText => value.Substring(SelectionStart, SelectionLength);
    public bool IsPlaceholder => value.Length == 0;
    public void SelectAll() { CaretUpstream = false; anchor = 0; caret = value.Length; InvalidateCaret(); }
    public void Select(int start, int length)
    {
        if (start < 0 || length < 0 || start > value.Length - length) throw new ArgumentOutOfRangeException(nameof(start));
        CaretUpstream = false; anchor = Boundary(start); caret = Boundary(start + length); InvalidateCaret();
    }
    /// <summary>Insert at the current selection as an undoable edit.</summary>
    public void InsertText(string text) { ArgumentNullException.ThrowIfNull(text); Insert(text); }

    internal void Insert(string text)
    {
        if (ReadOnly) return;
        var insert = Clean(text, false);
        var capacity = maximumLength - (value.Length - SelectionLength);
        insert = Truncate(insert, Math.Max(0, capacity));
        if (insert.Length == 0) return;
        var start = SelectionStart;
        Replace(start, SelectionLength, insert);
    }
    internal bool Key(UiKey key, bool shift, bool command, IUiClipboard? clipboard, ITextMetrics metrics, bool wordNavigation)
    {
        wordNavigation &= !IsPassword;
        if (command)
        {
            switch (key)
            {
                case UiKey.Z: if (shift) Redo(); else Undo(); return true;
                case UiKey.Y: Redo(); return true;
                case UiKey.A: SelectAll(); return true;
                case UiKey.C: if (!IsPassword && SelectionLength > 0 && clipboard != null) clipboard.SetText(SelectedText); return true;
                case UiKey.X:
                    if (!IsPassword && !ReadOnly && SelectionLength > 0 && clipboard != null) { clipboard.SetText(SelectedText); DeleteSelection(); }
                    return true;
                case UiKey.V: if (!ReadOnly && clipboard != null) Insert(clipboard.GetText() ?? ""); return true;
            }
        }
        switch (key)
        {
            case UiKey.Left: Move(!shift && SelectionLength > 0 ? SelectionStart : wordNavigation ? new TextWordBoundaries(value).Previous(caret) : Previous(caret), shift); break;
            case UiKey.Right: Move(!shift && SelectionLength > 0 ? SelectionStart + SelectionLength : wordNavigation ? new TextWordBoundaries(value).Next(caret) : Next(caret), shift); break;
            case UiKey.Home: Move(IsMultiline && !command ? geometry.LineAt(caret).Start : 0, shift); break;
            case UiKey.End:
                var line = geometry.LineAt(caret);
                var end = IsMultiline && !command ? line.End : value.Length;
                Move(end, shift, IsMultiline && !command && end < value.Length && value[end] != '\n'); break;
            case UiKey.Up: MoveVertical(-1, shift, metrics); break;
            case UiKey.Down: MoveVertical(1, shift, metrics); break;
            case UiKey.PageUp: MoveVertical(-Math.Max(1, (int)(owner.ContentBounds.Height / owner.Style.LineHeight)), shift, metrics); break;
            case UiKey.PageDown: MoveVertical(Math.Max(1, (int)(owner.ContentBounds.Height / owner.Style.LineHeight)), shift, metrics); break;
            case UiKey.Backspace:
                if (!ReadOnly && !DeleteSelection() && caret > 0) Replace(Previous(caret), caret - Previous(caret), "");
                break;
            case UiKey.Delete:
                if (!ReadOnly && !DeleteSelection() && caret < value.Length) Replace(caret, Next(caret) - caret, "");
                break;
            case UiKey.Enter: if (IsMultiline && !command) Insert("\n"); else Submitted?.Invoke(this); break;
        }
        // Space and character keys arrive separately through TextInput, never activate the control.
        return true;
    }
    internal void MovePointer(float x, float y, ITextMetrics metrics, bool extend)
    {
        var hit = geometry.Hit(x, y, metrics); Move(hit.Index, extend, hit.Upstream);
    }
    internal void SelectPointerWord(float x, float y, ITextMetrics metrics)
    {
        pointerWord = IsPassword ? (Start: 0, End: value.Length) : new TextWordBoundaries(value).At(geometry.HitWord(x, y, metrics));
        Select(pointerWord.Start, pointerWord.End - pointerWord.Start);
    }
    internal void DragPointerWord(float x, float y, ITextMetrics metrics)
    {
        var word = IsPassword ? (Start: 0, End: value.Length) : new TextWordBoundaries(value).At(geometry.HitWord(x, y, metrics));
        if (word.Start < pointerWord.Start) { anchor = pointerWord.End; caret = word.Start; }
        else { anchor = pointerWord.Start; caret = Math.Max(pointerWord.End, word.End); }
        CaretUpstream = false;
        InvalidateCaret();
    }
    private void MoveVertical(int rows, bool extend, ITextMetrics metrics)
    {
        if (!IsMultiline) return;
        var x = preferredX ?? geometry.CaretX(caret, metrics);
        var target = geometry.Vertical(caret, rows, x, metrics);
        Move(target.Index, extend, target.Upstream);
        preferredX = x;
    }
    internal void Arrange(ITextMetrics metrics)
    {
        geometry.Arrange(metrics, revealCaret);
        revealCaret = false;
    }
    public UiRect TextBounds => geometry.TextBounds;
    public UiRect CaretBounds => geometry.CaretBounds;
    public UiRect SelectionBounds => SelectionRects.FirstOrDefault();
    public IReadOnlyList<UiRect> SelectionRects => geometry.SelectionRects;
    public IReadOnlyList<string> DisplayLines => geometry.DisplayLines;
    internal void RevealCaret() { revealCaret = true; owner.Invalidate(); }
    private void InvalidateCaret() { preferredX = null; RevealCaret(); }
    private void Move(int position, bool extend, bool upstream = false) { CaretUpstream = upstream; caret = position; if (!extend) anchor = caret; InvalidateCaret(); }
    private bool DeleteSelection() { if (SelectionLength == 0) return false; Replace(SelectionStart, SelectionLength, ""); return true; }
    private void Replace(int start, int length, string insert)
    {
        var next = value.Remove(start, length).Insert(start, insert);
        var changed = value != next;
        if (changed) history.Record(State);
        // Inserting combining marks can join adjacent graphemes; snap to the next valid boundary.
        value = next;
        caret = StringInfo.ParseCombiningCharacters(value).Append(value.Length).First(i => i >= start + insert.Length);
        anchor = caret;
        Refresh();
        if (changed) Changed?.Invoke(this);
    }
    private int Boundary(int index) => StringInfo.ParseCombiningCharacters(value).Append(value.Length).Last(i => i <= index);
    private int Previous(int index) => StringInfo.ParseCombiningCharacters(value).Where(i => i < index).DefaultIfEmpty(0).Last();
    private int Next(int index) => StringInfo.ParseCombiningCharacters(value).Append(value.Length).First(i => i > index || i == value.Length);
    private string Clean(string text, bool limit = true)
    {
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');
        text = string.Concat(text.EnumerateRunes().Where(r => !System.Text.Rune.IsControl(r) || IsMultiline && r.Value == '\n').Select(r => r.ToString()));
        return limit ? Truncate(text, maximumLength) : text;
    }
    private static string Truncate(string text, int limit)
    {
        if (text.Length <= limit) return text;
        var end = StringInfo.ParseCombiningCharacters(text).Where(i => i <= limit).DefaultIfEmpty(0).Last();
        return text[..end];
    }
    private void Refresh() { CaretUpstream = false; owner.Text = IsPlaceholder ? placeholder : Mask(value); InvalidateCaret(); }
}
