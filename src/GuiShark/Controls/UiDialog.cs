namespace GuiShark;

/// <summary>View-owned modal dialog with explicit result and cancellation events.</summary>
public sealed class UiDialog
{
    private UiView? view;
    public UiElement Element { get; }
    public bool IsOpen => view?.Modal.Active == this;
    public bool CloseOnEscape { get; set; } = true;
    public string ReturnValue { get; internal set; } = "";
    internal UiDialog(UiElement element) => Element = element;
    internal void Attach(UiView? owner) => view = owner;
    public void ShowModal() => (view ?? throw new InvalidOperationException("Create a UiView before opening dialogs.")).Modal.Show(this);
    public void Close(string result = "") => view?.Modal.Close(this, result);
    public event Action<UiDialog>? Closed;
    public event Action<UiDialog>? Cancelled;
    internal void RequestCancel()
    {
        if (!CloseOnEscape) return;
        Close("cancel");
        Cancelled?.Invoke(this);
    }
    internal void NotifyClosed() => Closed?.Invoke(this);
}
