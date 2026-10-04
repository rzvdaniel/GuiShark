using System.Diagnostics;
using System.Text.Json;

namespace GuiShark.Workspace;

// Opt-in measurement with the same four visible panes and an isolated layout.
internal sealed class WorkspaceBenchmark(bool sharedFrames, int fps, Action close)
{
    private readonly List<double> renderMilliseconds = [];
    private long started;
    private long allocated;
    private double cpuMilliseconds;
    private Dictionary<string, long> initialFrames = [];
    private double startup;

    public void Record(double workMilliseconds, double elapsed, IReadOnlyDictionary<string, PaneSession> sessions)
    {
        if (started == 0)
        {
            if (elapsed > 30) throw new InvalidOperationException("Benchmark apps did not become ready.");
            if (sessions.Count != 4 || sessions.Values.Any(session => !session.Ready || session.Frames == 0)) return;
            if (startup == 0) startup = elapsed;
            if (elapsed - startup < 5) return;
            started = Stopwatch.GetTimestamp();
            allocated = GC.GetTotalAllocatedBytes();
            cpuMilliseconds = Cpu(sessions);
            initialFrames = sessions.ToDictionary(pair => pair.Key, pair => pair.Value.Frames);
            return;
        }
        renderMilliseconds.Add(workMilliseconds);
        var seconds = Stopwatch.GetElapsedTime(started).TotalSeconds;
        if (seconds < 10) return;
        var sorted = renderMilliseconds.Order().ToArray();
        var report = new
        {
            transport = sharedFrames ? "shared-memory" : "png",
            requestedFps = fps,
            seconds,
            hostFps = sorted.Length / seconds,
            hostWorkMeanMs = sorted.Average(),
            hostWorkP95Ms = sorted[(int)((sorted.Length - 1) * .95)],
            hostWorkMaxMs = sorted[^1],
            hostAllocatedBytesPerSecond = (GC.GetTotalAllocatedBytes() - allocated) / seconds,
            aggregateCpuCores = (Cpu(sessions) - cpuMilliseconds) / (seconds * 1000),
            panes = sessions.Select(pair => new { pair.Key, pair.Value.Id, framesPerSecond = (pair.Value.Frames - initialFrames[pair.Key]) / seconds })
        };
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        Directory.CreateDirectory("artifacts");
        File.WriteAllText($"artifacts/benchmark-{report.transport}-{fps}.json", json);
        Console.WriteLine(json);
        close();
    }

    private static double Cpu(IReadOnlyDictionary<string, PaneSession> sessions)
    {
        using var host = Process.GetCurrentProcess();
        return host.TotalProcessorTime.TotalMilliseconds + sessions.Values.Sum(session => session.CpuMilliseconds);
    }
}
