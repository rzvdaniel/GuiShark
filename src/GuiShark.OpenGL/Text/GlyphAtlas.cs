namespace GuiShark.OpenGL;

/// <summary>Shelf packing with transparent gutters, owned by one rasterizer.</summary>
internal sealed class GlyphAtlas
{
    private const int PageSize = 1024;
    private readonly List<TextImage> pages = [];
    private int x = 1, y = 1, rowHeight;
    public long Bytes => pages.Sum(p => (long)p.Pixels.Length);

    public (TextImage Image, UiRect Source) Add(byte[] coverage, int width, int height)
    {
        if (width + 2 > PageSize || height + 2 > PageSize)
            throw new InvalidOperationException("Glyph exceeds atlas size; reduce the font size.");
        if (x + width + 1 > PageSize) { x = 1; y += rowHeight + 2; rowHeight = 0; }
        if (pages.Count == 0 || y + height + 1 > PageSize)
        {
            // Explicit budget rather than an unbounded atlas allocation or invalidating glyphs mid-draw.
            if (pages.Count >= 16) throw new InvalidOperationException("Glyph atlas budget exceeded. Recreate the backend or reduce font/size diversity.");
            pages.Add(new(PageSize, PageSize));
            x = y = 1;
            rowHeight = 0;
        }
        var page = pages[^1];
        for (var row = 0; row < height; row++)
            for (var col = 0; col < width; col++)
            {
                var index = ((y + row) * PageSize + x + col) * 4;
                var alpha = coverage[row * width + col];
                page.Pixels[index] = page.Pixels[index + 1] = page.Pixels[index + 2] = page.Pixels[index + 3] = alpha;
            }
        page.Changed();
        var uv = new UiRect((float)x / PageSize, (float)y / PageSize, (float)width / PageSize, (float)height / PageSize);
        x += width + 2;
        rowHeight = Math.Max(rowHeight, height);
        return (page, uv);
    }

    public void Clear() { pages.Clear(); x = y = 1; rowHeight = 0; }
}
