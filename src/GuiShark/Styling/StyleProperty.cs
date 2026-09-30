namespace GuiShark;

internal static class StyleProperty
{
    public static void Apply(UiStyle style, string name, string value)
    {
        switch (name)
        {
            case "width": style.Width = CssLength.Parse(value); break;
            case "height": style.Height = CssLength.Parse(value); break;
            case "max-width": style.MaxWidth = CssLength.Parse(value); break;
            case "position": style.Position = value switch { "static" => ElementPosition.Flow, "absolute" => ElementPosition.Absolute, _ => throw Invalid(name, value) }; break;
            case "top": style.Top = CssLength.Parse(value); break;
            case "right": style.Right = CssLength.Parse(value); break;
            case "bottom": style.Bottom = CssLength.Parse(value); break;
            case "left": style.Left = CssLength.Parse(value); break;
            case "pointer-events": style.PointerEvents = value switch { "auto" => true, "none" => false, _ => throw Invalid(name, value) }; break;
            case "padding": style.Padding = ParseInsets(value); break;
            case "margin": style.Margin = ParseInsets(value); break;
            case "gap": style.Gap = Nonnegative(value); break;
            case "flex-grow": style.Grow = Nonnegative(value); break;
            case "flex-direction": style.Direction = value switch { "row" => FlowDirection.Row, "column" => FlowDirection.Column, _ => throw Invalid(name, value) }; break;
            case "align-items": style.Align = value switch { "stretch" => CrossAlignment.Stretch, "flex-start" => CrossAlignment.Start, "center" => CrossAlignment.Center, "flex-end" => CrossAlignment.End, _ => throw Invalid(name, value) }; break;
            case "justify-content": style.Justify = value switch { "flex-start" => MainAlignment.Start, "center" => MainAlignment.Center, "flex-end" => MainAlignment.End, "space-between" => MainAlignment.SpaceBetween, _ => throw Invalid(name, value) }; break;
            case "text-align": style.TextAlign = value switch { "left" => TextAlignment.Left, "center" => TextAlignment.Center, "right" => TextAlignment.Right, _ => throw Invalid(name, value) }; break;
            case "font-size": style.FontSize = Math.Max(1, Nonnegative(value)); break;
            case "font-family": style.FontFamily = FontFace.ParseFamily(value); break;
            case "font-weight": style.Bold = value switch { "bold" or "600" or "700" => true, "normal" or "400" => false, _ => throw Invalid(name, value) }; break;
            case "color": style.Color = UiColor.Parse(value); break;
            case "text-shadow": style.TextShadow = ParseShadow(value); break;
            case "background": style.BackgroundImage = null; SetBackground(style, value); break;
            case "background-color": SetBackground(style, value); break;
            case "background-image": style.BackgroundImage = ParseImage(value); break;
            case "background-size": style.BackgroundSize = ParseFit(value); break;
            case "object-fit": style.ObjectFit = ParseFit(value); break;
            case "-guishark-object-flip": style.ObjectFlip = ParseFlip(value); break;
            case "-guishark-image-tint": style.ImageTint = UiColor.Parse(value); break;
            case "-guishark-background-slice": style.BackgroundSlice = ParseInsets(value); break;
            case "-guishark-background-slice-width": style.BackgroundSliceWidth = ParseInsets(value); break;
            case "-guishark-background-inset": style.BackgroundInset = ParseInsets(value); break;
            case "border-radius": style.Radius = Nonnegative(value); break;
            case "border": SetBorder(style, value); break;
            case "border-color": style.BorderColor = UiColor.Parse(value); break;
            case "opacity": style.Opacity = Math.Clamp(CssLength.Pixels(value), 0, 1); break;
            case "display": style.Hidden = value switch { "none" => true, "flex" or "block" => false, _ => throw Invalid(name, value) }; break;
            case "box-sizing": if (value != "border-box") throw Invalid(name, value); break;
            default: throw new FormatException($"Unsupported CSS property '{name}'. See docs/css-subset.md.");
        }
    }

    private static ImageFit ParseFit(string value) => value switch
    {
        "fill" => ImageFit.Fill,
        "contain" => ImageFit.Contain,
        "cover" => ImageFit.Cover,
        _ => throw Invalid("image fit", value)
    };

    private static ImageFlip ParseFlip(string value) => value switch
    {
        "none" => ImageFlip.None,
        "horizontal" => ImageFlip.Horizontal,
        "vertical" => ImageFlip.Vertical,
        "both" => ImageFlip.Both,
        _ => throw Invalid("image flip", value)
    };

    private static TextShadow? ParseShadow(string value)
    {
        if (value == "none") return null;
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3) throw Invalid("text-shadow", value);
        return new(CssLength.Pixels(parts[0]), CssLength.Pixels(parts[1]), UiColor.Parse(parts[2]));
    }

    private static string? ParseImage(string value)
    {
        if (value == "none") return null;
        if (!value.StartsWith("url(") || !value.EndsWith(')')) throw Invalid("background-image", value);
        var path = value[4..^1].Trim();
        if (path.Length >= 2 && (path[0] == '\"' || path[0] == '\''))
        {
            if (path[^1] != path[0]) throw Invalid("background-image", value);
            path = path[1..^1];
        }
        if (string.IsNullOrWhiteSpace(path) || path.IndexOfAny(['\"', '\'', '(', ')', ';']) >= 0)
            throw Invalid("background-image", value);
        return path;
    }

    private static void SetBackground(UiStyle style, string value)
    {
        if (value.StartsWith("linear-gradient(to bottom,") && value.EndsWith(')'))
        {
            var colors = value[26..^1].Split(',').Select(v => UiColor.Parse(v.Trim())).ToArray();
            if (colors.Length != 2) throw Invalid("background", value);
            style.Background = colors[0];
            style.GradientEnd = colors[1];
        }
        else style.Background = style.GradientEnd = UiColor.Parse(value);
    }

    private static void SetBorder(UiStyle style, string value)
    {
        if (value == "none") { style.BorderWidth = 0; return; }
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3 || parts[1] != "solid") throw Invalid("border", value);
        style.BorderWidth = Nonnegative(parts[0]);
        style.BorderColor = UiColor.Parse(parts[2]);
    }

    private static Insets ParseInsets(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Nonnegative).ToArray();
        return parts.Length switch
        {
            1 => Insets.All(parts[0]),
            2 => new(parts[0], parts[1], parts[0], parts[1]),
            3 => new(parts[0], parts[1], parts[2], parts[1]),
            4 => new(parts[0], parts[1], parts[2], parts[3]),
            _ => throw Invalid("spacing", value)
        };
    }

    private static float Nonnegative(string value) => Math.Max(0, CssLength.Pixels(value));
    private static FormatException Invalid(string name, string value) => new($"Unsupported {name}: {value}");
}
