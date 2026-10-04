namespace GuiShark.Workspace;

internal static class WorkspaceDefaults
{
    public static WorkspaceState Create(WorkspaceOptions options)
    {
        var state = new WorkspaceState();
        var studio = state.AddSpace("Studio");
        var overview = state.ActiveTab;
        SetApp(state.ActivePane, options.Aurora, "Aurora / One");
        state.Split(false);
        SetApp(state.ActivePane, options.Pulse, "Pulse / One");
        state.Split(true);
        SetApp(state.ActivePane, options.Aurora, "Aurora / Two");
        state.Focus(overview.Root.First!.Pane!.Id);
        state.Split(true);
        SetApp(state.ActivePane, options.Pulse, "Pulse / Two");
        state.AddTab("Experiments");
        studio.ActiveTabId = overview.Id;
        state.AddSpace("Personal");
        state.ActiveSpaceId = studio.Id;
        return state;
    }
    private static void SetApp(WorkspacePane pane, string path, string name)
    {
        pane.ManifestPath = path;
        pane.Name = name;
    }
}
