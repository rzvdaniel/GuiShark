namespace GuiShark.ProcessHost;

internal sealed record HostOptions(string ManifestPath, bool Verify, bool Capture)
{
    public static HostOptions Parse(string[] args)
    {
        string? manifest = null;
        var verify = false;
        var capture = false;
        var index = 0;
        while (index < args.Length)
        {
            switch (args[index])
            {
                case "--app" when index + 1 < args.Length: manifest = args[index + 1]; index++; break;
                case "--verify": verify = true; break;
                case "--capture": capture = true; break;
                default: throw new ArgumentException($"Unknown option: {args[index]}");
            }
            index++;
        }
        return new(Path.GetFullPath(manifest ?? DefaultManifest()), verify, capture);
    }

    private static string DefaultManifest()
    {
        var output = new DirectoryInfo(AppContext.BaseDirectory);
        var configuration = output.Parent?.Name ?? "Debug";
        return Path.Combine(AppContext.BaseDirectory, "../../../../GuiShark.AuroraProcess/bin", configuration, "net10.0/app.json");
    }
}
