using System.Globalization;

namespace GuiShark;

public readonly record struct CssLength(float Value, bool Percent = false, bool IsAuto = false)
{
    public static CssLength Auto => new(0, IsAuto: true);
    public float Resolve(float available, float fallback) => IsAuto ? fallback : Percent ? available * Value / 100 : Value;

    public static CssLength Parse(string value)
    {
        if (value == "auto") return Auto;
        var percent = value.EndsWith('%');
        var number = percent ? value[..^1] : value.EndsWith("px") ? value[..^2] : value;
        var parsed = float.Parse(number, CultureInfo.InvariantCulture);
        if (!float.IsFinite(parsed) || parsed < 0) throw new FormatException($"Expected a finite, nonnegative length: {value}");
        return new(parsed, percent);
    }

    internal static float Pixels(string value)
    {
        var length = Parse(value);
        if (length.IsAuto || length.Percent || !float.IsFinite(length.Value))
            throw new FormatException($"Expected a finite pixel length: {value}");
        return length.Value;
    }
}
