namespace GuiShark;

public sealed class UiDocument
{
    private readonly Dictionary<string, UiElement> elements;
    internal StyleResolver Styles { get; }
    public UiElement Root { get; }
    public IAssetSource Assets { get; }
    public IReadOnlyList<FontFace> FontFaces { get; }

    internal UiDocument(UiElement root, IEnumerable<CssRule> rules, IAssetSource assets, IReadOnlyList<FontFace> fontFaces)
    {
        Root = root;
        Assets = assets;
        FontFaces = fontFaces;
        Styles = new(rules);
        elements = new(StringComparer.Ordinal);
        foreach (var element in root.DescendantsAndSelf().Where(e => e.Id.Length > 0))
            if (!elements.TryAdd(element.Id, element)) throw new FormatException($"Duplicate element id: {element.Id}");
    }

    public UiElement GetElement(string id) => elements.TryGetValue(id, out var element)
        ? element : throw new KeyNotFoundException($"No UI element with id '{id}'.");
}
