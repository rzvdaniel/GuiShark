using GuiShark.OpenGL;

namespace GuiShark.ControlsDemo;

internal static class GalleryTextBackend
{
    public static ITextBackend Create(string name, FontBook fonts) => name switch
    {
        "freetype" => new FreeTypeTextBackend(fonts),
        "msdf" => new MsdfTextBackend(
            new(Atlases(), "Lato-Regular.png", "Lato-Regular.json"),
            new(Atlases(), "Lato-Bold.png", "Lato-Bold.json"), "Lato"),
        _ => new SkiaTextBackend(fonts)
    };
    private static IAssetSource Atlases() => new DirectoryAssetSource(Path.Combine(AppContext.BaseDirectory, "Assets/atlas"));
}
