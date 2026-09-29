namespace GuiShark;

/// <summary>Methods return true when the UI consumes input. Coordinates are logical UI pixels.</summary>
public sealed class UiInput
{
    private readonly UiView view;
    private UiElement? hovered;
    private UiElement? pressed;
    private UiElement? keyboardPressed;
    private UiKey? activationKey;
    public UiElement? Focused { get; private set; }
    public bool HasPointerCapture => pressed != null;
    public bool WantsKeyboard => Focused != null;

    internal UiInput(UiView view) => this.view = view;

    public bool PointerMove(float x, float y)
    {
        view.Update();
        var hit = HitTester.Hit(view.Document.Root, x, y);
        if (hovered != hit)
        {
            SetHover(hovered, false);
            hovered = hit;
            SetHover(hovered, true);
            view.Invalidate();
        }
        if (pressed != null) SetPressed(pressed, HitTester.Button(hit) == pressed);
        return hit != null || HasPointerCapture;
    }

    public bool PointerDown(float x, float y)
    {
        if (pressed != null) SetPressed(pressed, false);
        pressed = null;
        PointerMove(x, y);
        var button = HitTester.Button(hovered);
        SetFocus(button != null && HitTester.CanActivate(button) ? button : null);
        if (Focused != null) { pressed = Focused; SetPressed(pressed, true); }
        return hovered != null;
    }

    public bool PointerUp(float x, float y)
    {
        PointerMove(x, y);
        var target = pressed;
        pressed = null;
        if (target == null) return hovered != null;
        SetPressed(target, false);
        if (HitTester.Button(hovered) == target && HitTester.CanActivate(target)) target.Activate();
        return true;
    }

    public bool KeyDown(UiKey key, bool shift = false, bool repeat = false)
    {
        view.Update();
        if (key == UiKey.Tab) { if (!repeat) AdvanceFocus(shift); return Focused != null; }
        if (key == UiKey.Escape) { var consumed = Focused != null || HasPointerCapture; Cancel(); return consumed; }
        if (Focused == null) return false;
        if (!repeat && keyboardPressed == null && HitTester.CanActivate(Focused))
        {
            activationKey = key;
            keyboardPressed = Focused;
            SetPressed(keyboardPressed, true);
        }
        return true;
    }

    public bool KeyUp(UiKey key)
    {
        view.Update();
        if (activationKey != key || keyboardPressed == null) return false;
        var target = keyboardPressed;
        keyboardPressed = null;
        activationKey = null;
        SetPressed(target, false);
        if (target == Focused && HitTester.CanActivate(target)) target.Activate();
        return true;
    }

    public void Cancel()
    {
        if (pressed != null) SetPressed(pressed, false);
        if (keyboardPressed != null) SetPressed(keyboardPressed, false);
        pressed = keyboardPressed = null;
        activationKey = null;
        SetHover(hovered, false);
        hovered = null;
        SetFocus(null);
        view.Invalidate();
    }

    internal void ValidateTargets()
    {
        if (Focused != null && !HitTester.CanActivate(Focused)) Cancel();
    }

    private void AdvanceFocus(bool backwards)
    {
        var buttons = view.Document.Root.DescendantsAndSelf().Where(HitTester.CanActivate).ToList();
        if (buttons.Count == 0) { SetFocus(null); return; }
        var index = Focused == null ? (backwards ? 0 : -1) : buttons.IndexOf(Focused);
        SetFocus(buttons[(index + (backwards ? buttons.Count - 1 : 1)) % buttons.Count]);
    }

    private void SetFocus(UiElement? element)
    {
        if (Focused == element) return;
        if (keyboardPressed != null) SetPressed(keyboardPressed, false);
        keyboardPressed = null;
        activationKey = null;
        if (Focused != null) Focused.IsFocused = false;
        Focused = element;
        if (Focused != null) Focused.IsFocused = true;
        view.Invalidate();
    }

    private void SetPressed(UiElement element, bool value)
    {
        if (element.IsPressed == value) return;
        element.IsPressed = value;
        view.Invalidate();
    }

    private static void SetHover(UiElement? element, bool value)
    {
        for (; element != null; element = element.Parent) element.IsHovered = value;
    }
}
