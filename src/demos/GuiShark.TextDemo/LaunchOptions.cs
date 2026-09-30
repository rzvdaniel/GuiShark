using System.Globalization;

namespace GuiShark.TextDemo;

internal sealed record LaunchOptions(string Assets, string? Capture, int Width, int Height, LabSettings Settings)
{
    public static LaunchOptions Parse(string[] args)
    {
        string assets = Path.Combine(AppContext.BaseDirectory, "Assets");
        string? capture = null;
        int width = 1440, height = 1040;
        var settings = new LabSettings();
        for (var i = 0; i < args.Length; i++)
        {
            string Value() => i + 1 < args.Length ? args[++i] : throw new ArgumentException("Missing option value.");
            switch (args[i])
            {
                case "--assets": assets = Value(); break;
                case "--capture": capture = Value(); break;
                case "--size":
                    var parts = Value().Split('x');
                    if (parts.Length != 2 || !int.TryParse(parts[0], out width) || !int.TryParse(parts[1], out height) || width < 1180 || height < 900)
                        throw new ArgumentException("--size requires WIDTHxHEIGHT, at least 1180x900.");
                    break;
                case "--density":
                    settings.Density = float.Parse(Value(), CultureInfo.InvariantCulture);
                    if (settings.Density is not (1 or 1.25f or 1.5f or 2)) throw new ArgumentException("Density: 1, 1.25, 1.5 or 2.");
                    break;
                case "--font-size":
                    settings.Size = float.Parse(Value(), CultureInfo.InvariantCulture);
                    if (!float.IsFinite(settings.Size) || settings.Size < 8 || settings.Size > 48) throw new ArgumentException("Font size: 8..48.");
                    break;
                case "--mode": settings.Mode = Value() switch
                    { "compare" => 0, "baseline" => 1, "skia" => 2, "freetype" => 3, "msdf" => 4, _ => throw new ArgumentException("Unknown backend mode.") }; break;
                case "--no-snap": settings.Snap = false; break;
                case "--hinting": settings.Hinting = Value() switch
                    { "none" => GuiShark.OpenGL.TextHinting.None, "slight" => GuiShark.OpenGL.TextHinting.Slight,
                      "normal" => GuiShark.OpenGL.TextHinting.Normal, "full" => GuiShark.OpenGL.TextHinting.Full,
                      _ => throw new ArgumentException("Hinting: none, slight, normal, full.") }; break;
                case "--filter": settings.Sampling = Value() switch
                    { "linear" => GuiShark.OpenGL.TextSampling.Linear, "nearest" => GuiShark.OpenGL.TextSampling.Nearest,
                      _ => throw new ArgumentException("Filter: linear, nearest.") }; break;
                case "--integer": settings.Fractional = false; break;
                case "--bold": settings.Bold = true; break;
                case "--shadow": settings.Shadow = true; break;
                case "--background": settings.Background = Value() switch
                    { "dark" => 0, "light" => 1, "hills" => 2, _ => throw new ArgumentException("Background: dark, light, hills.") }; break;
                default: throw new ArgumentException($"Unknown option: {args[i]}");
            }
        }
        return new(Path.GetFullPath(assets), capture == null ? null : Path.GetFullPath(capture), width, height, settings);
    }
}
