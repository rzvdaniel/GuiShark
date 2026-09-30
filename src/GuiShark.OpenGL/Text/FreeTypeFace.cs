using System.Runtime.InteropServices;
using System.Text;
using FreeTypeSharp;
using static FreeTypeSharp.FT;

namespace GuiShark.OpenGL;

/// <summary>Owns a pinned font buffer and one FreeType face/library pair.</summary>
internal sealed unsafe class FreeTypeFace : IDisposable
{
    private FT_LibraryRec_* library;
    private FT_FaceRec_* face;
    private GCHandle buffer;

    public FreeTypeFace(byte[] bytes)
    {
        try
        {
            fixed (FT_LibraryRec_** pointer = &library) Check(FT_Init_FreeType(pointer));
            buffer = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            fixed (FT_FaceRec_** pointer = &face)
                Check(FT_New_Memory_Face(library, (byte*)buffer.AddrOfPinnedObject(), bytes.Length, 0, pointer));
        }
        catch { Dispose(); throw; }
    }

    public RasterGlyph Rasterize(Rune rune, int size, TextHinting hinting)
    {
        Check(FT_Set_Pixel_Sizes(face, 0, (uint)size));
        var index = FT_Get_Char_Index(face, (nuint)rune.Value);
        if (index == 0) index = FT_Get_Char_Index(face, '?');
        var flags = FT_LOAD.FT_LOAD_NO_BITMAP;
        if (hinting == TextHinting.None) flags |= FT_LOAD.FT_LOAD_NO_HINTING;
        if (hinting == TextHinting.Slight) flags |= (FT_LOAD)(1 << 16); // FT_LOAD_TARGET_LIGHT
        if (hinting == TextHinting.Full) flags |= FT_LOAD.FT_LOAD_FORCE_AUTOHINT;
        Check(FT_Load_Glyph(face, index, flags));
        Check(FT_Render_Glyph(face->glyph, FT_Render_Mode_.FT_RENDER_MODE_NORMAL));
        var slot = face->glyph;
        var bitmap = slot->bitmap;
        var width = checked((int)bitmap.width);
        var height = checked((int)bitmap.rows);
        var pixels = new byte[checked(width * height)];
        if (width > 0 && height > 0)
        {
            if (bitmap.pixel_mode != FT_Pixel_Mode_.FT_PIXEL_MODE_GRAY)
                throw new NotSupportedException("FreeType backend expects grayscale outline glyphs.");
            for (var row = 0; row < height; row++)
                Marshal.Copy((nint)(bitmap.buffer + row * bitmap.pitch), pixels, row * width, width);
        }
        return new(width, height, slot->bitmap_left, slot->bitmap_top,
            (float)slot->advance.x / 64, (float)face->size->metrics.ascender / 64,
            (float)face->size->metrics.descender / 64, pixels);
    }

    private static void Check(FT_Error error)
    {
        if (error != 0) throw new InvalidOperationException($"FreeType error: {error}");
    }

    public void Dispose()
    {
        if (face != null) { FT_Done_Face(face); face = null; }
        if (library != null) { FT_Done_FreeType(library); library = null; }
        if (buffer.IsAllocated) buffer.Free();
    }
}

internal sealed record RasterGlyph(int Width, int Height, int Left, int Top, float Advance,
    float Ascender, float Descender, byte[] Coverage);
