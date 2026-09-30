namespace GuiShark;

internal static class FontFaceParser
{
    public static FontFace Parse(string declarations)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var declaration in declarations.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var colon = declaration.IndexOf(':');
            if (colon < 1) throw new FormatException("Invalid @font-face declaration.");
            var name = declaration[..colon].Trim().ToLowerInvariant();
            if (name is not ("font-family" or "src" or "font-weight"))
                throw new FormatException($"Unsupported @font-face descriptor: {name}");
            values[name] = declaration[(colon + 1)..].Trim();
        }
        if (!values.TryGetValue("font-family", out var family) || !values.TryGetValue("src", out var src))
            throw new FormatException("@font-face requires font-family and src.");
        var weight = values.GetValueOrDefault("font-weight", "normal");
        var bold = weight switch
        {
            "normal" or "400" => false, "bold" or "600" or "700" => true,
            _ => throw new FormatException("Unsupported font weight.")
        };
        return new(FontFace.ParseFamily(family), ParseSource(src), bold);
    }

    private static string ParseSource(string src)
    {
        if (!src.StartsWith("url(") || !src.EndsWith(')')) throw new FormatException("Font src requires one url(...).");
        var path = src[4..^1].Trim().Trim('\'', '"');
        if (path.Length == 0 || path.IndexOfAny(['(', ')', ',']) >= 0) throw new FormatException("Invalid font src.");
        return path;
    }
}
