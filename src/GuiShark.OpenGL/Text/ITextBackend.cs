namespace GuiShark.OpenGL;

public enum TextHinting { None, Slight, Normal, Full }
public enum TextSampling { Linear, Nearest }
public sealed record TextRenderOptions(bool PixelSnap = true, TextHinting Hinting = TextHinting.Normal,
    TextSampling Sampling = TextSampling.Linear);

public sealed record TextBackendInfo(string Name, string Description, bool SupportsHinting, bool SupportsSampling);
public readonly record struct TextCacheStatistics(int Entries, long Bytes);
public readonly record struct TextDrawRequest(UiElement Element, float Opacity, float ScaleX, float ScaleY);

/// <summary>Portable text metrics and drawing commands. No window or GL context required by a backend.</summary>
public interface ITextBackend : ITextMetrics, IDisposable
{
    TextBackendInfo Info { get; }
    TextCacheStatistics Cache { get; }
    bool Configure(float rasterScale, TextRenderOptions options);
    void Draw(TextDrawRequest request, ITextCanvas canvas);
}

/// <summary>GPU adapter for premultiplied RGBA text masks.</summary>
public interface ITextCanvas
{
    void Draw(TextImage image, UiRect destination, UiRect source, UiColor color, float opacity,
        TextSampling sampling = TextSampling.Linear);
}

/// <summary>RGBA8 image. Mutate pixels only on the render thread and call Changed after updates.</summary>
public sealed class TextImage(int width, int height)
{
    public int Width { get; } = width;
    public int Height { get; } = height;
    public byte[] Pixels { get; } = new byte[checked(width * height * 4)];
    public int Revision { get; private set; }
    public void Changed() => Revision++;
}
