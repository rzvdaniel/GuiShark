namespace GuiShark;

internal sealed class StyleResolver(IEnumerable<CssRule> rules, UiTheme theme)
{
    private readonly CssRule[] rules = rules.OrderBy(r => r.Selector.Specificity).ToArray();

    public void Resolve(UiElement element, UiStyle? parent = null)
    {
        var style = UiStyle.Default(element, parent);
        if (theme == UiTheme.Neutral) NeutralTheme.Apply(element, style);
        foreach (var rule in rules)
            if (rule.Selector.Matches(element)) Apply(style, rule.Declarations);
        Apply(style, element.InlineStyle);
        element.Style = style;
        foreach (var child in element.Children) Resolve(child, style);
    }

    private static void Apply(UiStyle style, IReadOnlyDictionary<string, string> declarations)
    {
        foreach (var (name, value) in declarations) StyleProperty.Apply(style, name, value);
    }
}
