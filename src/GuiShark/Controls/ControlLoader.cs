using AngleSharp.Dom;
using System.Globalization;

namespace GuiShark;

internal static class ControlLoader
{
    public static void Load(UiElement element, IElement source)
    {
        element.LabelFor = source.GetAttribute("for");
        if (element.Tag is not ("input" or "progress")) return;
        if (element.Tag == "input" && (source.GetAttribute("type")?.ToLowerInvariant() ?? "text") == "text")
        {
            var input = new UiTextInput(element) { Placeholder = source.GetAttribute("placeholder") ?? "", ReadOnly = source.HasAttribute("readonly") };
            if (source.GetAttribute("maxlength") is { } length)
            {
                if (!int.TryParse(length, out var maximum) || maximum < 0) throw new FormatException("maxlength must be a nonnegative integer.");
                input.MaximumLength = maximum;
            }
            input.Value = source.GetAttribute("value") ?? "";
            element.TextInput = input;
            return;
        }
        var kind = element.Tag == "progress" ? UiControlKind.Progress : source.GetAttribute("type")?.ToLowerInvariant() switch
        {
            "checkbox" => UiControlKind.Checkbox, "radio" => UiControlKind.Radio, "range" => UiControlKind.Range,
            _ => throw new FormatException("Supported input types: text, checkbox, radio, range.")
        };
        var min = kind == UiControlKind.Progress ? 0 : Number(source, "min", 0);
        var max = Number(source, "max", kind == UiControlKind.Progress ? 1 : 100);
        var step = source.GetAttribute("step") == "any" ? 0 : Number(source, "step", 1);
        if (max <= min || !float.IsFinite(max - min) || step < 0) throw new FormatException("Controls require max > min and step >= 0.");
        element.Control = new(element, kind, source.GetAttribute("name") ?? "", min, max, step);
        element.Control.Value = Number(source, "value", kind == UiControlKind.Range ? min + (max - min) / 2 : min);
        element.Control.Checked = source.HasAttribute("checked");
    }
    private static float Number(IElement source, string name, float fallback)
    {
        var attribute = source.GetAttribute(name);
        if (attribute == null) return fallback;
        if (float.TryParse(attribute, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && float.IsFinite(value)) return value;
        throw new FormatException($"Invalid {name}: {attribute}");
    }
}
