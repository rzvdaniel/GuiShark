namespace GuiShark.ControlsDemo;

/// <summary>Deterministic initial states for inspecting or capturing a gallery page.</summary>
internal static class GalleryPreview
{
    public static void Apply(UiView view, GalleryController controller, GalleryOptions options)
    {
        var document = view.Document;
        document.TabGroups[0].Select(document.GetElement($"tab-{options.Page}"));
        view.Update();
        if (options.Bidi) PreviewBidi(view);
        if (options.SelectLigature)
        {
            var sample = document.GetElement("ligature-text");
            view.Input.Focus(sample);
            sample.TextInput!.Select(2, 1);
            view.Update();
        }
        if (options.Compose)
        {
            view.Input.Focus(document.GetElement("multilingual-text"));
            view.Input.UpdateComposition("とうきょう", 3);
            view.Update();
        }
        if (options.SelectNotes)
        {
            var notes = document.GetElement("journal-notes");
            view.Input.Focus(notes);
            var start = notes.TextInput!.Value.IndexOf("The valley", StringComparison.Ordinal);
            var end = notes.TextInput.Value.IndexOf("Remember:", StringComparison.Ordinal) + "Remember:".Length;
            notes.TextInput.Select(start, end - start);
            view.Update();
        }
        if (options.Scrolled && options.Page == "journal")
        {
            var scroll = document.GetElement("journal-notes").Scroll;
            scroll.Offset = scroll.Maximum;
            view.Update();
        }
        else if (options.Scrolled)
        {
            document.GetElement("quest-scroll").Scroll.Offset = 380;
            document.GetElement("nested-inner").Scroll.Offset = 150;
            view.Update();
        }
        if (options.Modal == "password") document.GetElement("password-dialog").Dialog!.ShowModal();
        else if (options.Modal == "name")
        {
            document.GetElement("name-dialog").Dialog!.ShowModal();
            document.GetElement("character-name").TextInput!.SelectAll();
        }
        else if (options.Modal != null) controller.Inventory.Confirm(options.Modal);
        if (options.Dropdown != null) view.Popup.Open(document.GetElement(options.Dropdown));
        if (options.Tooltip != null)
        {
            view.Update();
            var bounds = document.GetElement(options.Tooltip).Bounds;
            view.Input.PointerMove(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        }
    }
    private static void PreviewBidi(UiView view)
    {
        var sample = view.Document.GetElement("arabic-chat");
        view.Input.Focus(sample);
        sample.TextInput!.Select(0, 10);
        view.Update();
        var page = view.Document.GetElement("page-multilingual");
        page.Scroll.Offset += sample.Bounds.Y - page.ContentBounds.Y - 80;
        view.Update();
    }
}
