using Silk.NET.SDL;

namespace GuiShark.ControlsDemo;

internal static class SdlKeys
{
    public static bool Shift(Keymod modifiers) => (modifiers & Keymod.Shift) != 0;
    public static bool Command(Keymod modifiers) => (modifiers & (OperatingSystem.IsMacOS() ? Keymod.Gui : Keymod.Ctrl)) != 0;
    public static bool Words(Keymod modifiers) => (modifiers & (OperatingSystem.IsMacOS() ? Keymod.Alt : Keymod.Ctrl)) != 0;
    public static UiKey? Map(int key) => (KeyCode)key switch
    {
        KeyCode.KTab => UiKey.Tab, KeyCode.KReturn or KeyCode.KKPEnter => UiKey.Enter,
        KeyCode.KSpace => UiKey.Space, KeyCode.KEscape => UiKey.Escape,
        KeyCode.KLeft => UiKey.Left, KeyCode.KRight => UiKey.Right, KeyCode.KUp => UiKey.Up, KeyCode.KDown => UiKey.Down,
        KeyCode.KHome => UiKey.Home, KeyCode.KEnd => UiKey.End,
        KeyCode.KBackspace => UiKey.Backspace, KeyCode.KDelete => UiKey.Delete,
        KeyCode.KZ => UiKey.Z, KeyCode.KY => UiKey.Y, KeyCode.KA => UiKey.A, KeyCode.KC => UiKey.C, KeyCode.KX => UiKey.X, KeyCode.KV => UiKey.V,
        KeyCode.KPageup => UiKey.PageUp, KeyCode.KPagedown => UiKey.PageDown, _ => null
    };
}
