namespace GuiShark;

/// <summary>Portable paired-click detection, using a monotonic clock and logical coordinates.</summary>
internal sealed class TextClickTracker
{
    private UiElement? last;
    private long time;
    private float x, y;
    public bool Down(UiElement? element, float nextX, float nextY)
    {
        var now = Environment.TickCount64;
        var twice = element?.TextInput != null && last == element && now - time <= 500
            && Math.Abs(nextX - x) <= 4 && Math.Abs(nextY - y) <= 4;
        last = twice ? null : element;
        time = now; x = nextX; y = nextY;
        return twice;
    }
    public void Move(float nextX, float nextY)
    {
        if (Math.Abs(nextX - x) > 4 || Math.Abs(nextY - y) > 4) Reset();
    }
    public void Reset() => last = null;
}
