using AngleSharp.Dom;

namespace GuiShark;

internal static class TextInputLoader
{
    public static bool Supports(UiElement element, IElement source) => element.Tag == "textarea" ||
        element.Tag == "input" && (source.GetAttribute("type")?.ToLowerInvariant() ?? "text") is "text" or "password";

    public static void Load(UiElement element, IElement source)
    {
        var input = new UiTextInput(element, element.Tag == "textarea", source.GetAttribute("type")?.Equals("password", StringComparison.OrdinalIgnoreCase) == true && element.Tag == "input") { Placeholder = source.GetAttribute("placeholder") ?? "", ReadOnly = source.HasAttribute("readonly") };
        if (source.GetAttribute("maxlength") is { } length)
        {
            if (!int.TryParse(length, out var maximum) || maximum < 0) throw new FormatException("maxlength must be a nonnegative integer.");
            input.MaximumLength = maximum;
        }
        if (source.GetAttribute("rows") is { } rows)
        {
            if (!int.TryParse(rows, out var count) || count <= 0 || count > 1000) throw new FormatException("rows must be between 1 and 1000.");
            input.Rows = count;
        }
        input.Value = element.Tag == "textarea" ? source.TextContent : source.GetAttribute("value") ?? "";
        element.TextInput = input;
    }
}
