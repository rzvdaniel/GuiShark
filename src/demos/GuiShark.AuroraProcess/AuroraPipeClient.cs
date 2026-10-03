using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Threading.Channels;
using GuiShark.AppProtocol;

namespace GuiShark.AuroraProcess;

internal sealed class AuroraPipeClient : IDisposable
{
    private NamedPipeClientStream pipe = null!;
    private readonly CancellationTokenSource stop = new();
    private readonly Channel<AppMessage> frames = Channel.CreateBounded<AppMessage>(new BoundedChannelOptions(1)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = true
    });
    private readonly Channel<AppMessage> controls = Channel.CreateBounded<AppMessage>(new BoundedChannelOptions(16)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = true
    });
    private readonly Channel<byte> signal = Channel.CreateBounded<byte>(new BoundedChannelOptions(1)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true,
        SingleWriter = false
    });

    public AuroraPipeClient() { }

    public void Run(AuroraWindow window, string pipeName)
    {
        pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        pipe.Connect(10_000);
        using var reader = new StreamReader(pipe, Encoding.UTF8, false, 1024, true);
        using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 1024, true) { AutoFlush = true };
        void Publish(AppMessage message)
        {
            if (message.Type == "frame") frames.Writer.TryWrite(message);
            else controls.Writer.TryWrite(message);
            signal.Writer.TryWrite(0);
        }

        window.SetSender(Publish);
        window.Enqueue(new AppMessage("resize", Width: 730, Height: 520, PixelWidth: 730, PixelHeight: 520));
        var receive = ReceiveAsync(reader, window, stop.Token);
        var send = SendAsync(writer, controls.Reader, frames.Reader, signal.Reader, stop.Token);
        try { window.Run(); }
        finally
        {
            frames.Writer.TryComplete();
            controls.Writer.TryComplete();
            signal.Writer.TryComplete();
            stop.Cancel();
            Complete(receive, stop.Token);
            Complete(send, stop.Token);
        }
    }

    public void Dispose()
    {
        pipe.Dispose();
        stop.Dispose();
    }

    private static void Complete(Task task, CancellationToken token)
    {
        try { task.GetAwaiter().GetResult(); }
        catch (OperationCanceledException error) when (token.IsCancellationRequested) { Debug.WriteLine(error); }
        catch (IOException error) { Debug.WriteLine(error); }
    }

    private static async Task ReceiveAsync(StreamReader reader, AuroraWindow window, CancellationToken token)
    {
        while (await reader.ReadLineAsync(token) is { } line)
            window.Enqueue(AppMessage.Parse(line));
        window.Enqueue(new AppMessage("stop"));
    }

    private static async Task SendAsync(StreamWriter writer, ChannelReader<AppMessage> controls,
        ChannelReader<AppMessage> frames, ChannelReader<byte> signal, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (controls.TryRead(out var control))
            {
                await writer.WriteLineAsync(control.ToJson().AsMemory(), token);
                continue;
            }
            if (frames.TryRead(out var frame))
            {
                await writer.WriteLineAsync(frame.ToJson().AsMemory(), token);
                continue;
            }
            if (!await signal.WaitToReadAsync(token)) return;
            signal.TryRead(out _);
        }
    }
}
