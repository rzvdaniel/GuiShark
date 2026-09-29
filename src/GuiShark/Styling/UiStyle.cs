namespace GuiShark;

public enum FlowDirection { Column, Row }
public enum CrossAlignment { Stretch, Start, Center, End }
public enum MainAlignment { Start, Center, End, SpaceBetween }
public enum TextAlignment { Left, Center, Right }

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
    public UiColor Background { get; internal set; } = UiColor.Transparent;
    public UiColor GradientEnd { get; internal set; } = UiColor.Transparent;
    public float Opacity { get; internal set; } = 1;
    public bool Hidden { get; internal set; }
    public float LineHeight => FontSize * 1.45f;

    internal static UiStyle Default(UiElement element, UiStyle? parent) => new()
    {
        FontSize = parent?.FontSize ?? 14,
        Color = parent?.Color ?? new(.12f, .15f, .2f),
        Bold = parent?.Bold ?? false,
        TextAlign = element.IsButton ? TextAlignment.Center : parent?.TextAlign ?? TextAlignment.Left,
        Padding = element.IsButton ? new(10, 18, 10, 18) : default
    };
}
