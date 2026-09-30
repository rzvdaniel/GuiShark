namespace GuiShark;

/// <summary>Single-choice dropdown state. Options are declared in HTML.</summary>
public sealed class UiSelect
{
    private readonly UiElement element;
    private int selectedIndex;
    internal UiSelect(UiElement element, int initialIndex)
    {
        this.element = element;
        selectedIndex = initialIndex;
        Apply();
    }
    public IReadOnlyList<UiElement> Options => element.Children;
    public UiElement? SelectedOption => selectedIndex >= 0 ? Options[selectedIndex] : null;
    public string Value => SelectedOption?.OptionValue ?? "";
    public int SelectedIndex
    {
        get => selectedIndex;
        set
        {
            if (value < -1 || value >= Options.Count) throw new ArgumentOutOfRangeException(nameof(value));
            if (selectedIndex == value) return;
            selectedIndex = value;
            Apply();
            element.Invalidate();
            Changed?.Invoke(this);
        }
    }
    private void Apply()
    {
        for (var i = 0; i < Options.Count; i++) Options[i].IsSelected = i == selectedIndex;
        element.Text = SelectedOption?.Text ?? "";
    }
    public event Action<UiSelect>? Changed;
}
