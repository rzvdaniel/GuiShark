using System.Globalization;

namespace GuiShark;

public readonly record struct UiColor(float R, float G, float B, float A = 1)
{
    public static UiColor Transparent => new(0, 0, 0, 0);
    public static UiColor Parse(string value)
    {
        if (value == "transparent") return Transparent;
        if (value == "white") return new(1, 1, 1);
        if (value == "black") return new(0, 0, 0);
        if (!value.StartsWith('#')) throw new FormatException($"Use #RGB, #RRGGBB or #RRGGBBAA colors: {value}");
        var hex = value[1..];
        if (hex.Length == 3) hex = string.Concat(hex.Select(c => $"{c}{c}"));
        if (hex.Length is not (6 or 8)) throw new FormatException($"Invalid color: {value}");
        float Channel(int offset) => byte.Parse(hex.AsSpan(offset, 2), NumberStyles.HexNumber) / 255f;
        return new(Channel(0), Channel(2), Channel(4), hex.Length == 8 ? Channel(6) : 1);
    }
}
