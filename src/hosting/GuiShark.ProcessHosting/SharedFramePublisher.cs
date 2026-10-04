using GuiShark.AppProtocol;
using OpenTK.Graphics.OpenGL4;

namespace GuiShark.ProcessHosting;

internal sealed class SharedFramePublisher : IDisposable
{
    private MappedFrames? frames;
    private readonly bool[] occupied = new bool[2];
    public bool Enabled => frames is not null;
    public long Generation => frames?.Generation ?? 0;
    public void Configure(AppMessage message)
    {
        if (message.Generation <= (frames?.Generation ?? 0)) return;
        MappedFrames replacement;
        try { replacement = new MappedFrames(message.Value!, message.PixelWidth, message.PixelHeight, message.Generation, false); }
        catch (FileNotFoundException) { return; } // Superseded resize before the child opened its mapping.
        frames?.Dispose();
        frames = replacement;
        Array.Clear(occupied);
    }
    public void Release(AppMessage message)
    {
        if (message.Generation == frames?.Generation && message.BufferSlot is >= 0 and < 2)
            occupied[message.BufferSlot] = false;
    }
    public bool CanPublish(int width, int height) => frames is not null && frames.Width == width && frames.Height == height
        && occupied.Any(value => !value);
    public AppMessage Publish(int width, int height, long sequence, string? value)
    {
        if (!CanPublish(width, height)) throw new InvalidOperationException("No shared frame slot is available.");
        var slot = Array.IndexOf(occupied, false);
        frames!.Access(slot, pointer => GL.ReadPixels(0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, pointer));
        occupied[slot] = true;
        return new AppMessage("frame", Value: value, PixelWidth: width, PixelHeight: height, Sequence: sequence,
            BufferSlot: slot, Generation: frames.Generation);
    }
    public void Dispose() => frames?.Dispose();
}
