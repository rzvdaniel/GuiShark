using GuiShark.AppProtocol;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace GuiShark.Workspace;

// Opt-in bounded desktop smoke check; never runs during normal startup.
internal sealed class WorkspaceVerification(WorkspaceController controller, WorkspaceLayout layout,
    IReadOnlyDictionary<string, PaneSession> sessions, WorkspaceStore store, WorkspaceInput input, WorkspaceChrome chrome, Action close)
{
    private readonly WorkspaceUiVerification ui = new(controller, layout, chrome, input);
    private int step;
    private string target = "";
    private string peer = "";
    private long hostAtFreeze;
    private long peerAtFreeze;
    private double freezeAt;
    private int oldPid;
    private long framesAtResize;
    private double visibilityAt;
    private string originalTab = "";
    private Dictionary<string, long> hiddenFrames = [];
    private Dictionary<string, long> hiddenPublished = [];
    public void Update(double elapsed, long hostFrames)
    {
        if (elapsed > 35) throw new InvalidOperationException($"VERIFY FAIL: timeout at step {step}");
        if (step == 0 && sessions.Count == 4 && sessions.Values.All(session => session.Ready && session.Frames > 0))
        { if (ui.Update()) Begin(elapsed); }
        else if (step == 1 && sessions[target].SampleCount == 1)
        {
            Require(sessions.Values.Where(session => session.Id != sessions[target].Id).All(session => session.SampleCount == 0), "click leaked into another app");
            sessions[target].Send(new AppMessage("freeze"));
            step = 2;
        }
        else if (step == 2 && sessions[target].FreezeStarted)
        {
            hostAtFreeze = hostFrames;
            peerAtFreeze = sessions[peer].Frames;
            freezeAt = elapsed;
            step = 12;
        }
        else if (step == 12 && elapsed - freezeAt > 3)
        {
            Require(sessions[target].Unresponsive, "frozen child was not marked unresponsive");
            Require(hostFrames - hostAtFreeze > 30, "host stopped during freeze");
            Require(sessions[peer].Frames - peerAtFreeze > 3, "peer app stopped during freeze");
            Console.WriteLine("VERIFY: host and peer continued while one app froze");
            step = 3;
        }
        else if (step is >= 8 and <= 11) VerifyVisibility(elapsed);
        else VerifyRecovery(elapsed);
    }
    private void VerifyRecovery(double elapsed)
    {
        if (step == 3 && elapsed - freezeAt > 5 && !sessions[target].Unresponsive)
        {
            oldPid = sessions[target].Id;
            sessions[target].Send(new AppMessage("crash"));
            step = 4;
        }
        else if (step == 4 && sessions[target].Exited && ui.TryBeginRestart())
        {
            Require(!sessions[peer].Exited, "peer app crashed");
            step = 7;
        }
        else if (step == 7) { ui.EndRestart(); step = 5; }
        else if (step == 5 && sessions[target].Ready && sessions[target].Frames > 0)
        {
            Require(sessions[target].Id != oldPid, "restart did not create a new process");
            Require(sessions[target].SampleCount == 0, "restart retained counter unexpectedly");
            originalTab = controller.State.ActiveTab.Id;
            var other = controller.State.ActiveSpace.Tabs.First(tab => tab.Id != originalTab);
            controller.Change(() => controller.State.ActiveSpace.ActiveTabId = other.Id);
            visibilityAt = elapsed;
            step = 8;
        }
    }
    private void VerifyVisibility(double elapsed)
    {
        if (step == 8 && elapsed - visibilityAt > 1)
        {
            hiddenFrames = sessions.ToDictionary(pair => pair.Key, pair => pair.Value.Frames);
            hiddenPublished = sessions.ToDictionary(pair => pair.Key, pair => pair.Value.PublishedFrames);
            visibilityAt = elapsed;
            step = 9;
        }
        else if (step == 9 && elapsed - visibilityAt > 1)
        {
            Require(sessions.All(pair => pair.Value.Frames == hiddenFrames[pair.Key]), "hidden apps continued uploading frames");
            Require(sessions.All(pair => pair.Value.PublishedFrames == hiddenPublished[pair.Key]), "hidden apps continued publishing frames");
            Require(sessions.Values.All(session => !session.Unresponsive), "idle hidden app was marked unresponsive");
            controller.Change(() => controller.State.ActiveSpace.ActiveTabId = originalTab);
            step = 10;
        }
        else if (step == 10 && sessions[peer].Frames > hiddenFrames[peer])
        {
            framesAtResize = sessions[target].Frames;
            var content = layout.Panes.First(placement => placement.Pane.Id == target).Content;
            var size = new OpenTK.Mathematics.Vector2i(1440, 960);
            for (var offset = 1; offset <= 20; offset++)
            {
                var bounds = new UiRect(content.X, content.Y, content.Width + offset, content.Height);
                sessions[target].Render(GuiShark.ProcessHosting.PanelViewport.FromBounds(bounds, size, size), size.Y);
            }
            sessions[target].Render(GuiShark.ProcessHosting.PanelViewport.FromBounds(content, size, size), size.Y);
            step = 11;
        }
        else if (step == 11 && sessions[target].Frames > framesAtResize && sessions[target].HasCurrentFrame)
        {
            Require(!sessions[target].HasFailure, "rapid resize failed the app");
            VerifyLayout();
            Console.WriteLine("VERIFY PASS: four independent apps, routed click, freeze isolation, crash/restart, hidden-tab culling with live heartbeats, rapid resize generations, spaces/tabs/splits/zoom, saved layout");
            step = 6;
            close();
        }
    }
    private void Begin(double elapsed)
    {
        Require(sessions.Values.Select(session => session.Id).Distinct().Count() == 4, "app processes were shared");
        var pane = layout.Panes.First(pane => pane.Pane.Name == "Aurora / One");
        target = pane.Pane.Id;
        peer = layout.Panes.First(pane => pane.Pane.Name == "Pulse / One").Pane.Id;
        var x = pane.Content.X + 200;
        var y = pane.Content.Y + 310;
        input.PointerMove(x, y);
        input.PointerDown(x, y, MouseButton.Left, false);
        input.PointerUp(x, y, MouseButton.Left, false);
        step = 1;
        Console.WriteLine($"VERIFY: routed a real host pointer click at {elapsed:F1}s");
    }
    private void VerifyLayout()
    {
        var state = controller.State;
        var ids = sessions.ToDictionary(pair => pair.Key, pair => pair.Value.Id);
        var studio = state.ActiveSpace;
        var overview = state.ActiveTab;
        var second = studio.Tabs.First(tab => tab.Id != overview.Id);
        controller.Change(() => studio.ActiveTabId = second.Id);
        Require(state.ActiveTab.Id == second.Id, "tab switch failed");
        var personal = state.Spaces.First(space => space.Id != studio.Id);
        controller.Change(() => state.ActiveSpaceId = personal.Id);
        Require(state.ActiveSpace.Id == personal.Id, "space switch failed");
        controller.Change(() => { state.ActiveSpaceId = studio.Id; studio.ActiveTabId = overview.Id; });
        Require(ids.All(pair => sessions[pair.Key].Id == pair.Value), "navigation restarted an app");
        state.Focus(target);
        state.ToggleZoom();
        layout.Arrange(overview, 1440, 960);
        Require(layout.Panes.Count == 1 && layout.Panes[0].Pane.Id == target, "zoom failed");
        state.ToggleZoom();
        layout.Arrange(overview, 1440, 960);
        var divider = layout.Splits[0];
        WorkspaceLayout.Resize(divider, divider.Bounds.X + divider.Bounds.Width * .6f, divider.Bounds.Y);
        Require(overview.Root.Ratio > .59f, "divider resize failed");
        state.AddTab("Verification");
        state.Split(false);
        state.ActivePane.Name = "Renamed pane";
        state.ClosePane();
        Require(state.ActiveTab.Root.Panes().Count() == 1, "close did not collapse split");
        state.CloseTab();
        studio.ActiveTabId = overview.Id;
        store.Save(state);
        var restored = store.Load(() => throw new InvalidOperationException("Saved state could not be restored"));
        Require(restored.Spaces.Count == 2 && restored.ActiveTab.Root.Panes().Count() == 4, "saved tree differs");
        Require(Math.Abs(restored.ActiveTab.Root.Ratio - overview.Root.Ratio) < .001, "split ratio was not restored");
        Require(restored.Panes().Select(pane => pane.Id).SequenceEqual(state.Panes().Select(pane => pane.Id)), "pane identities were not restored");
    }
    private static void Require(bool condition, string reason)
    {
        if (!condition) throw new InvalidOperationException($"VERIFY FAIL: {reason}");
    }
}
