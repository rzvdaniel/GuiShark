using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using GuiShark.AppProtocol;

namespace GuiShark.ProcessHosting;

public sealed class AppProcess : IDisposable
{
    private readonly NamedPipeServerStream pipe;
    private readonly Process process;
    private readonly CancellationTokenSource stop = new();
    private readonly Task pipeTask;
    private readonly Task errorsTask;
    private bool disposed;
    private readonly Channel<AppMessage> outgoing = Channel.CreateBounded<AppMessage>(new BoundedChannelOptions(256)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = false
    });
    private readonly Channel<AppMessage> incoming = Channel.CreateUnbounded<AppMessage>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
    private AppMessage? latestFrame;
    public bool InputOverflow { get; private set; }

    public int Id => process.Id;
    public bool HasExited => process.HasExited;
    public int ExitCode => process.ExitCode;
    public bool IsConnected => pipe.IsConnected;
    public bool TryReceive(out AppMessage? message)
    {
        if (incoming.Reader.TryRead(out message)) return true;
        message = Interlocked.Exchange(ref latestFrame, null);
        return message is not null;
    }
    public void Send(AppMessage message)
    {
        if (disposed || stop.IsCancellationRequested || outgoing.Writer.TryWrite(message)) return;
        // Never discard a release or edit silently. Stop this session instead of leaving input stuck.
        InputOverflow = true;
        stop.Cancel();
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
        catch (InvalidOperationException) { /* Already exited. */ }
    }

    public AppProcess(AppPackage package)
    {
        var name = $"guishark-{Guid.NewGuid():N}";
        pipe = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        var dotnetRoot = Directory.GetParent(RuntimeEnvironment.GetRuntimeDirectory())!.Parent!.Parent!.Parent!.FullName;
        var dotnet = Path.Combine(dotnetRoot, OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
        var start = new ProcessStartInfo(dotnet)
        {
            WorkingDirectory = Path.GetDirectoryName(package.EntryAssembly)!,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add(package.EntryAssembly);
        start.ArgumentList.Add("--pipe");
        start.ArgumentList.Add(name);
        try { process = Process.Start(start) ?? throw new InvalidOperationException("Could not launch the app process."); }
        catch
        {
            pipe.Dispose();
            stop.Dispose();
            throw;
        }
        errorsTask = DrainErrors(process.StandardError);
        pipeTask = RunPipe();
    }

    private async Task RunPipe()
    {
        try
        {
            await pipe.WaitForConnectionAsync(stop.Token);
            using var reader = new StreamReader(pipe, Encoding.UTF8, false, 1024, true);
            using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 1024, true) { AutoFlush = true };
            var receive = Receive(reader);
            var send = Send(writer);
            await Task.WhenAny(receive, send);
            await stop.CancelAsync();
            await Task.WhenAll(receive, send);
        }
        catch (Exception error) when (error is IOException or OperationCanceledException or ObjectDisposedException or System.Text.Json.JsonException or InvalidDataException)
        {
            System.Diagnostics.Debug.WriteLine($"App pipe ended: {error.Message}");
        }
        finally
        {
            incoming.Writer.TryWrite(new AppMessage("disconnected"));
        }
    }

    private async Task Receive(StreamReader reader)
    {
        while (await reader.ReadLineAsync(stop.Token) is { } line)
        {
            var message = AppMessage.Parse(line);
            if (message.Type == "frame") Interlocked.Exchange(ref latestFrame, message);
            else incoming.Writer.TryWrite(message);
        }
    }

    private async Task Send(StreamWriter writer)
    {
        await foreach (var message in outgoing.Reader.ReadAllAsync(stop.Token))
            await writer.WriteLineAsync(message.ToJson().AsMemory(), stop.Token);
    }

    private async Task DrainErrors(StreamReader reader)
    {
        try
        {
            while (await reader.ReadLineAsync(stop.Token) is { } line)
                await Console.Error.WriteLineAsync($"App: {line}");
        }
        catch (Exception error) when (error is ObjectDisposedException or IOException or OperationCanceledException)
        { System.Diagnostics.Debug.WriteLine(error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        stop.Cancel();
        outgoing.Writer.TryComplete();
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
        catch (InvalidOperationException) { /* The child exited between the check and kill. */ }
        pipe.Dispose();
        process.Dispose();
        _ = FinishDisposalAsync();
    }
    private async Task FinishDisposalAsync()
    {
        try { await Task.WhenAll(pipeTask, errorsTask); }
        finally { stop.Dispose(); }
    }

}
