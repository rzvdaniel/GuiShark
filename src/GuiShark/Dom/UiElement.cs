namespace GuiShark;

/// <summary>A retained UI node. Mutations invalidate the view; bounds update on Update/Render.</summary>
public sealed class UiElement
{
    private string text;
    private bool disabled;
    private readonly HashSet<string> classes;
    internal event Action? Changed;

    internal UiElement(string tag, string id, string text, string classNames, bool disabled)
    {
        Tag = tag;
        Id = id;
        this.text = text;
        this.disabled = disabled;
        classes = new(classNames.Split(' ', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
    }

    public string Tag { get; }
    public string Id { get; }
    public string Text { get => text; set { if (text != value) { text = value; Invalidate(); } } }
    public bool Disabled { get => disabled; set { if (disabled != value) { disabled = value; Invalidate(); } } }
    public bool IsButton => Tag == "button";
    public UiElement? Parent { get; internal set; }
    public IReadOnlyList<UiElement> Children { get; internal set; } = [];
    public UiStyle Style { get; internal set; } = new();
    public UiRect Bounds { get; internal set; }
    public UiRect Clip { get; internal set; }
    public UiRect ContentBounds => Bounds.Inset(Style.Padding).Inset(Insets.All(Style.BorderWidth));
    public string? ImageSource { get; internal set; }
    public bool IsHovered { get; internal set; }
    public bool IsPressed { get; internal set; }
    public bool IsFocused { get; internal set; }
    internal IReadOnlyDictionary<string, string> InlineStyle { get; set; } = new Dictionary<string, string>();
    public event Action<UiElement>? Clicked;

    public bool HasClass(string name) => classes.Contains(name);
    public void SetClass(string name, bool enabled)
    {
        if (enabled ? classes.Add(name) : classes.Remove(name)) Invalidate();
    }

    public IEnumerable<UiElement> DescendantsAndSelf()
    {
        yield return this;
        foreach (var child in Children)
            foreach (var node in child.DescendantsAndSelf()) yield return node;
    }

    internal void Activate() { if (!Disabled) Clicked?.Invoke(this); }
    internal void Invalidate() => Changed?.Invoke();
}
