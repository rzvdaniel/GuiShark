namespace GuiShark;

/// <summary>Ordered local font families. Uses the same bounded name syntax as @font-face.</summary>
public static class FontFamilyList
{
    public static IReadOnlyList<string> Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Split(',').Select(FontFace.ParseFamily).ToArray();
    }
}
