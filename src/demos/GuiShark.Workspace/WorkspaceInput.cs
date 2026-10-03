using GuiShark.AppProtocol;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace GuiShark.Workspace;

internal sealed class WorkspaceInput(WorkspaceController controller, WorkspaceLayout layout,
    WorkspaceChrome chrome, IReadOnlyDictionary<string, PaneSession> sessions, Action<string> command)
{
    private PanePlacement? captured;
    private string? focused;
    private SplitPlacement? dragging;
    private bool pendingChromeFocus;
    private readonly Dictionary<Keys, string> heldKeys = [];
    public void PointerDown(float x, float y, MouseButton button, bool shift)
    {
        if (controller.HasOverlay)
        {
            ReleaseFocus();
            if (button == MouseButton.Left) chrome.View.Input.PointerDown(x, y);
            return;
        }
        var split = layout.Splits.LastOrDefault(split => split.Divider.Contains(x, y));
        if (button == MouseButton.Left && split is not null) { ReleaseFocus(); dragging = split; return; }
        if (RoutePaneDown(x, y, button, shift)) return;
        ReleaseFocus();
        if (button == MouseButton.Left) chrome.View.Input.PointerDown(x, y);
    }
    private bool RoutePaneDown(float x, float y, MouseButton button, bool shift)
    {
        var pane = layout.Panes.FirstOrDefault(pane => pane.Bounds.Contains(x, y));
        if (pane is not null)
        {
            if (button == MouseButton.Right && y < pane.Content.Y)
            {
                ReleaseFocus();
                controller.Change(() => controller.State.Focus(pane.Pane.Id));
                controller.MenuOpen = true;
                return true;
            }
            if (button == MouseButton.Left && pane.Content.Contains(x, y) && sessions.TryGetValue(pane.Pane.Id, out var session) && session.AcceptsInput)
            {
                controller.Change(() => controller.State.Focus(pane.Pane.Id));
                if (focused != pane.Pane.Id) ReleaseFocus();
                focused = pane.Pane.Id;
                captured = pane;
                Pointer("pointer-down", pane, x, y, shift);
                return true;
            }
            if (button == MouseButton.Left)
            {
                // Do not rebuild the chrome between a host button's down/up events.
                controller.State.Focus(pane.Pane.Id);
                pendingChromeFocus = true;
            }
        }
        return false;
    }
    public void PointerMove(float x, float y)
    {
        if (dragging is not null)
        {
            WorkspaceLayout.Resize(dragging, x, y);
            controller.Dirty = controller.NeedsSave = true;
            return;
        }
        if (controller.HasOverlay) { chrome.View.Input.PointerMove(x, y); return; }
        var target = captured ?? layout.Panes.FirstOrDefault(pane => pane.Content.Contains(x, y));
        if (target is not null && sessions.ContainsKey(target.Pane.Id)) Pointer("pointer-move", target, x, y, false);
        else chrome.View.Input.PointerMove(x, y);
    }
    public void PointerUp(float x, float y, MouseButton button, bool shift)
    {
        if (button != MouseButton.Left) return;
        if (dragging is not null) { dragging = null; return; }
        if (captured is not null)
        {
            Pointer("pointer-up", captured, x, y, shift);
            captured = null;
        }
        else chrome.View.Input.PointerUp(x, y);
        if (pendingChromeFocus) { controller.Dirty = controller.NeedsSave = true; pendingChromeFocus = false; }
    }
    public void Wheel(float x, float y, float delta)
    {
        var target = layout.Panes.FirstOrDefault(pane => pane.Content.Contains(x, y));
        if (controller.HasOverlay || target is null)
        { chrome.View.Input.PointerWheel(x, y, delta); return; }
        if (sessions.TryGetValue(target.Pane.Id, out var session))
            session.Send(new AppMessage("pointer-wheel", X: x - target.Content.X, Y: y - target.Content.Y, Delta: delta));
    }
    private void Pointer(string type, PanePlacement pane, float x, float y, bool shift)
    {
        if (sessions.TryGetValue(pane.Pane.Id, out var session))
            session.Send(new AppMessage(type, X: x - pane.Content.X, Y: y - pane.Content.Y, Shift: shift));
    }
    public void KeyDown(KeyboardKeyEventArgs e)
    {
        if (controller.Prompt is null && !e.IsRepeat && e.Control && e.Shift && Shortcut(e.Key)) { ReleaseFocus(); return; }
        if (e.Key == Keys.Escape && (controller.HasOverlay))
        { controller.Cancel(); return; }
        if (focused is not null && !controller.HasOverlay && sessions.TryGetValue(focused, out var session))
        {
            session.Send(new AppMessage("key-down", Key: e.Key.ToString(), Shift: e.Shift, Command: e.Control, Repeat: e.IsRepeat));
            heldKeys[e.Key] = focused;
        }
        else if (Enum.TryParse<UiKey>(e.Key.ToString(), out var key)) chrome.View.Input.KeyDown(key, e.Shift, e.IsRepeat, e.Control);
    }
    public void KeyUp(Keys key)
    {
        if (heldKeys.Remove(key, out var id) && sessions.TryGetValue(id, out var session))
            session.Send(new AppMessage("key-up", Key: key.ToString()));
        else if (Enum.TryParse<UiKey>(key.ToString(), out var uiKey)) chrome.View.Input.KeyUp(uiKey);
    }
    public void Text(string text)
    {
        if (focused is not null && !controller.HasOverlay && sessions.TryGetValue(focused, out var session))
            session.Send(new AppMessage("text-input", Value: text));
        else chrome.View.Input.TextInput(text);
    }
    private bool Shortcut(Keys key)
    {
        switch (key)
        {
            case Keys.E: controller.Split(false); break;
            case Keys.D: controller.Split(true); break;
            case Keys.T: controller.NewTab(); break;
            case Keys.N: controller.NewSpace(); break;
            case Keys.Z: controller.Change(controller.State.ToggleZoom); break;
            case Keys.R: command("restart"); break;
            case Keys.W: controller.ClosePane(); break;
            case Keys.PageDown: CycleTab(1); break;
            case Keys.PageUp: CycleTab(-1); break;
            default: return false;
        }
        return true;
    }
    private void CycleTab(int direction)
    {
        var space = controller.State.ActiveSpace;
        var index = space.Tabs.FindIndex(tab => tab.Id == space.ActiveTabId);
        controller.Change(() => space.ActiveTabId = space.Tabs[(index + direction + space.Tabs.Count) % space.Tabs.Count].Id);
    }
    public void ReleaseFocus()
    {
        if (focused is not null && sessions.TryGetValue(focused, out var session)) session.Send(new AppMessage("focus-lost"));
        heldKeys.Clear();
        focused = null;
        captured = null;
        dragging = null;
        chrome.View.Input.Cancel();
    }
}
