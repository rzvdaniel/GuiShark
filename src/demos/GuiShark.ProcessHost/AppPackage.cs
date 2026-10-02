using System.Text.Json;

namespace GuiShark.ProcessHost;

internal sealed record AppPackage(string Id, string Title, string EntryAssembly, string AssetsPath, string Document)
{
    public static AppPackage Load(string manifestPath)
    {
        var root = Path.GetDirectoryName(Path.GetFullPath(manifestPath))!;
        using var json = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var config = json.RootElement;
        if (config.GetProperty("protocolVersion").GetInt32() != 1)
            throw new InvalidDataException("Unsupported app protocol version.");
        var assembly = Inside(root, config.GetProperty("entryAssembly").GetString()!);
        var assets = Inside(root, config.GetProperty("assetsDirectory").GetString()!);
        var document = config.GetProperty("document").GetString()!;
        _ = Inside(assets, document);
        if (!File.Exists(assembly) || !Directory.Exists(assets) || !File.Exists(Path.Combine(assets, document)))
            throw new FileNotFoundException("The app package is missing its executable or assets.");
        return new(config.GetProperty("id").GetString()!, config.GetProperty("title").GetString()!, assembly, assets, document);
    }

    private static string Inside(string root, string relative)
    {
        if (Path.IsPathRooted(relative)) throw new InvalidDataException("Package paths must be relative.");
        var full = Path.GetFullPath(Path.Combine(root, relative));
        if (!full.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Package paths must stay inside the package.");
        return full;
    }
}
