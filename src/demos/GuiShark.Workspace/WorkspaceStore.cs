using System.Text.Json;

namespace GuiShark.Workspace;

internal sealed class WorkspaceStore(string path)
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, RespectNullableAnnotations = true };
    public string Path { get; } = System.IO.Path.GetFullPath(path);
    public string? RestoreError { get; private set; }

    public WorkspaceState Load(Func<WorkspaceState> create)
    {
        if (!File.Exists(Path)) return create();
        try
        {
            var saved = JsonSerializer.Deserialize<WorkspaceSnapshot>(File.ReadAllText(Path), Json);
            if (saved is null || saved.Version != 1) throw new InvalidDataException("Unsupported workspace snapshot.");
            Validate(saved.State);
            return saved.State;
        }
        catch (Exception error) when (error is IOException or JsonException or InvalidDataException or ArgumentException or UnauthorizedAccessException)
        {
            RestoreError = $"Saved layout was not loaded: {error.Message}. Original file kept.";
            return create();
        }
    }
    public void Save(WorkspaceState state)
    {
        // Never overwrite an unrecognized snapshot. Users can recover it or select another --state path.
        if (RestoreError is not null) return;
        var directory = System.IO.Path.GetDirectoryName(Path)!;
        Directory.CreateDirectory(directory);
        var temporary = Path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(new WorkspaceSnapshot(1, state), Json));
        File.Move(temporary, Path, true);
    }
    private static void Validate(WorkspaceState state)
    {
        if (state is null || state.Spaces is null || state.Spaces.Count is < 1 or > 32) throw new InvalidDataException("Invalid space count.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var space in state.Spaces) ValidateSpace(space, ids);
        if (state.Panes().Count(pane => pane.ManifestPath.Length > 0) > 16) throw new InvalidDataException("Too many app sessions.");
        if (!state.Spaces.Any(space => space.Id == state.ActiveSpaceId)) throw new InvalidDataException("Invalid active space.");
    }
    private static void ValidateSpace(WorkspaceSpace space, HashSet<string> ids)
    {
        if (space is null || space.Tabs is null) throw new InvalidDataException("Missing space or tabs.");
        CheckIdentity(space.Id, space.Name, ids);
        if (space.Tabs.Count is < 1 or > 32) throw new InvalidDataException("Invalid tab count.");
        foreach (var tab in space.Tabs) ValidateTab(tab, ids);
        if (!space.Tabs.Any(tab => tab.Id == space.ActiveTabId)) throw new InvalidDataException("Invalid active tab.");
    }
    private static void ValidateTab(WorkspaceTab tab, HashSet<string> ids)
    {
        if (tab is null || tab.Root is null) throw new InvalidDataException("Missing tab or split tree.");
        CheckIdentity(tab.Id, tab.Name, ids);
        ValidateNode(tab.Root, ids, 0);
        var panes = tab.Root.Panes().ToArray();
        if (panes.Length > 16) throw new InvalidDataException("Too many panes.");
        if (!panes.Any(pane => pane.Id == tab.ActivePaneId)) throw new InvalidDataException("Invalid active pane.");
        if (tab.ZoomedPaneId is not null && !panes.Any(pane => pane.Id == tab.ZoomedPaneId))
            throw new InvalidDataException("Invalid zoomed pane.");
    }
    private static void ValidateNode(LayoutNode node, HashSet<string> ids, int depth)
    {
        if (depth > 16 || !float.IsFinite(node.Ratio) || node.Ratio is < .05f or > .95f)
            throw new InvalidDataException("Invalid split tree.");
        if (node.Pane is { } pane)
        {
            if (node.First is not null || node.Second is not null) throw new InvalidDataException("Invalid leaf.");
            CheckIdentity(pane.Id, pane.Name, ids);
            if (pane.ManifestPath.Length > 4096) throw new InvalidDataException("Invalid app path.");
            return;
        }
        if (node.First is null || node.Second is null) throw new InvalidDataException("Missing split branch.");
        ValidateNode(node.First, ids, depth + 1);
        ValidateNode(node.Second, ids, depth + 1);
    }
    private static void CheckIdentity(string id, string name, HashSet<string> ids)
    {
        if (string.IsNullOrEmpty(id) || id.Length > 64 || string.IsNullOrEmpty(name) || name.Length > 100 || !ids.Add(id))
            throw new InvalidDataException("Invalid or duplicate identity.");
    }
    private sealed record WorkspaceSnapshot(int Version, WorkspaceState State);
}
