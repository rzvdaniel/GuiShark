namespace GuiShark;

public sealed class UiDocument
{
    private readonly Dictionary<string, UiElement> elements;
    internal StyleResolver Styles { get; }
    public UiElement Root { get; }
    public IAssetSource Assets { get; }
    public IReadOnlyList<FontFace> FontFaces { get; }
    public IReadOnlyList<UiTabs> TabGroups { get; }

    internal UiDocument(UiElement root, IEnumerable<CssRule> rules, IAssetSource assets, IReadOnlyList<FontFace> fontFaces, UiTheme theme)
    {
        Root = root;
        Assets = assets;
        FontFaces = fontFaces;
        Styles = new(rules, theme);
        elements = new(StringComparer.Ordinal);
        foreach (var element in root.DescendantsAndSelf().Where(e => e.Id.Length > 0))
            if (!elements.TryAdd(element.Id, element)) throw new FormatException($"Duplicate element id: {element.Id}");
        if (root.DescendantsAndSelf().Any(e => e.Tag == "option" && e.Parent?.Tag != "select"))
            throw new FormatException("Options must be direct children of a select.");
        TabGroups = root.DescendantsAndSelf().Where(e => e.Role == "tablist").Select(e => new UiTabs(e, this)).ToArray();
        foreach (var owner in root.DescendantsAndSelf().Where(e => e.TooltipTargetId != null))
        {
            var target = GetElement(owner.TooltipTargetId!);
            if (target.Role != "tooltip") throw new FormatException("Tooltip aria-describedby requires a role=tooltip target.");
            owner.TooltipTarget = target;
        }
        foreach (var label in root.DescendantsAndSelf().Where(e => e.Tag == "label"))
        {
            label.LabelTarget = label.LabelFor is { } id && elements.TryGetValue(id, out var target)
                ? target : label.DescendantsAndSelf().FirstOrDefault(e => e.IsInteractive);
        }
        foreach (var radio in root.DescendantsAndSelf().Where(e => e.Control is { Kind: UiControlKind.Radio, Checked: true }).ToArray())
        {
            radio.Control!.Checked = false;
            radio.Control.Checked = true;
        }
    }

    public UiElement GetElement(string id) => elements.TryGetValue(id, out var element)
        ? element : throw new KeyNotFoundException($"No UI element with id '{id}'.");
}
