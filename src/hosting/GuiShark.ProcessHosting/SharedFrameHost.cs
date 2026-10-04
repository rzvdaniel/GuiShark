using GuiShark.AppProtocol;

namespace GuiShark.ProcessHosting;

internal sealed class SharedFrameHost : IDisposable
{
    private MappedFrames? frames;
    private long generation;
    private readonly List<MappedFrames> retired = [];
    public AppMessage Configure(int width, int height)
    {
        if (frames is null || frames.Width != width || frames.Height != height)
        {
            var replacement = new MappedFrames(System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                $"guishark-frames-{Guid.NewGuid():N}.bin"), width, height, ++generation, true);
            if (frames is not null) retired.Add(frames);
            frames = replacement;
        }
        return new AppMessage("shared-buffer", Value: frames.Path, PixelWidth: width, PixelHeight: height, Generation: frames.Generation);
    }
    public bool Read(AppMessage message, Action<IntPtr> upload)
    {
        if (frames is null || message.Generation != frames.Generation) return false;
        if (message.PixelWidth != frames.Width || message.PixelHeight != frames.Height)
            throw new InvalidDataException("Shared frame dimensions did not match the mapping.");
        frames.Access(message.BufferSlot, upload);
        return true;
    }
    public void Confirm(long acknowledgedGeneration)
    {
        if (frames is null || acknowledgedGeneration > frames.Generation) return;
        foreach (var previous in retired.Where(previous => previous.Generation < acknowledgedGeneration).ToArray())
        {
            previous.Dispose();
            retired.Remove(previous);
        }
    }
    public void Dispose()
    {
        frames?.Dispose();
        foreach (var previous in retired) previous.Dispose();
        retired.Clear();
    }
}
