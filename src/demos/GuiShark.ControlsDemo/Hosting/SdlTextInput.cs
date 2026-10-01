using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.SDL;

namespace GuiShark.ControlsDemo;

/// <summary>Native preedit/commit routing and candidate-window placement in logical window coordinates.</summary>
internal sealed unsafe class SdlTextInput(Sdl api, Func<UiView> currentView) : IUiClipboard
{
    private UiTextInput? focused;
    private UiComposition? composition;
    public void Synchronize()
    {
        var view = currentView();
        view.Update();
        var next = view.Input.Focused?.TextInput is { ReadOnly: false } edit ? edit : null;
        if (next != focused || composition != null && next?.Composition == null)
        {
            api.StopTextInput();
            if (next != null) api.StartTextInput();
            focused = next;
        }
        composition = next?.Composition;
        if (focused == null) return;
        var caret = focused.CaretBounds;
        var rect = new Rectangle<int>((int)caret.X, (int)caret.Y, Math.Max(1, (int)caret.Width), Math.Max(1, (int)caret.Height));
        api.SetTextInputRect(ref rect);
    }
    public void Commit(byte* text) => currentView().Input.TextInput(Marshal.PtrToStringUTF8((IntPtr)text) ?? "");
    public void Update(byte* pointer, int start, int length)
    {
        var text = Marshal.PtrToStringUTF8((IntPtr)pointer) ?? "";
        var characters = Math.Clamp(start, 0, text.Length);
        var selected = Math.Clamp(length, 0, text.Length - characters);
        var begin = Offset(text, characters);
        var end = Offset(text, characters + selected);
        currentView().Input.UpdateComposition(text, begin, end - begin);
    }
    private static int Offset(string text, int characters)
    {
        var offset = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            if (characters-- <= 0) break;
            offset += rune.Utf16SequenceLength;
        }
        return offset;
    }
    public string GetText()
    {
        var pointer = api.GetClipboardText();
        try { return Marshal.PtrToStringUTF8((IntPtr)pointer) ?? ""; }
        finally { api.Free(pointer); }
    }
    public void SetText(string text)
    {
        if (api.SetClipboardText(text) != 0) throw new InvalidOperationException(api.GetErrorS());
    }
}
