namespace GuiShark.OpenGL;

public readonly record struct TextRenderStatistics(int DrawCalls, int Uploads, long GpuBytes, TextCacheStatistics Cache);

internal sealed class OpenGlTextCanvas(QuadPainter painter) : ITextCanvas, IDisposable
{
    private sealed record Entry(GpuTexture Texture, int Revision, int LastFrame);
    private readonly Dictionary<TextImage, Entry> textures = new();
    private int frame;
    public int DrawCalls { get; private set; }
    public int Uploads { get; private set; }
    public long Bytes => textures.Values.Sum(e => (long)e.Texture.Width * e.Texture.Height * 4);

    public void Begin() { frame++; DrawCalls = Uploads = 0; }
    public void Draw(TextImage image, UiRect destination, UiRect source, UiColor color, float opacity,
        TextSampling sampling = TextSampling.Linear, float distanceRange = 0)
    {
        if (!textures.TryGetValue(image, out var entry))
        {
            entry = new(new(image), image.Revision, frame);
            Uploads++;
        }
        else if (entry.Revision != image.Revision)
        {
            entry.Texture.Update(image);
            Uploads++;
        }
        textures[image] = entry with { Revision = image.Revision, LastFrame = frame };
        painter.Texture(destination, entry.Texture, color, opacity, source: source, sampling: sampling, distanceRange: distanceRange);
        DrawCalls++;
    }

    public void End()
    {
        foreach (var pair in textures.Where(p => frame - p.Value.LastFrame > 2).ToArray())
        {
            pair.Value.Texture.Dispose();
            textures.Remove(pair.Key);
        }
    }

    public void Dispose()
    {
        foreach (var entry in textures.Values) entry.Texture.Dispose();
        textures.Clear();
    }
}
