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
                case "--size": (width, height) = ParseSize(Value()); break;
                default: if (!LabOptionParser.Apply(args[i], settings, Value)) throw new ArgumentException($"Unknown option: {args[i]}"); break;
            }
        }
        return new(Path.GetFullPath(assets), capture == null ? null : Path.GetFullPath(capture), width, height, settings);
    }
    private static (int Width, int Height) ParseSize(string value)
    {
        var parts = value.Split('x');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var width) || !int.TryParse(parts[1], out var height) || width < 1180 || height < 900)
            throw new ArgumentException("--size requires WIDTHxHEIGHT, at least 1180x900.");
        return (width, height);
    }

}
