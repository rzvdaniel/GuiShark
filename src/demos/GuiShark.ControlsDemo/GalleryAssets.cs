namespace GuiShark.ControlsDemo;

/// <summary>Read editable HTML/CSS from the chosen folder and linked fonts from the build output.</summary>
internal sealed class GalleryAssets(string directory) : IAssetSource
{
    private readonly DirectoryAssetSource source = new(directory);
    private readonly DirectoryAssetSource bundled = new(Path.Combine(AppContext.BaseDirectory, "Assets"));
    public string ReadText(string relativePath) => source.ReadText(relativePath);
    public Stream Open(string relativePath)
    {
        try { return source.Open(relativePath); }
        catch (FileNotFoundException) { return bundled.Open(relativePath); }
        catch (DirectoryNotFoundException) { return bundled.Open(relativePath); }
    }
}
