using SkiaSharp;

namespace GuiShark.OpenGL;

/// <summary>Local font assets shared by measurement and rasterizers. Owns Skia typefaces.</summary>
public sealed class FontBook : ITextMetrics, IDisposable
{
    private readonly Dictionary<(string Family, bool Bold), FontAsset> faces = new();
    private bool disposed;
    private readonly Dictionary<(string Family, bool Bold), FontAsset[]> candidates = new();
    internal int Revision { get; private set; }

    /// <summary>Loads the CSS font faces and chooses the host's default family, without duplicating file paths in C#.</summary>
    public FontBook(UiDocument document, string defaultFamily)
    {
        try
        {
            Load(document);
            faces[("", false)] = Resolve(defaultFamily, false);
            faces[("", true)] = Resolve(defaultFamily, true);
        }
        catch { Dispose(); throw; }
    }

    public FontBook(string regularFontPath, string boldFontPath)
    {
        try
        {
            Register("", false, File.ReadAllBytes(regularFontPath));
            Register("", true, File.ReadAllBytes(boldFontPath));
        }
        catch { Dispose(); throw; }
    }

    /// <summary>Loads local TTF/OTF assets declared with CSS @font-face. Call before layout.</summary>
    public void Load(UiDocument document)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        foreach (var face in document.FontFaces)
        {
            using var stream = document.Assets.Open(face.Source);
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            var bytes = buffer.ToArray();
            var key = (Normalize(face.Family), face.Bold);
            if (faces.TryGetValue(key, out var existing))
            {
                if (!existing.Bytes.AsSpan().SequenceEqual(bytes))
                    throw new InvalidOperationException($"Font family '{face.Family}' is already registered with a different file. Create a new FontBook to reload it.");
                continue;
            }
            Register(face.Family, face.Bold, bytes);
        }
    }

    private void Register(string family, bool bold, byte[] bytes)
    {
        using var data = SKData.CreateCopy(bytes);
        var typeface = SKTypeface.FromData(data) ?? throw new IOException($"Cannot load font '{family}'.");
        FontAsset? asset = null;
        try
        {
            asset = new(bytes, typeface);
            faces.Add((Normalize(family), bold), asset);
        }
        catch
        {
            if (asset != null) asset.Dispose();
            else typeface.Dispose();
            throw;
        }
        candidates.Clear();
        Revision++;
    }

    internal FontAsset Resolve(string family, bool bold)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return Candidates(family, bold)[0];
    }

    private FontAsset? Find(string family, bool bold)
    {
        var key = Normalize(family);
        if (faces.TryGetValue((key, bold), out var exact)) return exact;
        if (faces.TryGetValue((key, false), out var regular)) return regular;
        return null;
    }

    private FontAsset[] Candidates(string family, bool bold)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var key = (family, bold);
        if (candidates.TryGetValue(key, out var cached)) return cached;
        var names = family.Length == 0 ? [""] : FontFamilyList.Parse(family);
        var preferred = names.Select(name => Find(name, bold)).OfType<FontAsset>().ToArray();
        if (preferred.Length == 0)
            throw new InvalidOperationException($"Font family '{family}' is not loaded. Declare @font-face and call FontBook.Load(document).");
        var remaining = faces.Keys.Select(k => k.Family).Distinct().Select(name => Find(name, bold)).OfType<FontAsset>();
        var result = preferred.Concat(remaining).Distinct().ToArray();
        if (candidates.Count >= 128) candidates.Remove(candidates.First().Key);
        candidates[key] = result;
        return result;
    }

    internal IReadOnlyList<FontRun> Runs(string text, string family, bool bold) => FontFallback.Select(text, Candidates(family, bold));

    internal SKFont CreateFont(float size, bool bold, string family = "", TextRenderOptions? options = null) =>
        CreateFont(Resolve(family, bold), size, options);

    internal static SKFont CreateFont(FontAsset face, float size, TextRenderOptions? options = null) =>
        new(face.Typeface, size)
        {
            Edging = SKFontEdging.Antialias,
            Subpixel = options == null || !options.PixelSnap,
            Hinting = options?.Hinting switch
            {
                TextHinting.None => SKFontHinting.None,
                TextHinting.Slight => SKFontHinting.Slight,
                TextHinting.Full => SKFontHinting.Full,
                _ => SKFontHinting.Normal
            }
        };

    public float MeasureWidth(string text, float fontSize, bool bold) => MeasureWidth(text, fontSize, bold, "");
    public float MeasureWidth(string text, float fontSize, bool bold, string family)
    {
        var width = 0f;
        foreach (var run in Runs(text, family, bold))
        {
            using var font = CreateFont(run.Font, fontSize);
            width += font.MeasureText(run.Text);
        }
        return width;
    }

    private static string Normalize(string family) => family.Trim().ToUpperInvariant();
    public void Dispose()
    {
        if (disposed) return;
        foreach (var face in faces.Values.Distinct()) face.Dispose();
        faces.Clear();
        candidates.Clear();
        disposed = true;
    }
}
