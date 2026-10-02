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
    private readonly TextComposition composition;
    private bool revealCaret = true;
    private bool showPassword;
    private bool readOnly;
    private float? preferredX;
    private (int Start, int End) pointerWord;
    internal bool CaretUpstream { get; private set; }
    internal bool CaretTrailing { get; private set; }
    internal bool DisplayCaretTrailing => Composition is { } state ? state.SelectionStart + state.SelectionLength > 0 : CaretTrailing;
    private int maximumLength = int.MaxValue;
    public event Action<UiTextInput>? Changed;
    public event Action<UiTextInput>? Submitted;
    internal UiTextInput(UiElement owner, bool multiline = false, bool password = false)
    {
        this.owner = owner;
        IsMultiline = multiline;
        IsPassword = password;
        geometry = new(owner, this);
        composition = new(this);
    }
    public bool IsMultiline { get; }
    public bool IsPassword { get; }
    public UiComposition? Composition => composition.State;
    public event Action<UiTextInput>? CompositionChanged;
    internal string DisplayValue => composition.DisplayValue;
    internal int DisplayCaret => composition.DisplayCaret;
    internal int CompositionStart => composition.Start;
    public IReadOnlyList<UiRect> CompositionRects => geometry.CompositionRects;
    internal void UpdateComposition(string text, int selectionStart, int selectionLength)
    {
        if (ReadOnly) return;
        if (text.Length == 0) { CancelComposition(); return; }
        composition.Update(text, selectionStart, selectionLength);
        Refresh();
        CompositionChanged?.Invoke(this);
    }
    public void CancelComposition()
    {
        if (!composition.Clear()) return;
        Refresh();
        CaretUpstream = composition.Upstream; CaretTrailing = composition.Trailing;
        CompositionChanged?.Invoke(this);
    }
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
    private TextEditState State => new(value, anchor, caret, CaretUpstream, CaretTrailing);
    public void Undo() { CancelComposition(); if (CanUndo) Restore(history.Undo(State)); }
    public void Redo() { CancelComposition(); if (CanRedo) Restore(history.Redo(State)); }
    private void Restore(TextEditState state)
    {
        value = state.Value; anchor = state.Anchor; caret = state.Caret;
        Refresh(); CaretUpstream = state.Upstream; CaretTrailing = state.Trailing; Changed?.Invoke(this);
    }
    public string Value
    {
        get => value;
        set
        {
            CancelComposition();
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
    public bool ReadOnly
    {
        get => readOnly;
        set
        {
            if (readOnly == value) return;
            readOnly = value;
            if (readOnly) CancelComposition();
            owner.Invalidate();
        }
    }
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
    public bool IsPlaceholder => value.Length == 0 && Composition == null;
    public void SelectAll() { CancelComposition(); CaretUpstream = false; CaretTrailing = false; anchor = 0; caret = value.Length; InvalidateCaret(); }
    public void Select(int start, int length)
    {
        if (start < 0 || length < 0 || start > value.Length - length) throw new ArgumentOutOfRangeException(nameof(start));
        CancelComposition();
        CaretUpstream = false; CaretTrailing = false; anchor = Boundary(start); caret = Boundary(start + length); InvalidateCaret();
    }
    /// <summary>Insert at the current selection as an undoable edit.</summary>
    public void InsertText(string text) { ArgumentNullException.ThrowIfNull(text); Insert(text); }

    internal void Insert(string text)
    {
        CancelComposition();
        if (ReadOnly) return;
        var insert = Clean(text, false);
        var capacity = maximumLength - (value.Length - SelectionLength);
        insert = Truncate(insert, Math.Max(0, capacity));
        if (insert.Length == 0) return;
        var start = SelectionStart;
        Replace(start, SelectionLength, insert);
    }
    internal bool Key(UiKey key, bool shift, bool command, IUiClipboard? clipboard, bool wordNavigation)
    {
        wordNavigation &= !IsPassword;
        if (command && HandleCommand(key, shift, clipboard)) return true;
        if (HandleNavigation(key, shift, command, wordNavigation)) return true;
        if (key == UiKey.Backspace) DeleteBackward();
        if (key == UiKey.Delete) DeleteForward();
        if (key == UiKey.Enter)
        {
            if (IsMultiline && !command) Insert("\n");
            else Submitted?.Invoke(this);
        }
        // Space and character keys arrive separately through TextInput, never activate the control.
        return true;
    }
    private bool HandleCommand(UiKey key, bool shift, IUiClipboard? clipboard)
    {
        switch (key)
        {
            case UiKey.Z: if (shift) Redo(); else Undo(); return true;
            case UiKey.Y: Redo(); return true;
            case UiKey.A: SelectAll(); return true;
            case UiKey.C: CopySelection(clipboard); return true;
            case UiKey.X: CutSelection(clipboard); return true;
            case UiKey.V: if (!ReadOnly && clipboard != null) Insert(clipboard.GetText() ?? ""); return true;
            default: return false;
        }
    }
    private void CopySelection(IUiClipboard? clipboard)
    {
        if (!IsPassword && SelectionLength > 0 && clipboard != null) clipboard.SetText(SelectedText);
    }
    private void CutSelection(IUiClipboard? clipboard)
    {
        if (IsPassword || ReadOnly || SelectionLength == 0 || clipboard == null) return;
        clipboard.SetText(SelectedText);
        DeleteSelection();
    }
    private bool HandleNavigation(UiKey key, bool shift, bool command, bool words)
    {
        var page = Math.Max(1, (int)(owner.ContentBounds.Height / owner.Style.LineHeight));
        switch (key)
        {
            case UiKey.Left: MoveHorizontal(false, shift, words); break;
            case UiKey.Right: MoveHorizontal(true, shift, words); break;
            case UiKey.Home: MoveToEdge(false, shift, command); break;
            case UiKey.End: MoveToEdge(true, shift, command); break;
            case UiKey.Up: MoveVertical(-1, shift); break;
            case UiKey.Down: MoveVertical(1, shift); break;
            case UiKey.PageUp: MoveVertical(-page, shift); break;
            case UiKey.PageDown: MoveVertical(page, shift); break;
            default: return false;
        }
        return true;
    }
    private void MoveHorizontal(bool right, bool shift, bool words)
    {
        if (!shift && SelectionLength > 0) { Move(geometry.SelectionEdge(right), false); return; }
        var position = new TextEditPosition(caret, CaretUpstream, CaretTrailing);
        Move(words ? geometry.Word(position, right) : geometry.Horizontal(position, right), shift);
    }
    private void MoveToEdge(bool right, bool shift, bool command)
    {
        if (command) Move(right ? value.Length : 0, shift);
        else Move(geometry.Edge(caret, right), shift);
    }
    private void DeleteBackward()
    {
        if (ReadOnly || DeleteSelection() || caret == 0) return;
        Replace(Previous(caret), caret - Previous(caret), "");
    }
    private void DeleteForward()
    {
        if (ReadOnly || DeleteSelection() || caret >= value.Length) return;
        Replace(caret, Next(caret) - caret, "");
    }
    internal void MovePointer(float x, float y, bool extend)
    {
        var hit = geometry.Hit(x, y); Move(hit, extend);
    }
    internal void SelectPointerWord(float x, float y)
    {
        pointerWord = IsPassword ? (Start: 0, End: value.Length) : new TextWordBoundaries(value).At(geometry.HitWord(x, y));
        Select(pointerWord.Start, pointerWord.End - pointerWord.Start);
    }
    internal void DragPointerWord(float x, float y)
    {
        var word = IsPassword ? (Start: 0, End: value.Length) : new TextWordBoundaries(value).At(geometry.HitWord(x, y));
        if (word.Start < pointerWord.Start) { anchor = pointerWord.End; caret = word.Start; }
        else { anchor = pointerWord.Start; caret = Math.Max(pointerWord.End, word.End); }
        CaretUpstream = false; CaretTrailing = false;
        InvalidateCaret();
    }
    private void MoveVertical(int rows, bool extend)
    {
        if (!IsMultiline) return;
        var x = preferredX ?? geometry.CaretX(caret);
        var target = geometry.Vertical(caret, rows, x);
        Move(target, extend);
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
    public IReadOnlyList<TextLineContext> DisplayContexts => geometry.DisplayContexts;
    internal void RevealCaret() { revealCaret = true; owner.Invalidate(); }
    private void InvalidateCaret() { preferredX = null; RevealCaret(); }
    private void Move(TextEditPosition position, bool extend) => Move(position.Index, extend, position.Upstream, position.Trailing);
    private void Move(int position, bool extend, bool upstream = false, bool trailing = false) { CaretTrailing = trailing; CaretUpstream = upstream; caret = position; if (!extend) anchor = caret; InvalidateCaret(); }
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
    private void Refresh() { CaretUpstream = false; CaretTrailing = false; owner.Text = DisplayValue.Length == 0 ? placeholder : Mask(DisplayValue); InvalidateCaret(); }
}
