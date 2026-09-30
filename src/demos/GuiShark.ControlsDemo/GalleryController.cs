namespace GuiShark.ControlsDemo;

/// <summary>Demo behavior and diagnostics, without window or graphics responsibilities.</summary>
internal sealed class GalleryController
{
    private readonly UiDocument document;
    private readonly Queue<string> events = new();
    private int clicks;
    public GalleryController(UiDocument document)
    {
        this.document = document;
        foreach (var element in document.Root.DescendantsAndSelf())
        {
            if (element.Control is { } control)
                control.Changed += _ => Changed(element);
        }
        document.GetElement("action").Clicked += _ => Log($"Launch clicked ({++clicks})");
        document.GetElement("skin-button").Clicked += _ => Log("Emerald action clicked");
        document.GetElement("reset").Clicked += _ => Reset();
        document.GetElement("lock").Control!.Changed += control =>
        {
            document.GetElement("volume").Disabled = control.Checked;
            document.GetElement("action").Disabled = control.Checked;
        };
        Refresh();
        Log("Gallery ready. Try a control or Tab to explore.");
    }
    public void Update(UiElement? focused) => document.GetElement("focus").Text = $"Keyboard focus: {focused?.Id ?? "none"}";
    private void Changed(UiElement element)
    {
        var control = element.Control!;
        if (control.Kind == UiControlKind.Progress) return;
        Refresh();
        Log(control.Kind is UiControlKind.Checkbox or UiControlKind.Radio
            ? $"{element.Id}: {(control.Checked ? "on" : "off")}" : $"{element.Id}: {control.Value:0.##}");
    }
    private void Refresh()
    {
        var volume = document.GetElement("volume").Control!.Value;
        document.GetElement("volume-value").Text = $"Master volume: {volume:0}%";
        document.GetElement("progress").Control!.Value = volume;
        document.GetElement("summary").Text = $"Volume {volume:0}% / subtitles {(document.GetElement("subtitles").Control!.Checked ? "on" : "off")}";
        document.GetElement("scale-value").Text = $"UI scale: {document.GetElement("scale").Control!.Value:0.00}x";
    }
    private void Reset()
    {
        document.GetElement("lock").Control!.Checked = false;
        document.GetElement("volume").Control!.Value = 65;
        document.GetElement("scale").Control!.Value = 1;
        document.GetElement("subtitles").Control!.Checked = true;
        document.GetElement("normal").Control!.Checked = true;
        Log("Settings reset");
    }
    private void Log(string message)
    {
        events.Enqueue(message);
        while (events.Count > 5) events.Dequeue();
        document.GetElement("events").Text = string.Join(" / ", events.Reverse());
    }
}
