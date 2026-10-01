namespace GuiShark;

/// <summary>Methods return true when the UI consumes input. Coordinates are logical UI pixels.</summary>
public sealed class UiInput
{
    private readonly UiView view;
    private readonly UiFocusNavigation navigation;
    private UiElement? hovered;
    private UiElement? pressed;
    private UiElement? keyboardPressed;
    private UiKey? activationKey;
    private readonly ScrollDrag scrollDrag = new();
    private readonly HashSet<UiKey> consumedKeys = [];
    private UiElement? popupPressed;
    private bool swallowPointerUp;
    private readonly TextClickTracker textClicks = new();
    private bool wordDrag;
    public IUiClipboard? Clipboard { get; set; }
    public UiElement? Focused { get; private set; }
    public bool HasPointerCapture => pressed != null || scrollDrag.Active || view.Popup.IsOpen || swallowPointerUp || view.Modal.IsOpen;
    public bool WantsKeyboard => Focused != null || view.Popup.IsOpen || view.Modal.IsOpen;

    internal UiInput(UiView view)
    {
        this.view = view;
        navigation = new(view, () => Focused, SetFocus);
    }

    public bool PointerMove(float x, float y)
    {
        textClicks.Move(x, y);
        view.Update();
        if (view.Popup.IsOpen) { view.Popup.Hover(x, y); return true; }
        if (scrollDrag.Active) { scrollDrag.Move(y); return true; }
        var hit = Hit(x, y);
        view.Tooltips.Track(pressed == null ? hit : null, x, y);
        if (hovered != hit)
        {
            SetHover(hovered, false);
            hovered = hit;
            SetHover(hovered, true);
            view.Invalidate();
        }
        if (pressed != null)
        {
            if (wordDrag) pressed.TextInput?.DragPointerWord(x, y, view.TextMetrics);
            else pressed.TextInput?.MovePointer(x, y, view.TextMetrics, extend: true);
            SetPressed(pressed, HitTester.Interactive(hit) == pressed);
            if (HitTester.CanActivate(pressed)) ControlInteraction.Drag(pressed, x);
        }
        return hit != null || HasPointerCapture;
    }

    public bool PointerDown(float x, float y, bool shift = false)
    {
        PreparePointerPress();
        view.Tooltips.Hide();
        wordDrag = false;
        if (view.Popup.IsOpen)
        {
            textClicks.Reset();
            popupPressed = view.Popup.Hit(x, y);
            swallowPointerUp = true;
            if (popupPressed == null) view.Popup.Close();
            return true;
        }
        if (pressed != null) SetPressed(pressed, false);
        pressed = null;
        PointerMove(x, y);
        view.Tooltips.Hide();
        if (scrollDrag.Begin(hovered, x, y)) { textClicks.Reset(); return true; }
        var button = HitTester.Interactive(hovered);
        var doubleClick = textClicks.Down(button != null && CanFocus(button) ? button : null, x, y);
        if (button != null && CanFocus(button) && HitTester.CanActivate(button)) SetFocus(button);
        else if (!view.Modal.IsOpen) SetFocus(null);
        if (button != null && Focused == button) BeginPress(x, y, shift, doubleClick);
        return hovered != null || view.Modal.IsOpen;
    }

    private void PreparePointerPress()
    {
        Focused?.TextInput?.CancelComposition();
        view.Update();
    }

    private void BeginPress(float x, float y, bool shift, bool doubleClick)
    {
        pressed = Focused!;
        SetPressed(pressed, true);
        wordDrag = doubleClick && !shift && pressed.TextInput != null;
        if (wordDrag) pressed.TextInput!.SelectPointerWord(x, y, view.TextMetrics);
        else pressed.TextInput?.MovePointer(x, y, view.TextMetrics, shift);
        ControlInteraction.Drag(pressed, x);
    }

    public bool PointerUp(float x, float y)
    {
        view.Update();
        if (swallowPointerUp)
        {
            swallowPointerUp = false;
            if (popupPressed != null && view.Popup.Hit(x, y) == popupPressed) view.Popup.Commit(popupPressed);
            popupPressed = null;
            return true;
        }
        if (scrollDrag.Active) { scrollDrag.Move(y); scrollDrag.Cancel(); return true; }
        PointerMove(x, y);
        var target = pressed;
        pressed = null;
        wordDrag = false;
        if (target == null) return hovered != null || view.Modal.IsOpen;
        SetPressed(target, false);
        if (target.TextInput == null && HitTester.Interactive(hovered) == target && HitTester.CanActivate(target)) Activate(target);
        return true;
    }

    public bool KeyDown(UiKey key, bool shift = false, bool repeat = false, bool command = false, bool? wordNavigation = null)
    {
        view.Update();
        view.Tooltips.Hide();
        textClicks.Reset();
        if (HandleCompositionKey(key)) return true;
        var scoped = HandleScopeKey(key, shift, repeat);
        if (scoped.HasValue) return scoped.Value;
        if (Focused == null) return view.Modal.IsOpen;
        if (Focused.TextInput is { } input) { var handled = input.Key(key, shift, command, Clipboard, view.TextMetrics, wordNavigation ?? command); consumedKeys.Add(key); return handled; }
        if (navigation.AdvanceTab(key)) return true;
        if (navigation.AdvanceRadio(key)) return true;
        if (navigation.ScrollPage(key)) return true;
        if (Focused.Select != null && key is UiKey.Up or UiKey.Down) { consumedKeys.Add(key); view.Popup.Open(Focused); return true; }
        if (ControlInteraction.Key(Focused, key)) return true;
        return HandleActivation(key, repeat);
    }

    private bool HandleCompositionKey(UiKey key)
    {
        if (Focused?.TextInput is not { Composition: not null } input) return false;
        if (key == UiKey.Tab) { input.CancelComposition(); return false; }
        if (key == UiKey.Escape) input.CancelComposition();
        consumedKeys.Add(key);
        return true;
    }

    /// <summary>Forward host IME preedit updates. Offsets are UTF-16; empty text cancels preedit.</summary>
    public bool UpdateComposition(string text, int selectionStart = 0, int selectionLength = 0)
    {
        ArgumentNullException.ThrowIfNull(text);
        view.Update();
        if (Focused?.TextInput is not { ReadOnly: false } input || !CanFocus(Focused)) return view.Modal.IsOpen;
        input.UpdateComposition(text, selectionStart, selectionLength);
        return true;
    }

    private bool? HandleScopeKey(UiKey key, bool shift, bool repeat)
    {
        if (key == UiKey.Escape && repeat && consumedKeys.Contains(key)) return true;
        if (key == UiKey.Tab) { view.Popup.Close(); if (!repeat) navigation.AdvanceFocus(shift); return Focused != null || view.Modal.IsOpen; }
        if (view.Popup.IsOpen) { consumedKeys.Add(key); return view.Popup.Key(key); }
        if (key == UiKey.Escape && view.Modal.Active is { } dialog)
        {
            dialog.RequestCancel();
            consumedKeys.Add(key);
            return true;
        }
        if (key == UiKey.Escape) { var consumed = Focused != null || HasPointerCapture; Cancel(); return consumed; }
        return null;
    }

    private bool HandleActivation(UiKey key, bool repeat)
    {
        if (key is not (UiKey.Enter or UiKey.Space)) return view.Modal.IsOpen;
        if (!repeat && keyboardPressed == null && HitTester.CanActivate(Focused!))
        {
            activationKey = key;
            keyboardPressed = Focused!;
            SetPressed(keyboardPressed, true);
        }
        return true;
    }

    /// <summary>Forward committed Unicode text from the host text event, separately from physical keys.</summary>
    public bool TextInput(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        view.Update();
        if (Focused?.TextInput is { } input && CanFocus(Focused)) { input.Insert(text); return true; }
        return view.Modal.IsOpen;
    }

    public bool KeyUp(UiKey key)
    {
        view.Update();
        if (consumedKeys.Remove(key)) return true;
        if (activationKey != key || keyboardPressed == null) return view.Modal.IsOpen;
        var target = keyboardPressed;
        keyboardPressed = null;
        activationKey = null;
        SetPressed(target, false);
        if (target == Focused && HitTester.CanActivate(target)) Activate(target);
        return true;
    }

    public void Cancel()
    {
        Focused?.TextInput?.CancelComposition();
        if (pressed != null) SetPressed(pressed, false);
        if (keyboardPressed != null) SetPressed(keyboardPressed, false);
        pressed = keyboardPressed = null;
        wordDrag = false;
        textClicks.Reset();
        scrollDrag.Cancel();
        view.Tooltips.Hide();
        view.Popup.Close();
        popupPressed = null;
        swallowPointerUp = false;
        consumedKeys.Clear();
        activationKey = null;
        SetHover(hovered, false);
        hovered = null;
        SetFocus(null);
        view.Invalidate();
    }

    internal void ValidateTargets()
    {
        if (Focused?.TextInput is { ReadOnly: true } input) input.CancelComposition();
        if (Focused != null && !CanFocus(Focused)) Cancel();
        if (view.Modal.Active is { } dialog && Focused == null) FocusFirst(dialog.Element);
        if (scrollDrag.Active && !scrollDrag.IsVisible) scrollDrag.Cancel();
    }

    public bool PointerWheel(float x, float y, float delta)
    {
        if (!float.IsFinite(delta)) throw new ArgumentOutOfRangeException(nameof(delta));
        view.Update();
        view.Tooltips.Hide();
        textClicks.Reset();
        if (view.Popup.IsOpen) { view.Popup.Wheel(delta); return true; }
        var hit = Hit(x, y);
        for (var node = hit; node != null; node = node.Parent)
        {
            var before = node.Scroll.Offset;
            if (node.Style.ScrollY) node.Scroll.Offset -= delta * 40;
            if (before != node.Scroll.Offset) return true;
        }
        return hit != null || view.Modal.IsOpen;
    }

    /// <summary>Move keyboard focus within the current modal scope, or clear it.</summary>
    public void Focus(UiElement? element)
    {
        view.Update();
        if (element != null && (!view.Document.Root.DescendantsAndSelf().Contains(element) || !CanFocus(element)))
            throw new ArgumentException("Element is not focusable in this view's active scope.", nameof(element));
        SetFocus(element);
    }

    internal bool CanFocus(UiElement element)
    {
        if (!HitTester.CanFocus(element) || !view.Modal.Contains(element)) return false;
        for (var node = element; node != null; node = node.Parent)
            if (node.Role == "tooltip") return false;
        return true;
    }

    internal void FocusFirst(UiElement root)
    {
        var controls = root.DescendantsAndSelf().Where(e => CanFocus(e) && (e.TabGroup == null || e.IsSelected)).ToArray();
        SetFocus(controls.FirstOrDefault(e => e.AutoFocus) ?? controls.FirstOrDefault());
    }

    private UiElement? Hit(float x, float y) => view.Modal.Active is { } dialog
        ? HitTester.Hit(dialog.Element, x, y, overlay: true) : HitTester.Hit(view.Document.Root, x, y);

    private void Activate(UiElement target)
    {
        if (target.Select != null) view.Popup.Open(target);
        else target.Activate();
    }

    private void SetFocus(UiElement? element)
    {
        if (Focused == element) return;
        if (keyboardPressed != null) SetPressed(keyboardPressed, false);
        keyboardPressed = null;
        activationKey = null;
        if (Focused != null) Focused.IsFocused = false;
        Focused?.TextInput?.CancelComposition();
        Focused = element;
        if (Focused != null) { Focused.IsFocused = true; Focused.TextInput?.RevealCaret(); }
        view.Invalidate();
        if (Focused != null) navigation.Reveal(Focused);
    }

    private void SetPressed(UiElement element, bool value)
    {
        if (element.IsPressed == value) return;
        element.IsPressed = value;
        view.Invalidate();
    }

    private static void SetHover(UiElement? element, bool value)
    {
        for (; element != null; element = element.Parent)
        {
            element.IsHovered = value;
            if (element.LabelTarget is { } target) target.IsHovered = value;
        }
    }
}
