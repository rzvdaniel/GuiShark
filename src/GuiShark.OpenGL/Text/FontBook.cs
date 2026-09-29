using SkiaSharp;

namespace GuiShark.OpenGL;

/// <summary>Host-supplied fonts, shared by layout measurement and text rasterization.</summary>
public sealed class FontBook : ITextMetrics, IDisposable
{
    private readonly SKTypeface regular;
    private readonly SKTypeface bold;

    public FontBook(string regularFontPath, string boldFontPath)
    {
        regular = SKTypeface.FromFile(regularFontPath) ?? throw new IOException($"Cannot load font: {regularFontPath}");
        try { bold = SKTypeface.FromFile(boldFontPath) ?? throw new IOException($"Cannot load font: {boldFontPath}"); }
        catch { regular.Dispose(); throw; }
    }

    internal SKFont CreateFont(float size, bool isBold) => new(isBold ? bold : regular, size)
    {
        Edging = SKFontEdging.Antialias,
        Subpixel = true
    };

    public float MeasureWidth(string text, float fontSize, bool bold)
    {
        using var font = CreateFont(fontSize, bold);
        return font.MeasureText(text);
    }

    public void Dispose() { regular.Dispose(); bold.Dispose(); }
}
