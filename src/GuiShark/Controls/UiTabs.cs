namespace GuiShark;

/// <summary>HTML button tabs associated with panels through aria-controls.</summary>
public sealed class UiTabs
{
    private readonly IReadOnlyList<UiElement> panels;
    public IReadOnlyList<UiElement> Tabs { get; }
    public UiElement Selected { get; private set; }
    internal UiTabs(UiElement list, UiDocument document)
    {
        Tabs = list.Children.Where(e => e.Role == "tab" && e.IsButton).ToArray();
        if (Tabs.Count == 0) throw new FormatException("A tablist requires direct button children with role=tab.");
        panels = Tabs.Select(e => document.GetElement(e.ControlTargetId ?? throw new FormatException("Tabs require aria-controls."))).ToArray();
        if (panels.Distinct().Count() != panels.Count || panels.Any(e => e.Role != "tabpanel"))
            throw new FormatException("Each tab requires a distinct role=tabpanel target.");
        foreach (var tab in Tabs) tab.TabGroup = this;
        Selected = Tabs.FirstOrDefault(e => e.IsSelected && !e.Disabled) ?? Tabs.FirstOrDefault(e => !e.Disabled) ?? Tabs[0];
        Apply();
    }
    public void Select(UiElement tab)
    {
        if (!Tabs.Contains(tab)) throw new ArgumentException("Tab belongs to a different group.", nameof(tab));
        if (tab.Disabled || tab.Hidden || Selected == tab) return;
        Selected = tab;
        Apply();
        Changed?.Invoke(this);
    }
    internal void Validate()
    {
        if (!Selected.Disabled && !Selected.Hidden) return;
        var next = Tabs.FirstOrDefault(e => !e.Disabled && !e.Hidden);
        if (next != null) Select(next);
    }
    public event Action<UiTabs>? Changed;
    private void Apply()
    {
        for (var i = 0; i < Tabs.Count; i++)
        {
            Tabs[i].IsSelected = Tabs[i] == Selected;
            panels[i].Hidden = Tabs[i] != Selected;
            Tabs[i].Invalidate();
        }
    }
}
