using GuiShark.OpenGL;

namespace GuiShark.Workspace;

internal sealed class WorkspaceChrome(FontBook fonts, DirectoryAssetSource assets) : IDisposable
{
    private OpenGlUiRenderer? renderer;
    private OpenGlUiRenderer? overlayRenderer;
    private UiView? mainView;
    private UiView? overlayView;
    public UiView View => overlayView ?? mainView!;
#if DEBUG
    private string debugFrameRate = "Host FPS: …";
    public void SetDebugFrameRate(string label)
    {
        debugFrameRate = label;
        mainView!.Document.GetElement("debug-fps").Text = label;
    }
#endif
    public void Rebuild(ChromeMarkup markup, float width, float height, WorkspaceController controller)
    {
        ReleaseViews();
        mainView = Create(markup.Build(width, height), width, height);
        renderer = new OpenGlUiRenderer(mainView, fonts);
        var baseActions = markup.Actions.ToArray();
        Bind(mainView, baseActions, controller);
        if (controller.HasOverlay)
        {
            overlayView = Create(markup.BuildOverlay(), width, height);
            overlayRenderer = new OpenGlUiRenderer(overlayView, fonts);
            Bind(overlayView, markup.Actions.Skip(baseActions.Length), controller);
        }
        if (controller.Prompt is not null)
        {
            var input = View.Document.GetElement("prompt-value");
            input.TextInput!.Submitted += _ => controller.Accept(input.TextInput.Value);
            View.Input.Focus(input);
        }
#if DEBUG
        SetDebugFrameRate(debugFrameRate);
#endif
        View.Update();
    }
    private UiView Create(string html, float width, float height)
    {
        var result = new UiView(HtmlLoader.Load(html, assets), fonts);
        result.Resize(width, height);
        return result;
    }
    private static void Bind(UiView view, IEnumerable<KeyValuePair<string, Action>> actions, WorkspaceController controller)
    {
        foreach (var (id, action) in actions)
            view.Document.GetElement(id).Clicked += _ => Invoke(action, controller);
    }
    private static void Invoke(Action action, WorkspaceController controller)
    {
        try { action(); }
        catch (Exception error) when (error is IOException or ArgumentException or System.Text.Json.JsonException or UnauthorizedAccessException)
        { controller.Notice = error.Message; }
    }
    public void UpdateStatus(WorkspaceLayout layout, IReadOnlyDictionary<string, PaneSession> sessions, WorkspaceController controller)
    {
        mainView!.Document.GetElement("notice").Text = controller.Notice;
        foreach (var id in layout.Panes.Select(placement => placement.Pane.Id))
        {
            var text = "No app · Aurora, Pulse, or Open app…";
            if (sessions.TryGetValue(id, out var session))
                text = session.Unresponsive && !session.Exited ? $"Unresponsive · PID {session.Id} · Other apps continue" : session.Status;
            mainView.Document.GetElement("status-" + id).Text = text;
        }
    }
    public void Render(int width, int height) => renderer?.Render(width, height);
    public void RenderOverlay(int width, int height) => overlayRenderer?.Render(width, height);
    private void ReleaseViews()
    {
        overlayRenderer?.Dispose();
        renderer?.Dispose();
        overlayView?.Dispose();
        mainView?.Dispose();
        overlayView = null;
        overlayRenderer = null;
    }
    public void Dispose() => ReleaseViews();
}
