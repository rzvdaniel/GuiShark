namespace GuiShark.Balloon;

internal sealed record LaunchOptions(string AssetsPath, bool Play, string? CapturePath, int Width, int Height)
{
    public static LaunchOptions Parse(string[] args)
    {
        var assets = Path.Combine(AppContext.BaseDirectory, "Assets");
        var play = false;
        string? capture = null;
        var width = 1200;
        var height = 820;
        for (var i = 0; i < args.Length; i++)
            switch (args[i])
            {
                case "--play": play = true; break;
                case "--assets": assets = Value(args, ref i); break;
                case "--capture": capture = Value(args, ref i); break;
                case "--size": (width, height) = ParseSize(Value(args, ref i)); break;
                default: throw new ArgumentException($"Unknown option: {args[i]}");
            }
        return new(Path.GetFullPath(assets), play, capture == null ? null : Path.GetFullPath(capture), width, height);
    }

    private static (int Width, int Height) ParseSize(string value)
    {
        var parts = value.Split('x');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var width) || !int.TryParse(parts[1], out var height)
            || width < 920 || height < 680)
            throw new ArgumentException("Window size must be WIDTHxHEIGHT, at least 920x680.");
        return (width, height);
    }

    private static string Value(string[] args, ref int index) => ++index < args.Length
        ? args[index] : throw new ArgumentException("Missing option value.");
}
