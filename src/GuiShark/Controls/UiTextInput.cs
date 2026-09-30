using System.Globalization;

namespace GuiShark;

/// <summary>Single-line editing state. Selection offsets are UTF-16 indices at grapheme boundaries.</summary>
public sealed class UiTextInput
{
    private readonly UiElement owner;
    private string value = "";
    private string placeholder = "";
    private int anchor;
    private int caret;
    private float offset;
    private int maximumLength = int.MaxValue;
    public event Action<UiTextInput>? Changed;
    public event Action<UiTextInput>? Submitted;
    internal UiTextInput(UiElement owner) => this.owner = owner;
    public string Value
    {
        get => value;
        set
        {
            var next = Clean(value ?? "");
            if (this.value == next) return;
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
        set { ArgumentOutOfRangeException.ThrowIfNegative(value); maximumLength = value; Value = this.value; }
    }
    public int Caret => caret;
    public int SelectionStart => Math.Min(anchor, caret);
    public int SelectionLength => Math.Abs(anchor - caret);
    public string SelectedText => value.Substring(SelectionStart, SelectionLength);
    public bool IsPlaceholder => value.Length == 0;
    public void SelectAll() { anchor = 0; caret = value.Length; owner.Invalidate(); }
    public void Select(int start, int length)
    {
        if (start < 0 || length < 0 || start > value.Length - length) throw new ArgumentOutOfRangeException(nameof(start));
        anchor = Boundary(start); caret = Boundary(start + length); owner.Invalidate();
    }
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
    internal bool Key(UiKey key, bool shift, bool command, IUiClipboard? clipboard)
    {
        if (command)
        {
            switch (key)
            {
                case UiKey.A: SelectAll(); return true;
                case UiKey.C: if (SelectionLength > 0 && clipboard != null) clipboard.SetText(SelectedText); return true;
                case UiKey.X:
                    if (!ReadOnly && SelectionLength > 0 && clipboard != null) { clipboard.SetText(SelectedText); DeleteSelection(); }
                    return true;
                case UiKey.V: if (!ReadOnly && clipboard != null) Insert(clipboard.GetText() ?? ""); return true;
            }
        }
        switch (key)
        {
            case UiKey.Left: Move(!shift && SelectionLength > 0 ? SelectionStart : Previous(caret), shift); break;
            case UiKey.Right: Move(!shift && SelectionLength > 0 ? SelectionStart + SelectionLength : Next(caret), shift); break;
            case UiKey.Home: Move(0, shift); break;
            case UiKey.End: Move(value.Length, shift); break;
            case UiKey.Backspace:
                if (!ReadOnly && !DeleteSelection() && caret > 0) Replace(Previous(caret), caret - Previous(caret), "");
                break;
            case UiKey.Delete:
                if (!ReadOnly && !DeleteSelection() && caret < value.Length) Replace(caret, Next(caret) - caret, "");
                break;
            case UiKey.Enter: Submitted?.Invoke(this); break;
        }
        // Space and character keys arrive separately through TextInput, never activate the control.
        return true;
    }
    internal void MovePointer(float x, ITextMetrics metrics, bool extend)
    {
        var local = x - owner.ContentBounds.X + offset;
        var positions = StringInfo.ParseCombiningCharacters(value).Append(value.Length).ToArray();
        var nearest = positions.MinBy(i => Math.Abs(Measure(value[..i], metrics) - local));
        Move(nearest, extend);
    }
    internal void Arrange(ITextMetrics metrics)
    {
        var width = owner.ContentBounds.Width;
        var x = Measure(value[..caret], metrics);
        if (x < offset) offset = x;
        if (x > offset + Math.Max(0, width - 2)) offset = x - Math.Max(0, width - 2);
        var textWidth = Measure(owner.Text, metrics);
        offset = Math.Clamp(offset, 0, Math.Max(0, textWidth + 2 - width));
        TextBounds = owner.ContentBounds with { X = owner.ContentBounds.X - offset, Width = Math.Max(width, textWidth + 2) };
        CaretBounds = new(owner.ContentBounds.X + x - offset, owner.ContentBounds.Y, 1, Math.Min(owner.Style.LineHeight, owner.ContentBounds.Height));
        var left = Measure(value[..SelectionStart], metrics);
        var right = Measure(value[..(SelectionStart + SelectionLength)], metrics);
        SelectionBounds = new(owner.ContentBounds.X + left - offset, owner.ContentBounds.Y, right - left, CaretBounds.Height);
    }
    public UiRect TextBounds { get; private set; }
    public UiRect CaretBounds { get; private set; }
    public UiRect SelectionBounds { get; private set; }
    private float Measure(string text, ITextMetrics metrics) => metrics.MeasureWidth(text, owner.Style.FontSize, owner.Style.Bold, owner.Style.FontFamily);
    private void Move(int position, bool extend) { caret = position; if (!extend) anchor = caret; owner.Invalidate(); }
    private bool DeleteSelection() { if (SelectionLength == 0) return false; Replace(SelectionStart, SelectionLength, ""); return true; }
    private void Replace(int start, int length, string insert)
    {
        var next = value.Remove(start, length).Insert(start, insert);
        var changed = value != next;
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
        text = string.Concat(text.EnumerateRunes().Where(r => !System.Text.Rune.IsControl(r)).Select(r => r.ToString()));
        return limit ? Truncate(text, maximumLength) : text;
    }
    private static string Truncate(string text, int limit)
    {
        if (text.Length <= limit) return text;
        var end = StringInfo.ParseCombiningCharacters(text).Where(i => i <= limit).DefaultIfEmpty(0).Last();
        return text[..end];
    }
    private void Refresh() { owner.Text = IsPlaceholder ? placeholder : value; owner.Invalidate(); }
}
