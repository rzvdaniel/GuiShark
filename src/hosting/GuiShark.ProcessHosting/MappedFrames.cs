using System.IO.MemoryMappedFiles;

namespace GuiShark.ProcessHosting;

// File-backed mappings work on Unix too; named mappings do not.
internal sealed class MappedFrames : IDisposable
{
    private readonly FileStream file;
    private readonly MemoryMappedFile mapping;
    private readonly MemoryMappedViewAccessor view;
    public string Path { get; }
    public int Width { get; }
    public int Height { get; }
    public long Generation { get; }
    private readonly int slotBytes;

    public MappedFrames(string path, int width, int height, long generation, bool owner)
    {
        if (width is <= 0 or > 8192 || height is <= 0 or > 8192 || (long)width * height > 16 * 1024 * 1024)
            throw new InvalidDataException("Shared frame dimensions are outside the supported limits.");
        Path = path;
        Width = width;
        Height = height;
        Generation = generation;
        slotBytes = checked(width * height * 4);
        var options = new FileStreamOptions
        {
            Mode = owner ? FileMode.CreateNew : FileMode.Open,
            Access = FileAccess.ReadWrite,
            Share = FileShare.ReadWrite | FileShare.Delete,
            Options = owner ? FileOptions.DeleteOnClose : FileOptions.None
        };
        if (owner && !OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        file = new FileStream(path, options);
        try
        {
            if (owner) file.SetLength((long)slotBytes * 2);
            if (file.Length != (long)slotBytes * 2) throw new InvalidDataException("Shared frame mapping size does not match its header.");
            mapping = MemoryMappedFile.CreateFromFile(file, null, file.Length, MemoryMappedFileAccess.ReadWrite,
                HandleInheritability.None, leaveOpen: true);
            try { view = mapping.CreateViewAccessor(); }
            catch { mapping.Dispose(); throw; }
        }
        catch { file.Dispose(); throw; }
    }

    public unsafe void Access(int slot, Action<IntPtr> operation)
    {
        if (slot is < 0 or > 1) throw new InvalidDataException("Invalid shared frame slot.");
        byte* pointer = null;
        var handle = view.SafeMemoryMappedViewHandle;
        try
        {
            handle.AcquirePointer(ref pointer);
            operation((IntPtr)(pointer + view.PointerOffset + (long)slot * slotBytes));
        }
        finally { if (pointer != null) handle.ReleasePointer(); }
    }

    public void Dispose() { view.Dispose(); mapping.Dispose(); file.Dispose(); }
}
