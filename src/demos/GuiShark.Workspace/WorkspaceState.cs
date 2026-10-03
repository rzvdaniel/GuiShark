using System.Text.Json.Serialization;

namespace GuiShark.Workspace;

// Serializable layout only. Processes and GL resources never belong to saved state.
internal sealed class WorkspaceState
{
    public List<WorkspaceSpace> Spaces { get; set; } = [];
    public string ActiveSpaceId { get; set; } = "";
    [JsonIgnore]
    public WorkspaceSpace ActiveSpace => Spaces.First(space => space.Id == ActiveSpaceId);
    [JsonIgnore]
    public WorkspaceTab ActiveTab => ActiveSpace.Tabs.First(tab => tab.Id == ActiveSpace.ActiveTabId);
    [JsonIgnore]
    public WorkspacePane ActivePane => ActiveTab.Root.Panes().First(pane => pane.Id == ActiveTab.ActivePaneId);

    public WorkspaceSpace AddSpace(string name)
    {
        var space = new WorkspaceSpace { Name = name };
        Spaces.Add(space);
        ActiveSpaceId = space.Id;
        AddTab("Overview");
        return space;
    }
    public WorkspaceTab AddTab(string name)
    {
        var tab = new WorkspaceTab { Name = name };
        tab.ActivePaneId = tab.Root.Pane!.Id;
        ActiveSpace.Tabs.Add(tab);
        ActiveSpace.ActiveTabId = tab.Id;
        return tab;
    }
    public WorkspacePane Split(bool down)
    {
        var node = ActiveTab.Root.Find(ActiveTab.ActivePaneId)!;
        var pane = new WorkspacePane();
        node.First = new LayoutNode { Pane = node.Pane };
        node.Second = new LayoutNode { Pane = pane };
        node.Pane = null;
        node.Down = down;
        node.Ratio = .5f;
        ActiveTab.ActivePaneId = pane.Id;
        ActiveTab.ZoomedPaneId = null;
        return pane;
    }
    public void ClosePane()
    {
        var tab = ActiveTab;
        tab.Root = tab.Root.Remove(tab.ActivePaneId) ?? new LayoutNode();
        tab.ActivePaneId = tab.Root.Panes().First().Id;
        tab.ZoomedPaneId = null;
    }
    public void CloseTab()
    {
        var space = ActiveSpace;
        space.Tabs.Remove(ActiveTab);
        if (space.Tabs.Count == 0) AddTab("Overview");
        else space.ActiveTabId = space.Tabs[0].Id;
    }
    public void CloseSpace()
    {
        Spaces.Remove(ActiveSpace);
        if (Spaces.Count == 0) AddSpace("My space");
        else ActiveSpaceId = Spaces[0].Id;
    }
    public IEnumerable<WorkspacePane> Panes() => Spaces.SelectMany(space => space.Tabs).SelectMany(tab => tab.Root.Panes());
    public void Focus(string paneId) => ActiveTab.ActivePaneId = paneId;
    public void ToggleZoom() => ActiveTab.ZoomedPaneId = ActiveTab.ZoomedPaneId is null ? ActiveTab.ActivePaneId : null;
}

internal sealed class WorkspaceSpace
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "My space";
    public string ActiveTabId { get; set; } = "";
    public List<WorkspaceTab> Tabs { get; set; } = [];
}
internal sealed class WorkspaceTab
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Overview";
    public string ActivePaneId { get; set; } = "";
    public string? ZoomedPaneId { get; set; }
    public LayoutNode Root { get; set; } = new();
}
internal sealed class WorkspacePane
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Empty pane";
    public string ManifestPath { get; set; } = "";
}
internal sealed class LayoutNode
{
    public WorkspacePane? Pane { get; set; } = new();
    public LayoutNode? First { get; set; }
    public LayoutNode? Second { get; set; }
    public bool Down { get; set; }
    public float Ratio { get; set; } = .5f;
    public IEnumerable<WorkspacePane> Panes() => Pane is not null ? [Pane] : First!.Panes().Concat(Second!.Panes());
    public LayoutNode? Find(string id)
    {
        if (Pane is null) return First!.Find(id) ?? Second!.Find(id);
        return Pane.Id == id ? this : null;
    }
    public LayoutNode? Remove(string id)
    {
        if (Pane is not null) return Pane.Id == id ? null : this;
        var first = First!.Remove(id);
        var second = Second!.Remove(id);
        if (first is null) return second;
        if (second is null) return first;
        First = first;
        Second = second;
        return this;
    }
}
