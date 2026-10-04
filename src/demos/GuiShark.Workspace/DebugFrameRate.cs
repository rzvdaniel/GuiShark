#if DEBUG
using System.Diagnostics;

namespace GuiShark.Workspace;

internal sealed class DebugFrameRate
{
    private readonly Stopwatch interval = new();
    private int frames;

    public string? RecordFrame()
    {
        if (!interval.IsRunning) { interval.Start(); return null; }
        frames++;
        var seconds = interval.Elapsed.TotalSeconds;
        if (seconds < 1) return null;
        var label = $"Host FPS: {frames / seconds:F1}";
        frames = 0;
        interval.Restart();
        return label;
    }
}
#endif
