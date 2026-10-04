using GuiShark.ProcessHosting;

namespace GuiShark.PulseProcess;

internal sealed class PulseWindow(bool embedded = false) : ProcessAppWindow("Pulse · Live signals", embedded)
{
    private double elapsed;
    private bool paused;
    protected override void OnAppLoaded(UiView appView)
    {
        appView.Document.GetElement("toggle").Clicked += button =>
        {
            paused = !paused;
            button.Text = paused ? "Resume signals" : "Pause signals";
        };
    }
    protected override void OnAppUpdate(UiView appView, double seconds)
    {
        if (paused) return;
        elapsed += seconds;
        for (var index = 0; index < 6; index++)
            appView.Document.GetElement($"signal{index}").Control!.Value =
                (float)(50 + 40 * Math.Sin(elapsed * .8 + index * .7));
        appView.Document.GetElement("time").Text = $"Streaming for {elapsed:F1}s · PID {Environment.ProcessId}";
    }
}
