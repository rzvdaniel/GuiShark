using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using GuiShark.AppProtocol;

namespace GuiShark.ProcessHost;

internal sealed class AppProcess : IDisposable
{
    private readonly NamedPipeServerStream pipe;
    private readonly Process process;
    private readonly CancellationTokenSource stop = new();
    private readonly Channel<AppMessage> outgoing = Channel.CreateBounded<AppMessage>(new BoundedChannelOptions(256)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = false
    });
    private readonly Channel<AppMessage> incoming = Channel.CreateBounded<AppMessage>(new BoundedChannelOptions(2)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = true
    });

    public int Id => process.Id;
    public bool HasExited => process.HasExited;
    public int ExitCode => process.ExitCode;
    public bool IsConnected => pipe.IsConnected;
    public bool TryReceive(out AppMessage? message) => incoming.Reader.TryRead(out message);
    public void Send(AppMessage message) => outgoing.Writer.TryWrite(message);

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
        process = Process.Start(start) ?? throw new InvalidOperationException("Could not launch the app process.");
        _ = DrainErrors();
        process.Exited += (_, _) => Console.Error.WriteLine($"Aurora process exited ({process.ExitCode}).");
        process.EnableRaisingEvents = true;
        _ = RunPipe();
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
            var ended = await Task.WhenAny(receive, send);
            _ = ended.Exception;
        }
        catch (Exception error) when (error is IOException or OperationCanceledException or ObjectDisposedException)
        {
            _ = Console.Error.WriteLineAsync($"Aurora pipe ended: {error}");
        }
        finally
        {
            incoming.Writer.TryWrite(new AppMessage("disconnected"));
        }
    }

    private async Task Receive(StreamReader reader)
    {
        while (await reader.ReadLineAsync(stop.Token) is { } line)
            incoming.Writer.TryWrite(AppMessage.Parse(line));
    }

    private async Task Send(StreamWriter writer)
    {
        await foreach (var message in outgoing.Reader.ReadAllAsync(stop.Token))
            await writer.WriteLineAsync(message.ToJson());
    }

    private async Task DrainErrors()
    {
        try
        {
            while (await process.StandardError.ReadLineAsync() is { } line)
                await Console.Error.WriteLineAsync($"Aurora: {line}");
        }
        catch (ObjectDisposedException) { /* The host is closing the child stream. */ }
    }

    public void Dispose()
    {
        stop.Cancel();
        outgoing.Writer.TryComplete();
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
        catch (InvalidOperationException) { /* The child exited between the check and kill. */ }
        pipe.Dispose();
        process.Dispose();
        stop.Dispose();
    }
}
