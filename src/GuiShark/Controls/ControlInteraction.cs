namespace GuiShark;

internal static class ControlInteraction
{
    public static void Drag(UiElement element, float x)
    {
        if (element.Control is not { Kind: UiControlKind.Range } control) return;
        var bounds = element.ContentBounds;
        var width = Math.Max(0, bounds.Width - 16);
        if (width > 0) control.Value = control.Minimum + Math.Clamp((x - bounds.X - 8) / width, 0, 1) * (control.Maximum - control.Minimum);
    }

    public static bool Key(UiElement element, UiKey key)
    {
        if (element.Control is not { Kind: UiControlKind.Range } control) return false;
        var increment = control.Step > 0 ? control.Step : (control.Maximum - control.Minimum) / 100;
        switch (key)
        {
            case UiKey.Left: case UiKey.Down: control.Value -= increment; return true;
            case UiKey.Right: case UiKey.Up: control.Value += increment; return true;
            case UiKey.Home: control.Value = control.Minimum; return true;
            case UiKey.End: control.Value = control.Maximum; return true;
            default: return false;
        }
    }
}
