namespace GuiShark;

/// <summary>Host-provided clipboard access; call on the host window thread.</summary>
public interface IUiClipboard
{
    string? GetText();
    void SetText(string text);
}
