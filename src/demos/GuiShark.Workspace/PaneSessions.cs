using GuiShark.AppProtocol;
using GuiShark.ProcessHosting;
using OpenTK.Mathematics;

namespace GuiShark.Workspace;

internal sealed class PaneSessions : IDisposable
{
    private readonly Dictionary<string, PaneSession> sessions = [];
    public IReadOnlyDictionary<string, PaneSession> Items => sessions;
    public void Synchronize(WorkspaceState state)
    {
        var panes = state.Panes().Where(pane => pane.ManifestPath.Length > 0).ToDictionary(pane => pane.Id);
        foreach (var id in sessions.Keys.ToArray())
        {
            if (panes.TryGetValue(id, out var pane) && pane.ManifestPath == sessions[id].ManifestPath) continue;
            sessions[id].Dispose();
            sessions.Remove(id);
            failures.Remove(id);
        }
        foreach (var pane in panes.Values)
        {
            if (sessions.ContainsKey(pane.Id)) continue;
            var session = new PaneSession(pane);
            sessions.Add(pane.Id, session);
            session.Start();
        }
    }
    private readonly Dictionary<string, bool> failures = [];
    public bool Update()
    {
        var changed = false;
        foreach (var (id, session) in sessions)
        {
            session.Update();
            var failed = session.HasFailure;
            if (failures.GetValueOrDefault(id) != failed) changed = true;
            failures[id] = failed;
        }
        return changed;
    }
    public void Render(WorkspaceLayout layout, Vector2i client, Vector2i framebuffer)
    {
        foreach (var placement in layout.Panes)
            if (sessions.TryGetValue(placement.Pane.Id, out var session) && !session.HasFailure)
                session.Render(PanelViewport.FromBounds(placement.Content, client, framebuffer), framebuffer.Y);
    }
    public void Command(string paneId, string command)
    {
        if (!sessions.TryGetValue(paneId, out var session)) return;
        if (command == "restart") session.Start();
        else session.Send(new AppMessage(command));
    }
    public bool AllHaveFrames => sessions.Values.All(session => session.Frames >= 3 && !session.HasFailure && !session.Unresponsive);
    public void Dispose()
    {
        foreach (var session in sessions.Values) session.Dispose();
        sessions.Clear();
    }
}
