namespace GuiShark.ControlsDemo;

internal sealed record GalleryOptions(bool Capture, string Page, string? Dropdown, bool Scrolled, string? AssetsPath, string? Modal, string? Tooltip, bool SelectNotes, bool Compose, bool SelectLigature)
{
    public static GalleryOptions Parse(string[] args)
    {
        var capture = false;
        var page = "basics";
        string? dropdown = null, assets = null, modal = null, tooltip = null;
        var scrolled = false;
        var selectNotes = false;
        var compose = false;
        var selectLigature = false;
        foreach (var arg in args)
        {
            if (arg == "--capture") capture = true;
            else if (arg == "--compose") compose = true;
            else if (arg == "--select-ligature") selectLigature = true;
            else if (arg == "--select-notes") selectNotes = true;
            else if (arg == "--scroll") scrolled = true;
            else if (arg.StartsWith("--page=", StringComparison.Ordinal)) page = arg[7..];
            else if (arg.StartsWith("--dropdown=", StringComparison.Ordinal)) dropdown = arg[11..];
            else if (arg.StartsWith("--modal=", StringComparison.Ordinal)) modal = arg[8..];
            else if (arg.StartsWith("--tooltip=", StringComparison.Ordinal)) tooltip = arg[10..];
            else if (!arg.StartsWith("--", StringComparison.Ordinal) && assets == null) assets = arg;
            else throw new ArgumentException($"Unknown gallery argument: {arg}");
        }
        Validate(page);
        return new(capture, page, dropdown, scrolled, assets, modal, tooltip, selectNotes, compose, selectLigature);
    }

    private static void Validate(string page)
    {
        if (page is not ("basics" or "settings" or "lists" or "inventory" or "journal" or "multilingual")) throw new ArgumentException("Unknown gallery page.");
    }
}
