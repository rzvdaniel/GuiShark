namespace GuiShark;

/// <summary>A retained UI node. Mutations invalidate the view; bounds update on Update/Render.</summary>
public sealed class UiElement
{
    private string text;
    private bool disabled;
    private bool hidden;
    private string? tooltipText;
    private UiTextDirection? direction;
    private readonly HashSet<string> classes;
    internal event Action? Changed;

    internal UiElement(string tag, string id, string text, string classNames, bool disabled)
    {
        Tag = tag;
        Id = id;
        this.text = text;
        this.disabled = disabled;
        Scroll = new(this);
        classes = new(classNames.Split(' ', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
    }

    public string Tag { get; }
    public UiTextDirection? Direction { get => direction; set { if (direction != value) { direction = value; Invalidate(); } } }
    public UiTextDirection TextDirection => Direction ?? Parent?.TextDirection ?? UiTextDirection.LeftToRight;
    public string Id { get; }
    public string Text { get => text; set { if (text != value) { text = value; Invalidate(); } } }
    public bool Disabled { get => disabled; set { if (disabled != value) { disabled = value; Invalidate(); } } }
    public bool IsButton => Tag == "button";
    public UiTextInput? TextInput { get; internal set; }
    public UiRect TextBounds => TextInput?.TextBounds ?? ContentBounds;
    public UiControl? Control { get; internal set; }
    public bool IsInteractive => IsButton || TextInput != null || Select != null || Control?.IsInteractive == true;
    public UiSelect? Select { get; internal set; }
    public UiDialog? Dialog { get; internal set; }
    public string? TooltipText { get => tooltipText; set { if (tooltipText != value) { tooltipText = value; Invalidate(); } } }
    internal string? TooltipTargetId { get; set; }
    internal UiElement? TooltipTarget { get; set; }
    internal bool IsOverlay => Dialog != null || Role == "tooltip";
    public UiTabs? TabGroup { get; internal set; }
    public UiScroll Scroll { get; }
    public bool AutoFocus { get; internal set; }
    public string? Role { get; internal set; }
    public bool IsSelected { get; internal set; }
    public bool Hidden { get => hidden; set { if (hidden != value) { hidden = value; Invalidate(); } } }
    internal string? ControlTargetId { get; set; }
    internal string? OptionValue { get; set; }
    internal string? LabelFor { get; set; }
    internal UiElement? LabelTarget { get; set; }
    public UiElement? Parent { get; internal set; }
    public IReadOnlyList<UiElement> Children { get; internal set; } = [];
    public UiStyle Style { get; internal set; } = new();
    public UiRect Bounds { get; internal set; }
    public UiRect Clip { get; internal set; }
    public UiRect ContentBounds => Bounds.Inset(Style.Padding).Inset(Insets.All(Style.BorderWidth))
        .Inset(Style.ScrollY ? new Insets(0, 12, 0, 0) : default);
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

    internal void Activate()
    {
        if (Disabled) return;
        Control?.Activate();
        TabGroup?.Select(this);
        Clicked?.Invoke(this);
    }
    internal void Invalidate() => Changed?.Invoke();
}
