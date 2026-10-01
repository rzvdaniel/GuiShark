using Silk.NET.SDL;

namespace GuiShark.ControlsDemo;

internal sealed unsafe class SdlInput(SdlWindow host, Func<UiView> currentView, Action<KeyCode> shortcut)
{
    public SdlTextInput Text { get; } = new(host.Api, currentView);
    public void Poll()
    {
        Event input = default;
        while (host.Api.PollEvent(ref input) != 0) Dispatch(input);
        Text.Synchronize();
    }
    private void Dispatch(Event input)
    {
        var view = currentView();
        switch ((EventType)input.Type)
        {
            case EventType.Quit: host.Running = false; break;
            case EventType.Windowevent: Window(input.Window); break;
            case EventType.Keydown: KeyDown(input.Key); break;
            case EventType.Keyup:
                if (SdlKeys.Map(input.Key.Keysym.Sym) is { } key) view.Input.KeyUp(key);
                break;
            case EventType.Textinput: Text.Commit(input.Text.Text); break;
            case EventType.Textediting: Text.Update(input.Edit.Text, input.Edit.Start, input.Edit.Length); break;
            case EventType.TexteditingExt: Extended(input.EditExt); break;
            case EventType.Mousemotion: view.Input.PointerMove(input.Motion.X, input.Motion.Y); break;
            case EventType.Mousebuttondown: Pointer(input.Button, true); break;
            case EventType.Mousebuttonup: Pointer(input.Button, false); break;
            case EventType.Mousewheel: Wheel(input.Wheel); break;
        }
        Text.Synchronize();
    }
    private void Window(WindowEvent input)
    {
        var view = currentView();
        if ((WindowEventID)input.Event == WindowEventID.FocusLost) view.Input.Cancel();
        if ((WindowEventID)input.Event == WindowEventID.Close) host.Running = false;
        var size = host.ClientSize;
        view.Resize(size.X, size.Y);
    }
    private void Extended(TextEditingExtEvent input)
    {
        try { Text.Update(input.Text, input.Start, input.Length); }
        finally { host.Api.Free(input.Text); }
    }
    private void KeyDown(KeyboardEvent input)
    {
        var view = currentView();
        var modifiers = (Keymod)input.Keysym.Mod;
        if (SdlKeys.Map(input.Keysym.Sym) is { } key && view.Input.KeyDown(key, SdlKeys.Shift(modifiers), input.Repeat != 0,
            SdlKeys.Command(modifiers), SdlKeys.Words(modifiers))) return;
        var code = (KeyCode)input.Keysym.Sym;
        if (view.Input.WantsKeyboard && code is not (KeyCode.KF5 or KeyCode.KF12)) return;
        if (input.Repeat == 0) shortcut(code);
    }
    private void Pointer(MouseButtonEvent input, bool down)
    {
        if (input.Button != Sdl.ButtonLeft) return;
        if (down) currentView().Input.PointerDown(input.X, input.Y, SdlKeys.Shift(host.Api.GetModState()));
        else currentView().Input.PointerUp(input.X, input.Y);
    }
    private void Wheel(MouseWheelEvent input)
    {
        int x = 0, y = 0;
        host.Api.GetMouseState(ref x, ref y);
        var delta = input.Direction == 1 ? -input.PreciseY : input.PreciseY;
        currentView().Input.PointerWheel(x, y, delta);
    }
}
