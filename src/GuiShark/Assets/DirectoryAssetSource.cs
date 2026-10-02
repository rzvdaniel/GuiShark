namespace GuiShark;

public sealed class DirectoryAssetSource : IAssetSource
{
    private readonly string root;
    public DirectoryAssetSource(string directory)
    {
        var fullPath = Path.GetFullPath(directory);
        root = Path.EndsInDirectorySeparator(fullPath) ? fullPath : fullPath + Path.DirectorySeparatorChar;
    }
    public string ReadText(string relativePath) => File.ReadAllText(Resolve(relativePath));
    public Stream Open(string relativePath) => File.OpenRead(Resolve(relativePath));

    private string Resolve(string path)
    {
        var fullPath = Path.GetFullPath(Path.Combine(root, path));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!fullPath.StartsWith(root, comparison)) throw new IOException("Assets must be inside the asset directory.");
        return fullPath;
    }
}
