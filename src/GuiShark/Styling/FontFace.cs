namespace GuiShark;

/// <summary>A local font declared with @font-face. Paths follow the document's asset source.</summary>
public sealed record FontFace(string Family, string Source, bool Bold)
{
    internal static string ParseFamily(string value)
    {
        var family = value.Trim();
        if (family.Length > 0 && family[0] is '\'' or '"')
        {
            if (family.Length < 2 || family[^1] != family[0]) throw new FormatException("Unclosed font family name.");
            family = family[1..^1];
        }
        if (family.Length == 0 || family.IndexOfAny([',', ';', '(', ')', '\'', '"']) >= 0)
            throw new FormatException("Expected one local font family name.");
        return family;
    }
}
