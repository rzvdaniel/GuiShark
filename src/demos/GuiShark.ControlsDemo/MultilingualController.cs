namespace GuiShark.ControlsDemo;

/// <summary>A portable composition exercise alongside genuine OS input in the SDL host.</summary>
internal sealed class MultilingualController
{
    private readonly UiView view;
    private readonly Queue<string> events = new();
    private readonly UiElement field;
    public MultilingualController(UiView view)
    {
        this.view = view;
        field = view.Document.GetElement("multilingual-text");
        foreach (var id in new[] { "multilingual-text", "multilingual-notes", "indic-text", "fallback-text" })
        {
            var element = view.Document.GetElement(id);
            element.TextInput!.CompositionChanged += edit => Log(edit.Composition is { } state
                ? $"{id}: preedit {state.Text.Length} UTF-16 units, cursor {state.SelectionStart}"
                : $"{id}: preedit ended");
            element.TextInput.Changed += edit => Log($"{id}: committed {edit.Value.Length} UTF-16 units");
        }
        Bind("preedit-start", () => Preview("とうきょう"));
        Bind("preedit-convert", () => Preview("東京"));
        Bind("preedit-commit", () => { view.Input.Focus(field); view.Input.TextInput("東京"); });
        Bind("preedit-cancel", () => field.TextInput!.CancelComposition());
        Bind("ligature-select", () => SelectLigature(false));
        Bind("ligature-reset", () => SelectLigature(true));
    }
    private void SelectLigature(bool reset)
    {
        var sample = view.Document.GetElement("ligature-text");
        view.Input.Focus(sample);
        if (reset) sample.TextInput!.Value = "office affine efficient";
        sample.TextInput!.Select(2, 1);
    }
    private void Preview(string text)
    {
        view.Input.Focus(field);
        field.TextInput!.SelectAll();
        view.Input.UpdateComposition(text, text.Length);
    }
    private void Bind(string id, Action action) => view.Document.GetElement(id).Clicked += _ => action();
    private void Log(string text)
    {
        events.Enqueue(text);
        while (events.Count > 4) events.Dequeue();
        view.Document.GetElement("composition-events").Text = string.Join(" / ", events);
    }
}
