namespace GuiShark.AuroraProcess;

internal sealed class AuroraCounter
{
    public int Count { get; private set; }
    public string Message => Count == 0
        ? "Ready for your next command."
        : $"Event {Count} handled in process {Environment.ProcessId}.";

    public void Increment() => Count++;
}
