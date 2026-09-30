using GuiShark.OpenGL;

namespace GuiShark.TextDemo;

internal sealed class BackendCatalog(FontBook fonts, IAssetSource assets)
{
    private MsdfAtlas? regular, bold;
    public static string Name(int index) => index switch
    {
        0 => "Skia baseline", 1 => "Skia pixel aligned", 2 => "FreeType glyph atlas", _ => "MSDF scalable atlas"
    };
    public static string Detail(int index) => index switch
    {
        0 => "Reference • original resampling • fixed options",
        1 => "Device-size grayscale • pixel alignment • hinting",
        2 => "Grayscale glyph cache • integer device sizes • hinting",
        _ => "Scalable distance fields • linear only • no hinting"
    };
    public ITextBackend Create(int index) => index switch
    {
        0 => new SkiaTextBackend(fonts, true), 1 => new SkiaTextBackend(fonts),
        2 => new FreeTypeTextBackend(fonts), _ => CreateMsdf()
    };
    private ITextBackend CreateMsdf()
    {
        regular ??= new(assets, "atlas/Lato-Regular.png", "atlas/Lato-Regular.json");
        bold ??= new(assets, "atlas/Lato-Bold.png", "atlas/Lato-Bold.json");
        return new MsdfTextBackend(regular, bold, "Lato");
    }
}
