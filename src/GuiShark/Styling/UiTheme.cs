namespace GuiShark;

/// <summary>Base styling; application CSS takes precedence.</summary>
public enum UiTheme { None, Neutral }

internal static class NeutralTheme
{
    public static void Apply(UiElement element, UiStyle style)
    {
        if (element.IsOverlay)
        {
            StyleProperty.Apply(style, "background", "#152638");
            StyleProperty.Apply(style, "border", "1px solid #68849e");
            StyleProperty.Apply(style, "border-radius", "10px");
            StyleProperty.Apply(style, "color", "#eef6ff");
            return;
        }
        if (!element.IsButton && element.Control == null && element.TextInput == null && element.Select == null && element.Tag != "option") return;
        StyleProperty.Apply(style, "background", element.IsPressed ? "#263c55" : element.IsHovered || element.IsSelected ? "#3d526b" : "#304259");
        StyleProperty.Apply(style, "border", $"1px solid {(element.IsFocused ? "#93c5fd" : "#60758e")}");
        StyleProperty.Apply(style, "border-radius", element.Control?.Kind == UiControlKind.Radio ? "12px" : "6px");
        StyleProperty.Apply(style, "color", "#f0f5ff");
        if (element.Disabled) style.Opacity = .45f;
    }
}
