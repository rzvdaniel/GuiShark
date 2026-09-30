namespace GuiShark.Balloon;

internal sealed record LaunchOptions(string AssetsPath, bool Play, string? CapturePath)
{
    public static LaunchOptions Parse(string[] args)
    {
        var assets = Path.Combine(AppContext.BaseDirectory, "Assets");
        var play = false;
        string? capture = null;
        for (var i = 0; i < args.Length; i++)
            switch (args[i])
            {
                case "--play": play = true; break;
                case "--assets": assets = Value(args, ref i); break;
                case "--capture": capture = Value(args, ref i); break;
                default: throw new ArgumentException($"Unknown option: {args[i]}");
            }
        return new(Path.GetFullPath(assets), play, capture == null ? null : Path.GetFullPath(capture));
    }

    private static string Value(string[] args, ref int index) => ++index < args.Length
        ? args[index] : throw new ArgumentException("Missing option value.");
}
