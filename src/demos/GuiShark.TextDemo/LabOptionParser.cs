using System.Globalization;
using GuiShark.OpenGL;

namespace GuiShark.TextDemo;

/// <summary>Rendering option parsing, separate from window/assets launch options.</summary>
internal static class LabOptionParser
{
    private static readonly IReadOnlyDictionary<string, Action<LabSettings, Func<string>>> Options =
        new Dictionary<string, Action<LabSettings, Func<string>>>(StringComparer.Ordinal)
    {
        ["--sample"] = (settings, value) => settings.Script = LanguageSamples.Parse(value()),
        ["--density"] = (settings, value) => settings.Density = ParseDensity(value()),
        ["--font-size"] = (settings, value) => settings.Size = ParseSize(value()),
        ["--mode"] = (settings, value) => settings.Mode = ParseMode(value()),
        ["--hinting"] = (settings, value) => settings.Hinting = ParseHinting(value()),
        ["--filter"] = (settings, value) => settings.Sampling = ParseSampling(value()),
        ["--background"] = (settings, value) => settings.Background = ParseBackground(value()),
        ["--no-snap"] = (settings, _) => settings.Snap = false,
        ["--integer"] = (settings, _) => settings.Fractional = false,
        ["--bold"] = (settings, _) => settings.Bold = true,
        ["--shadow"] = (settings, _) => settings.Shadow = true
    };
    public static bool Apply(string name, LabSettings settings, Func<string> value)
    {
        if (!Options.TryGetValue(name, out var apply)) return false;
        apply(settings, value); return true;
    }
    private static float ParseDensity(string value)
    {
        var density = float.Parse(value, CultureInfo.InvariantCulture);
        if (density is not (1 or 1.25f or 1.5f or 2)) throw new ArgumentException("Density: 1, 1.25, 1.5 or 2.");
        return density;
    }
    private static float ParseSize(string value)
    {
        var size = float.Parse(value, CultureInfo.InvariantCulture);
        if (!float.IsFinite(size) || size < 8 || size > 48) throw new ArgumentException("Font size: 8..48.");
        return size;
    }
    private static int ParseMode(string value) => value switch
    { "compare" => 0, "skia" => 1, "harfbuzz" => 2, _ => throw new ArgumentException("Unknown backend mode.") };
    private static TextHinting ParseHinting(string value) => value switch
    { "none" => TextHinting.None, "slight" => TextHinting.Slight, "normal" => TextHinting.Normal, "full" => TextHinting.Full, _ => throw new ArgumentException("Hinting: none, slight, normal, full.") };
    private static TextSampling ParseSampling(string value) => value switch
    { "linear" => TextSampling.Linear, "nearest" => TextSampling.Nearest, _ => throw new ArgumentException("Filter: linear, nearest.") };
    private static int ParseBackground(string value) => value switch
    { "dark" => 0, "light" => 1, "hills" => 2, _ => throw new ArgumentException("Background: dark, light, hills.") };
}
