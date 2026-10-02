using System.IO.Pipes;
using System.Text;
using GuiShark.AppProtocol;

namespace GuiShark.AuroraProcess;

internal sealed class AuroraPipeClient
{
    private readonly AuroraCounter counter = new();

    public async Task RunAsync(string pipeName)
    {
        using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await pipe.ConnectAsync(timeout.Token);
        using var reader = new StreamReader(pipe, Encoding.UTF8, false, 1024, true);
        using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 1024, true) { AutoFlush = true };
        await Send(writer, new AppMessage("ready", Value: "Aurora is connected"));
        while (await reader.ReadLineAsync() is { } line)
        {
            var message = AppMessage.Parse(line);
            if (message.Type == "stop") break;
            await Handle(message, writer);
        }
    }

    private async Task Handle(AppMessage message, StreamWriter writer)
    {
        switch (message.Type)
        {
            case "click" when message.Id == "increment":
                counter.Increment();
                await Send(writer, new AppMessage("set-text", "count", counter.Count.ToString()));
                await Send(writer, new AppMessage("set-text", "message", counter.Message));
                break;
            case "freeze":
                Thread.Sleep(5000);
                await Send(writer, new AppMessage("status", Value: "Aurora resumed after five seconds"));
                break;
            case "crash":
                Environment.FailFast("Deliberate Aurora process crash");
                break;
        }
    }

    private static Task Send(StreamWriter writer, AppMessage message) => writer.WriteLineAsync(message.ToJson());
}
