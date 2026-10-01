namespace GuiShark.ControlsDemo;

/// <summary>Local journal and chat behavior; no networking or persistent storage.</summary>
internal sealed class JournalController
{
    private readonly UiView view;
    private readonly Action<string> log;
    private readonly Queue<string> messages = new();
    private string saved;
    private UiDocument Document => view.Document;
    private UiTextInput Journal => Document.GetElement("journal-notes").TextInput!;
    private UiTextInput Composer => Document.GetElement("chat-compose").TextInput!;
    public JournalController(UiView view, Action<string> log)
    {
        this.view = view; this.log = log; saved = Journal.Value;
        Document.GetElement("journal-undo").Clicked += _ => { Journal.Undo(); FocusJournal(); };
        Document.GetElement("journal-redo").Clicked += _ => { Journal.Redo(); FocusJournal(); };
        Document.GetElement("journal-add").Clicked += _ =>
        {
            Journal.Select(Journal.Value.Length, 0);
            Journal.InsertText("\n\nField note: a golden lantern waits beside the river. Follow the winding trail through the ferns.");
            FocusJournal();
        };
        Document.GetElement("journal-save").Clicked += _ =>
        {
            saved = Journal.Value;
            Document.GetElement("journal-status").Text = "Saved in this demo session. Reloading resets the journal.";
            log("Journal saved in memory");
        };
        Document.GetElement("journal-restore").Clicked += _ =>
        {
            Journal.Value = saved;
            Document.GetElement("journal-status").Text = "Restored the saved note. Edit history starts fresh.";
            FocusJournal();
        };
        Journal.Changed += _ => Document.GetElement("journal-status").Text = "Unsaved edits / Ctrl or Command + Z to undo";
        Composer.Submitted += _ => Send();
        Document.GetElement("chat-send").Clicked += _ => Send();
        messages.Enqueue("KEEPER: Welcome to Lantern Valley. Leave a note for your fellow explorers.");
        messages.Enqueue("WILLOW: I found a lantern beside the old watchtower.\nThe view from the ridge is beautiful.");
        RefreshChat();
    }
    private void FocusJournal() => view.Input.Focus(Document.GetElement("journal-notes"));
    public void Update()
    {
        Document.GetElement("journal-undo").Disabled = !Journal.CanUndo;
        Document.GetElement("journal-redo").Disabled = !Journal.CanRedo;
        Document.GetElement("journal-count").Text = $"{Journal.Value.Length} / {Journal.MaximumLength} characters / {Journal.DisplayLines.Count} visual lines";
        Document.GetElement("chat-send").Disabled = string.IsNullOrWhiteSpace(Composer.Value);
    }
    private void Send()
    {
        if (string.IsNullOrWhiteSpace(Composer.Value)) return;
        messages.Enqueue($"YOU: {Composer.Value.Trim()}");
        while (messages.Count > 12) messages.Dequeue();
        Composer.Value = "";
        RefreshChat();
        view.Input.Focus(Document.GetElement("chat-compose"));
        log("Message added to local chat");
    }
    private void RefreshChat()
    {
        Document.GetElement("chat-history").TextInput!.Value = string.Join("\n\n", messages);
        view.Update();
        var scroll = Document.GetElement("chat-history").Scroll;
        scroll.Offset = scroll.Maximum;
    }
}
