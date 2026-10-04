using System.Text.Json;

namespace GuiShark.ProcessHosting;

public sealed record AppPackage(string Id, string Title, string EntryAssembly, string AssetsPath, string Document)
{
    public static AppPackage Load(string manifestPath)
    {
        var root = Path.GetDirectoryName(Path.GetFullPath(manifestPath))!;
        using var json = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var config = json.RootElement;
        if (config.ValueKind != JsonValueKind.Object || !config.TryGetProperty("protocolVersion", out var version)
            || version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var number) || number != 1)
            throw new InvalidDataException("Unsupported app protocol version.");
        var assembly = Inside(root, ReadString(config, "entryAssembly"));
        var assets = Inside(root, ReadString(config, "assetsDirectory"));
        var document = ReadString(config, "document");
        _ = Inside(assets, document);
        if (!File.Exists(assembly) || !Directory.Exists(assets) || !File.Exists(Path.Combine(assets, document)))
            throw new FileNotFoundException("The app package is missing its executable or assets.");
        return new(ReadString(config, "id"), ReadString(config, "title"), assembly, assets, document);
    }

    private static string ReadString(JsonElement config, string name)
    {
        if (!config.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String)
            throw new InvalidDataException($"Missing package field: {name}");
        var value = property.GetString();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 4096)
            throw new InvalidDataException($"Invalid package field: {name}");
        if (name is "title" or "id" && value.Length > 100)
            throw new InvalidDataException($"Package {name} must be at most 100 characters.");
        return value;
    }

    private static string Inside(string root, string relative)
    {
        if (Path.IsPathRooted(relative)) throw new InvalidDataException("Package paths must be relative.");
        var full = Path.GetFullPath(Path.Combine(root, relative));
        if (!full.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, (OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)))
            throw new InvalidDataException("Package paths must stay inside the package.");
        return full;
    }
}
