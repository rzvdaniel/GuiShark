using System.Diagnostics;
using GuiShark.AppProtocol;
using GuiShark.ProcessHosting;

namespace GuiShark.Workspace;

internal sealed class PaneSession(WorkspacePane pane) : IDisposable
{
    private AppProcess? process;
    private AppPanel? panel;
    private long lastFrame = Stopwatch.GetTimestamp();
    private string status = "Starting…";
    private PanelViewport lastViewport;
    private bool failed;
    public bool HasFailure => failed || Exited;
    public bool AcceptsInput => process is { HasExited: false } && !failed;
    public string ManifestPath { get; } = pane.ManifestPath;
    public string Status => process is { HasExited: true } ? $"Exited ({process.ExitCode}) · Restart to recover" : status;
    public int Id => process?.Id ?? 0;
    public long Frames { get; private set; }
    public int SampleCount { get; private set; }
    public bool Ready { get; private set; }
    public bool Exited => process?.HasExited == true;
    public bool Unresponsive => Ready && Stopwatch.GetElapsedTime(lastFrame).TotalSeconds > 2;

    public void Start()
    {
        process?.Dispose();
        panel?.Dispose();
        process = null;
        panel = null;
        Ready = false;
        failed = false;
        Frames = 0;
        SampleCount = 0;
        lastViewport = default;
        lastFrame = Stopwatch.GetTimestamp();
        try
        {
            var package = AppPackage.Load(ManifestPath);
            process = new AppProcess(package);
            panel = new AppPanel();
            status = $"Starting {package.Title}…";
        }
        catch (Exception error) when (error is IOException or System.Text.Json.JsonException or InvalidDataException
            or ArgumentException or System.ComponentModel.Win32Exception or UnauthorizedAccessException)
        {
            failed = true;
            status = $"Cannot start: {error.Message}";
        }
    }
    public void Update()
    {
        if (process is null) return;
        while (process.TryReceive(out var message)) Apply(message!);
        if (process.InputOverflow) status = "Input queue overflow · Restart to recover";
    }
    private void Apply(AppMessage message)
    {
        try
        {
            switch (message.Type)
            {
                case "ready": Ready = true; status = $"Running · PID {Id}"; break;
                case "frame":
                    panel?.Apply(message);
                    lastFrame = Stopwatch.GetTimestamp();
                    Frames++;
                    if (int.TryParse(message.Value, out var count)) SampleCount = count;
                    status = $"Running · PID {Id}";
                    break;
                case "state" when int.TryParse(message.Value, out var stateCount): SampleCount = stateCount; break;
                case "disconnected": failed = true; status = "Disconnected · Restart to recover"; break;
            }
        }
        catch (Exception error) when (error is InvalidDataException or FormatException or ArgumentException)
        {
            failed = true;
            status = $"Invalid frame: {error.Message}";
        }
    }
    public void Render(PanelViewport viewport, int screenHeight)
    {
        if (!viewport.IsValid) return;
        if (lastViewport.PixelWidth != viewport.PixelWidth || lastViewport.PixelHeight != viewport.PixelHeight
            || Math.Abs(lastViewport.Bounds.Width - viewport.Bounds.Width) > .01f || Math.Abs(lastViewport.Bounds.Height - viewport.Bounds.Height) > .01f)
        {
            lastViewport = viewport;
            Send(new AppMessage("resize", Width: (int)viewport.Bounds.Width, Height: (int)viewport.Bounds.Height,
                PixelWidth: viewport.PixelWidth, PixelHeight: viewport.PixelHeight));
        }
        panel?.Render(viewport, screenHeight);
    }
    public void Send(AppMessage message)
    {
        if (process is { HasExited: false }) process.Send(message);
    }
    public void Dispose() { process?.Dispose(); panel?.Dispose(); }
}
