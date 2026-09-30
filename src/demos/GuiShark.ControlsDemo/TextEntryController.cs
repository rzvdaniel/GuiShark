namespace GuiShark.ControlsDemo;

/// <summary>Character naming behavior; the SDK owns editing and modal focus.</summary>
internal sealed class TextEntryController
{
    private readonly UiDocument document;
    private UiDialog Dialog => document.GetElement("name-dialog").Dialog!;
    private UiTextInput Name => document.GetElement("character-name").TextInput!;
    public TextEntryController(UiView view, Action<string> log)
    {
        document = view.Document;
        document.GetElement("rename-character").Clicked += _ => { Dialog.ShowModal(); Name.SelectAll(); };
        document.GetElement("cancel-name").Clicked += _ => Dialog.Close("cancel");
        document.GetElement("save-name").Clicked += _ => Save();
        Name.Changed += _ => Validate();
        Name.Submitted += _ => Save();
        Dialog.Closed += dialog => { if (dialog.ReturnValue == "save") log($"Character named: {Name.Value.Trim()}"); };
        Validate();
    }
    private void Validate()
    {
        var valid = Name.Value.Trim().Length > 0;
        document.GetElement("save-name").Disabled = !valid;
        document.GetElement("name-help").Text = valid ? $"{Name.Value.Length} / {Name.MaximumLength} characters" : "Please enter a name.";
    }
    private void Save()
    {
        if (Name.Value.Trim().Length == 0) return;
        document.GetElement("character-status").Text = $"Explorer: {Name.Value.Trim()}";
        Dialog.Close("save");
    }
}
