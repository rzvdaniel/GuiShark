using GuiShark.OpenGL;

namespace GuiShark.TextDemo;

/// <summary>Two shaping settings of the same supported rasterizer.</summary>
internal sealed class BackendCatalog(FontBook fonts)
{
    public static string Name(int index) => index == 0 ? "Skia / unshaped" : "Skia + HarfBuzz / default";
    public static string Detail(int index) => index == 0
        ? "Glyph mapping only • pixel alignment • hinting"
        : "Shaped font runs • local fallback • hinting";
    public ITextBackend Create(int index) => new SkiaTextBackend(fonts, shaping: index == 1);
}
