using System.Globalization;
using System.Net;
using System.Text;

namespace GuiShark.Workspace;

// The shell itself is ordinary GuiShark HTML/CSS. Absolute boxes express the external split model.
internal sealed class ChromeMarkup(WorkspaceController controller, WorkspaceLayout layout, Action<string> sessionCommand, IReadOnlyDictionary<string, PaneSession> sessions)
{
    private readonly StringBuilder html = new();
    public Dictionary<string, Action> Actions { get; } = [];
    private float width;
    private float height;

    public string Build(float clientWidth, float clientHeight)
    {
        width = clientWidth;
        height = clientHeight;
        html.Clear();
        Actions.Clear();
        html.Append("<!doctype html><html><head><link rel='stylesheet' href='workspace.css'></head><body>");
        Sidebar();
        Tabs();
        Toolbar();
        foreach (var pane in layout.Panes) Pane(pane);
        foreach (var split in layout.Splits) Box("div", "divider", split.Divider, "");
        Box("span", "label", new(214, height - 23, Math.Max(0, width - 230), 20), "", "notice");
        html.Append("</body></html>");
        return html.ToString();
    }
    public string BuildOverlay()
    {
        html.Clear();
        html.Append("<!doctype html><html><head><link rel='stylesheet' href='workspace.css'></head><body class='overlay'>");
        if (controller.MenuOpen) Menu();
        if (controller.TabChooserOpen) TabChooser();
        if (controller.Prompt is not null) Prompt();
        html.Append("</body></html>");
        return html.ToString();
    }
    private void Sidebar()
    {
        Box("div", "sidebar", new(12, 12, 188, Math.Max(0, height - 24)), "");
        Box("span", "brand", new(24, 25, 162, 28), "GuiShark");
        Box("span", "label", new(24, 61, 164, 18), "APPLICATION WORKSPACE");
        Box("span", "label", new(24, 100, 162, 18), "SPACES");
        html.Append($"<div class='box space-list' style='{Style(new(23, 127, 166, Math.Max(80, height - 330)))}'>");
        foreach (var space in controller.State.Spaces)
        {
            var active = space.Id == controller.State.ActiveSpaceId;
            var id = $"action{Actions.Count}";
            Actions.Add(id, () => controller.Change(() => controller.State.ActiveSpaceId = space.Id));
            html.Append($"<button id='{id}' class='space-item {(active ? "active" : "")}'>{Encode(space.Name)}</button>");
        }
        html.Append("</div>");
        Button("+ New space", new(23, height - 156, 166, 28), controller.NewSpace);
        Button("Rename space", new(23, height - 110, 166, 28), controller.RenameSpace);
        Button("Close space", new(23, height - 72, 166, 28), controller.CloseSpace);
    }
    private void Tabs()
    {
        var x = 212f;
        var tabs = controller.State.ActiveSpace.Tabs;
        // Keep a real tab strip for a few tabs and an accessible chooser when it would overflow.
        if (tabs.Count > 4)
        {
            Button("Choose tab…", new(x, 16, Math.Max(220, width - 420), 30), () =>
            {
                controller.TabChooserOpen = true;
                controller.MenuOpen = false;
                controller.Dirty = true;
            });
            x = width - 196;
        }
        else
        {
            foreach (var tab in tabs)
            {
                var size = Math.Clamp(tab.Name.Length * 7 + 24, 100, 150);
                Button(tab.Name, new(x, 16, size, 30), () => controller.Change(() => controller.State.ActiveSpace.ActiveTabId = tab.Id),
                    tab.Id == controller.State.ActiveSpace.ActiveTabId);
                x += size + 6;
            }
        }
        Button("+", new(x, 16, 32, 30), controller.NewTab);
        Button("Tab actions", new(width - 144, 16, 132, 30), () => { controller.MenuOpen = !controller.MenuOpen; controller.Dirty = true; });
    }
    private void Toolbar()
    {
        var x = 212f;
        void Item(string name, int size, Action action) { Button(name, new(x, 60, size, 30), action); x += size + 6; }
        Item("Split right", 100, () => controller.Split(false));
        Item("Split down", 100, () => controller.Split(true));
        Item(controller.State.ActiveTab.ZoomedPaneId is null ? "Zoom pane" : "Restore panes", 118,
            () => controller.Change(controller.State.ToggleZoom));
        Item("Open app…", 104, controller.Browse);
        Item("Aurora", 76, controller.LoadAurora);
        Item("Pulse", 68, controller.LoadPulse);
        Item("Restart", 78, () => sessionCommand("restart"));
        Item("Actions", 78, () => { controller.MenuOpen = !controller.MenuOpen; controller.Dirty = true; });
    }
    private void Pane(PanePlacement placement)
    {
        var pane = placement.Pane;
        var bounds = placement.Bounds;
        var active = pane.Id == controller.State.ActiveTab.ActivePaneId;
        Box("div", active ? "pane active" : "pane", bounds, "");
        Box("div", "header", new(bounds.X + 2, bounds.Y + 2, Math.Max(0, bounds.Width - 4), 28), pane.Name);
        Box("span", "status", new(bounds.X + 2, bounds.Bottom - 23, Math.Max(0, bounds.Width - 4), 21), "", "status-" + pane.Id);
        if (sessions.TryGetValue(pane.Id, out var session) && session.HasFailure)
        {
            Box("div", "empty", new(bounds.X + 16, bounds.Y + 50, Math.Max(0, bounds.Width - 32), 58), "This app stopped. Other apps are still running.");
            Button("Restart app", new(bounds.X + 24, bounds.Y + 120, 130, 32), () =>
            {
                controller.Change(() => controller.State.Focus(pane.Id));
                sessionCommand("restart");
            });
        }
        else if (pane.ManifestPath.Length == 0)
        {
            Box("div", "empty", new(bounds.X + 16, bounds.Y + 54, Math.Max(0, bounds.Width - 32), 54), "Choose an app for this pane");
            Button("Aurora", new(bounds.X + 24, bounds.Y + 112, 98, 30), () => { controller.State.Focus(pane.Id); controller.LoadAurora(); });
            Button("Pulse", new(bounds.X + 130, bounds.Y + 112, 90, 30), () => { controller.State.Focus(pane.Id); controller.LoadPulse(); });
        }
    }
    private void TabChooser()
    {
        var x = 212f;
        Box("div", "menu", new(x, 52, 380, Math.Min(height - 80, 500)), "");
        html.Append($"<div class='box space-list' style='{Style(new(x + 10, 62, 360, Math.Min(height - 100, 480)))}'>");
        foreach (var tab in controller.State.ActiveSpace.Tabs)
        {
            var id = $"action{Actions.Count}";
            Actions.Add(id, () => controller.Change(() => controller.State.ActiveSpace.ActiveTabId = tab.Id));
            html.Append($"<button id='{id}' class='space-item {(tab.Id == controller.State.ActiveSpace.ActiveTabId ? "active" : "")}'>{Encode(tab.Name)}</button>");
        }
        html.Append("</div>");
    }
    private void Menu()
    {
        var x = Math.Max(212, width - 246);
        Box("div", "menu", new(x, 98, 234, 424), "");
        var y = 108f;
        void Item(string title, Action action) { Button(title, new(x + 10, y, 214, 28), action); y += 34; }
        Item("Rename pane", controller.RenamePane);
        Item("Close pane", controller.ClosePane);
        Item("Rename tab", controller.RenameTab);
        Item("Close tab", controller.CloseTab);
        Item("Freeze selected app (5s)", () => { sessionCommand("freeze"); controller.Cancel(); });
        Item("Crash selected app", () => { sessionCommand("crash"); controller.Cancel(); });
        Item("Split right", () => controller.Split(false));
        Item("Split down", () => controller.Split(true));
        Item("Restart selected app", () => { sessionCommand("restart"); controller.Cancel(); });
        Item("Open local app.json…", controller.Browse);
        Item("New tab", controller.NewTab);
        Item("Dismiss", controller.Cancel);
    }
    private void Prompt()
    {
        var prompt = controller.Prompt!;
        Box("div", "scrim", new(0, 0, width, height), "");
        var x = Math.Max(0, (width - 600) / 2);
        var y = Math.Max(0, (height - 220) / 2);
        Box("div", "prompt", new(x, y, 600, 220), "");
        Box("span", "header", new(x + 20, y + 20, 560, 40), prompt.Title);
        html.Append($"<input id='prompt-value' value='{Encode(prompt.Initial)}' style='{Style(new(x + 20, y + 76, 560, 38))}' />");
        Button("Cancel", new(x + 350, y + 140, 100, 32), controller.Cancel);
        // Submission is bound by WorkspaceChrome, which owns the live input value.
        Button("Confirm", new(x + 462, y + 140, 118, 32), () => sessionCommand("confirm"));
    }
    private void Button(string title, UiRect rect, Action action, bool active = false)
    {
        var id = $"action{Actions.Count}";
        Actions.Add(id, action);
        html.Append($"<button id='{id}' class='{(active ? "active" : "")}' style='{Style(rect)}'>{Encode(title)}</button>");
    }
    private void Box(string tag, string classes, UiRect rect, string text, string? id = null) =>
        html.Append($"<{tag} class='box {classes}' id='{Encode(id ?? "")}' style='{Style(rect)}'>{Encode(text)}</{tag}>");
    private static string Encode(string value) => WebUtility.HtmlEncode(value);
    private static string Style(UiRect rect) => string.Create(CultureInfo.InvariantCulture,
        $"left:{rect.X}px;top:{rect.Y}px;width:{Math.Max(0, rect.Width)}px;height:{Math.Max(0, rect.Height)}px;");
}
