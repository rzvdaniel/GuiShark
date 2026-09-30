namespace GuiShark;

/// <summary>A local font declared with @font-face. Paths follow the document's asset source.</summary>
public sealed record FontFace(string Family, string Source, bool Bold)
{
    internal static string ParseFamily(string value)
    {
        var family = value.Trim().Trim('\'', '"');
        if (family.Length == 0 || family.IndexOfAny([',', ';', '(', ')']) >= 0)
            throw new FormatException("font-family accepts one named family; fallback lists are not supported.");
        return family;
    }
}
