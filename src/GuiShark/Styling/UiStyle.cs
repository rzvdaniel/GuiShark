namespace GuiShark;

public enum FlowDirection { Column, Row }
public enum CrossAlignment { Stretch, Start, Center, End }
public enum MainAlignment { Start, Center, End, SpaceBetween }
public enum TextAlignment { Left, Center, Right }
public enum ElementPosition { Flow, Absolute }
public enum ImageFit { Fill, Contain, Cover }

public sealed class UiStyle
{
    public CssLength Width { get; internal set; } = CssLength.Auto;
    public CssLength Height { get; internal set; } = CssLength.Auto;
    public CssLength MaxWidth { get; internal set; } = CssLength.Auto;
    public Insets Padding { get; internal set; }
    public Insets Margin { get; internal set; }
    public float Gap { get; internal set; }
    public float Grow { get; internal set; }
    public FlowDirection Direction { get; internal set; }
    public CrossAlignment Align { get; internal set; } = CrossAlignment.Stretch;
    public MainAlignment Justify { get; internal set; }
    public TextAlignment TextAlign { get; internal set; }
    public float FontSize { get; internal set; } = 14;
    public bool Bold { get; internal set; }
    public float Radius { get; internal set; }
    public float BorderWidth { get; internal set; }
    public UiColor BorderColor { get; internal set; } = UiColor.Transparent;
    public UiColor Color { get; internal set; } = new(.12f, .15f, .2f);
    public TextShadow? TextShadow { get; internal set; }
    public UiColor Background { get; internal set; } = UiColor.Transparent;
    public UiColor GradientEnd { get; internal set; } = UiColor.Transparent;
    public string? BackgroundImage { get; internal set; }
    public ImageFit BackgroundSize { get; internal set; } = ImageFit.Fill;
    public ImageFit ObjectFit { get; internal set; } = ImageFit.Fill;
    public UiColor ImageTint { get; internal set; } = new(1, 1, 1);
    public Insets BackgroundSlice { get; internal set; }
    public Insets? BackgroundSliceWidth { get; internal set; }
    public Insets BackgroundInset { get; internal set; }
    public float Opacity { get; internal set; } = 1;
    public bool Hidden { get; internal set; }
    public bool PointerEvents { get; internal set; } = true;
    public ElementPosition Position { get; internal set; }
    public CssLength Top { get; internal set; } = CssLength.Auto;
    public CssLength Right { get; internal set; } = CssLength.Auto;
    public CssLength Bottom { get; internal set; } = CssLength.Auto;
    public CssLength Left { get; internal set; } = CssLength.Auto;
    public float LineHeight => FontSize * 1.45f;

    internal static UiStyle Default(UiElement element, UiStyle? parent) => new()
    {
        FontSize = parent?.FontSize ?? 14,
        Color = parent?.Color ?? new(.12f, .15f, .2f),
        TextShadow = parent?.TextShadow,
        Bold = parent?.Bold ?? false,
        PointerEvents = parent?.PointerEvents ?? true,
        TextAlign = element.IsButton ? TextAlignment.Center : parent?.TextAlign ?? TextAlignment.Left,
        Padding = element.IsButton ? new(10, 18, 10, 18) : default
    };
}
