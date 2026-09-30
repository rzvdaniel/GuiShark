using System.Runtime.InteropServices;
using System.Text.Json;
using SkiaSharp;

namespace GuiShark.OpenGL;

/// <summary>Loads bottom-origin MSDF atlas PNG + JSON exported by msdf-atlas-gen.</summary>
public sealed class MsdfAtlas
{
    internal TextImage Image { get; }
    internal Dictionary<int, MsdfGlyph> Glyphs { get; } = new();
    internal float Ascender { get; }
    internal float Descender { get; }
    internal float Range { get; }

    public MsdfAtlas(IAssetSource assets, string imagePath, string metadataPath)
    {
        using var json = JsonDocument.Parse(assets.ReadText(metadataPath));
        var root = json.RootElement;
        var atlas = root.GetProperty("atlas");
        if (atlas.GetProperty("type").GetString() != "msdf" || atlas.GetProperty("yOrigin").GetString() != "bottom")
            throw new FormatException("Expected an MSDF atlas with bottom yOrigin.");
        using var stream = assets.Open(imagePath);
        using var decoded = SKBitmap.Decode(stream) ?? throw new IOException($"Cannot decode atlas: {imagePath}");
        using var rgba = new SKBitmap(decoded.Width, decoded.Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        if (!decoded.CopyTo(rgba, SKColorType.Rgba8888)) throw new IOException("Cannot convert MSDF data to RGBA.");
        Image = new(rgba.Width, rgba.Height);
        Marshal.Copy(rgba.GetPixels(), Image.Pixels, 0, Image.Pixels.Length);
        if (Image.Width != atlas.GetProperty("width").GetInt32() || Image.Height != atlas.GetProperty("height").GetInt32())
            throw new FormatException("MSDF PNG dimensions do not match JSON.");
        Range = atlas.GetProperty("distanceRange").GetSingle();
        var metrics = root.GetProperty("metrics");
        Ascender = metrics.GetProperty("ascender").GetSingle();
        Descender = metrics.GetProperty("descender").GetSingle();
        foreach (var entry in root.GetProperty("glyphs").EnumerateArray())
        {
            UiRect plane = default, source = default;
            if (entry.TryGetProperty("planeBounds", out var p) && entry.TryGetProperty("atlasBounds", out var a))
            {
                plane = new(Number(p, "left"), -Number(p, "top"), Number(p, "right") - Number(p, "left"), Number(p, "top") - Number(p, "bottom"));
                source = new(Number(a, "left") / Image.Width, (Image.Height - Number(a, "top")) / Image.Height,
                    (Number(a, "right") - Number(a, "left")) / Image.Width, (Number(a, "top") - Number(a, "bottom")) / Image.Height);
            }
            Glyphs.Add(entry.GetProperty("unicode").GetInt32(), new(entry.GetProperty("advance").GetSingle(), plane, source));
        }
        if (!Glyphs.ContainsKey('?')) throw new FormatException("MSDF atlas must contain '?' for missing glyphs.");
    }

    private static float Number(JsonElement element, string name) => element.GetProperty(name).GetSingle();
}

internal sealed record MsdfGlyph(float Advance, UiRect Plane, UiRect Source);
