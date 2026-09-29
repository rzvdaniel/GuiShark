using SkiaSharp;

namespace GuiShark.OpenGL;

internal sealed class ImageTextureCache(IAssetSource assets) : IDisposable
{
    private readonly Dictionary<string, GpuTexture> textures = new();
    public GpuTexture Get(string path)
    {
        if (textures.TryGetValue(path, out var texture)) return texture;
        using var stream = assets.Open(path);
        using var decoded = SKBitmap.Decode(stream) ?? throw new IOException($"Cannot decode image: {path}");
        using var rgba = new SKBitmap(decoded.Width, decoded.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(rgba);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(decoded, 0, 0, new SKSamplingOptions(SKFilterMode.Linear));
        canvas.Flush();
        return textures[path] = new(rgba);
    }
    public void Dispose() { foreach (var texture in textures.Values) texture.Dispose(); textures.Clear(); }
}
