namespace GuiShark.OpenGL;

/// <summary>Control geometry shares the regular quad pipeline and clipping.</summary>
internal sealed class ControlPainter(QuadPainter painter)
{
    public void Draw(UiElement element, float opacity)
    {
        if (element.Control is not { } control) return;
        var bounds = element.ContentBounds;
        var accent = element.Style.AccentColor;
        if (control.Kind is UiControlKind.Checkbox or UiControlKind.Radio)
        {
            if (!control.Checked) return;
            var size = Math.Min(bounds.Width, bounds.Height) * .55f;
            painter.Solid(new(bounds.X + (bounds.Width - size) / 2, bounds.Y + (bounds.Height - size) / 2, size, size),
                accent, opacity, control.Kind == UiControlKind.Radio ? size / 2 : 2);
            return;
        }
        if (control.Kind == UiControlKind.Progress)
        {
            painter.Solid(new(bounds.X, bounds.Y, bounds.Width * control.Fraction, bounds.Height), accent, opacity, 3);
            return;
        }
        var trackWidth = Math.Max(0, bounds.Width - 16);
        var centerY = bounds.Y + bounds.Height / 2;
        painter.Solid(new(bounds.X + 8, centerY - 2, trackWidth, 4), element.Style.BorderColor, opacity, 2);
        painter.Solid(new(bounds.X + 8, centerY - 2, trackWidth * control.Fraction, 4), accent, opacity, 2);
        painter.Solid(new(bounds.X + trackWidth * control.Fraction, centerY - 8, 16, 16), accent, opacity, 8);
    }
}
