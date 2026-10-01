namespace GuiShark;

internal static class StyleProperty
{
    private static readonly IReadOnlyDictionary<string, Action<UiStyle, string>> Setters =
        new Dictionary<string, Action<UiStyle, string>>(StringComparer.Ordinal)
    {
        ["width"] = (style, value) => { style.Width = CssLength.Parse(value); },
        ["height"] = (style, value) => { style.Height = CssLength.Parse(value); },
        ["max-width"] = (style, value) => { style.MaxWidth = CssLength.Parse(value); },
        ["position"] = (style, value) => { style.Position = ParseChoice("position", value, ("static", ElementPosition.Flow), ("absolute", ElementPosition.Absolute)); },
        ["top"] = (style, value) => { style.Top = CssLength.Parse(value); },
        ["right"] = (style, value) => { style.Right = CssLength.Parse(value); },
        ["bottom"] = (style, value) => { style.Bottom = CssLength.Parse(value); },
        ["left"] = (style, value) => { style.Left = CssLength.Parse(value); },
        ["pointer-events"] = (style, value) => { style.PointerEvents = ParseChoice("pointer-events", value, ("auto", true), ("none", false)); },
        ["padding"] = (style, value) => { style.Padding = ParseInsets(value); },
        ["margin"] = (style, value) => { style.Margin = ParseInsets(value); },
        ["gap"] = (style, value) => { style.Gap = Nonnegative(value); },
        ["flex-grow"] = (style, value) => { style.Grow = Nonnegative(value); },
        ["flex-direction"] = (style, value) => { style.Direction = ParseChoice("flex-direction", value, ("row", FlowDirection.Row), ("column", FlowDirection.Column)); },
        ["align-items"] = (style, value) => { style.Align = ParseChoice("align-items", value, ("stretch", CrossAlignment.Stretch), ("flex-start", CrossAlignment.Start), ("center", CrossAlignment.Center), ("flex-end", CrossAlignment.End)); },
        ["justify-content"] = (style, value) => { style.Justify = ParseChoice("justify-content", value, ("flex-start", MainAlignment.Start), ("center", MainAlignment.Center), ("flex-end", MainAlignment.End), ("space-between", MainAlignment.SpaceBetween)); },
        ["text-align"] = (style, value) => { style.TextAlign = ParseChoice("text-align", value, ("left", TextAlignment.Left), ("center", TextAlignment.Center), ("right", TextAlignment.Right)); },
        ["font-size"] = (style, value) => { style.FontSize = Math.Max(1, Nonnegative(value)); },
        ["font-family"] = (style, value) => { style.FontFamily = string.Join(", ", FontFamilyList.Parse(value)); },
        ["font-weight"] = (style, value) => { style.Bold = ParseChoice("font-weight", value, ("bold", true), ("600", true), ("700", true), ("normal", false), ("400", false)); },
        ["-guishark-accent-color"] = (style, value) => { style.AccentColor = UiColor.Parse(value); },
        ["color"] = (style, value) => { style.Color = UiColor.Parse(value); },
        ["text-shadow"] = (style, value) => { style.TextShadow = ParseShadow(value); },
        ["background"] = (style, value) => { style.BackgroundImage = null; SetBackground(style, value); },
        ["background-color"] = (style, value) => { SetBackground(style, value); },
        ["background-image"] = (style, value) => { style.BackgroundImage = ParseImage(value); },
        ["background-size"] = (style, value) => { style.BackgroundSize = ParseFit(value); },
        ["object-fit"] = (style, value) => { style.ObjectFit = ParseFit(value); },
        ["-guishark-object-flip"] = (style, value) => { style.ObjectFlip = ParseFlip(value); },
        ["-guishark-image-tint"] = (style, value) => { style.ImageTint = UiColor.Parse(value); },
        ["-guishark-background-slice"] = (style, value) => { style.BackgroundSlice = ParseInsets(value); },
        ["-guishark-background-slice-width"] = (style, value) => { style.BackgroundSliceWidth = ParseInsets(value); },
        ["-guishark-background-inset"] = (style, value) => { style.BackgroundInset = ParseInsets(value); },
        ["border-radius"] = (style, value) => { style.Radius = Nonnegative(value); },
        ["border"] = (style, value) => { SetBorder(style, value); },
        ["border-color"] = (style, value) => { style.BorderColor = UiColor.Parse(value); },
        ["opacity"] = (style, value) => { style.Opacity = Math.Clamp(CssLength.Pixels(value), 0, 1); },
        ["overflow-y"] = (style, value) => { style.ScrollY = ParseChoice("overflow-y", value, ("auto", true), ("scroll", true), ("hidden", false)); },
        ["display"] = (style, value) => { style.Hidden = ParseChoice("display", value, ("none", true), ("flex", false), ("block", false)); },
        ["box-sizing"] = (style, value) => { if (value != "border-box") throw Invalid("box-sizing", value); },
    };

    public static void Apply(UiStyle style, string name, string value)
    {
        if (!Setters.TryGetValue(name, out var setter))
            throw new FormatException($"Unsupported CSS property '{name}'. See docs/css-subset.md.");
        setter(style, value);
    }

    private static T ParseChoice<T>(string name, string value, params (string Name, T Value)[] choices)
    {
        foreach (var choice in choices)
            if (choice.Name == value) return choice.Value;
        throw Invalid(name, value);
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
