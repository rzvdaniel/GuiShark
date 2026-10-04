namespace GuiShark.Workspace;

internal sealed record WorkspacePrompt(string Title, string Initial, Action<string> Accept);

internal sealed class WorkspaceController(WorkspaceState state, WorkspaceOptions options)
{
    public WorkspaceState State { get; } = state;
    public bool Dirty { get; set; } = true;
    public bool NeedsSave { get; set; }
    public WorkspacePrompt? Prompt { get; private set; }
    public bool MenuOpen { get; set; }
    public bool TabChooserOpen { get; set; }
    public bool HasOverlay => Prompt is not null || MenuOpen || TabChooserOpen;
    public string Notice { get; set; } = "Right-click a pane header for actions · Ctrl+Shift shortcuts";

    public void Change(Action action)
    {
        action();
        Dirty = NeedsSave = true;
        MenuOpen = TabChooserOpen = false;
    }
    public void Ask(string title, string initial, Action<string> accept)
    {
        Prompt = new(title, initial, accept);
        MenuOpen = TabChooserOpen = false;
        Dirty = true;
    }
    public void Accept(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) { Notice = "Enter a value."; return; }
        var prompt = Prompt!;
        try
        {
            prompt.Accept(value.Trim());
            Prompt = null;
            Dirty = NeedsSave = true;
        }
        catch (Exception error) when (error is ArgumentException or IOException)
        {
            Notice = error.Message;
        }
    }
    public void Cancel() { Prompt = null; MenuOpen = TabChooserOpen = false; Dirty = true; }
    public void NewSpace() => Ask("Create space", "My space", name =>
    {
        if (State.Spaces.Count >= 32) throw new ArgumentException("Maximum 32 spaces.");
        State.AddSpace(Name(name));
    });
    public void NewTab() => Ask("Create tab", "New tab", name =>
    {
        if (State.ActiveSpace.Tabs.Count >= 32) throw new ArgumentException("Maximum 32 tabs per space.");
        State.AddTab(Name(name));
    });
    public void RenameSpace() => Ask("Rename space", State.ActiveSpace.Name, name => State.ActiveSpace.Name = Name(name));
    public void RenameTab() => Ask("Rename tab", State.ActiveTab.Name, name => State.ActiveTab.Name = Name(name));
    public void RenamePane() => Ask("Rename pane", State.ActivePane.Name, name => State.ActivePane.Name = Name(name));
    public void CloseSpace() => Ask("Close space and stop its apps? Type close", "", value => Confirm(value, State.CloseSpace));
    public void CloseTab() => Ask("Close tab and stop its apps? Type close", "", value => Confirm(value, State.CloseTab));
    public void ClosePane() => Ask("Close pane and stop its app? Type close", "", value => Confirm(value, State.ClosePane));
    public void LoadAurora() => Load(options.Aurora);
    public void LoadPulse() => Load(options.Pulse);
    public void Browse() => Ask("Open local app.json (starts trusted code)", "", Load);
    private void Load(string path)
    {
        var full = Path.GetFullPath(path);
        if (!File.Exists(full)) throw new FileNotFoundException("Cannot find app.json", full);
        if (State.Panes().Count(pane => pane.ManifestPath.Length > 0) >= 16 && State.ActivePane.ManifestPath.Length == 0)
            throw new ArgumentException("Maximum 16 running apps in this prototype.");
        var package = ProcessHosting.AppPackage.Load(full);
        Change(() => { State.ActivePane.ManifestPath = full; State.ActivePane.Name = package.Title; });
    }
    public void Split(bool down)
    {
        if (State.ActiveTab.Root.Panes().Count() >= 16) { Notice = "Maximum 16 panes per tab."; return; }
        Change(() => State.Split(down));
    }
    private static string Name(string value) => value.Length <= 100 ? value : throw new ArgumentException("Names must be at most 100 characters.");
    private static void Confirm(string value, Action action)
    {
        if (!value.Equals("close", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Type close to confirm.");
        action();
    }
}
