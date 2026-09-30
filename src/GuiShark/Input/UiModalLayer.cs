namespace GuiShark;

/// <summary>One modal per view; owns focus scope, backdrop and restoration.</summary>
public sealed class UiModalLayer
{
    private readonly UiView view;
    private UiElement? previousFocus;
    public UiDialog? Active { get; private set; }
    public bool IsOpen => Active != null;
    public UiColor BackdropColor { get; set; } = new(0, .015f, .035f, .72f);
    internal UiModalLayer(UiView view) => this.view = view;
    internal void Show(UiDialog dialog)
    {
        if (Active == dialog) return;
        if (Active != null) throw new InvalidOperationException("Close the current modal before opening another.");
        if (!view.Document.Root.DescendantsAndSelf().Contains(dialog.Element)) throw new ArgumentException("Dialog belongs to another view.");
        previousFocus = view.Input.Focused;
        view.Input.Cancel();
        Active = dialog;
        dialog.ReturnValue = "";
        view.Invalidate();
        view.Update();
        if (Active == dialog) view.Input.FocusFirst(dialog.Element);
    }
    internal void Close(UiDialog dialog, string result)
    {
        if (Active != dialog) return;
        var restore = previousFocus;
        previousFocus = null;
        Active = null;
        dialog.ReturnValue = result;
        view.Input.Cancel();
        view.Invalidate();
        view.Update();
        if (restore != null && view.Input.CanFocus(restore)) view.Input.Focus(restore);
        dialog.NotifyClosed();
    }
    public bool Contains(UiElement element)
    {
        if (Active == null) return true;
        for (var node = element; node != null; node = node.Parent)
            if (node == Active.Element) return true;
        return false;
    }
    internal void Clear() { Active = null; previousFocus = null; }

    internal void Validate()
    {
        if (Active == null) return;
        for (var node = Active.Element; node != null; node = node.Parent)
            if (node.Hidden || node.Disabled || node.Style.Hidden)
            {
                Close(Active, "cancel");
                return;
            }
    }
}
