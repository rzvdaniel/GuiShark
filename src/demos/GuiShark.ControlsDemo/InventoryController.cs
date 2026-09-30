using GuiShark;

namespace GuiShark.ControlsDemo;

/// <summary>Inventory application logic; dialogs and tooltips are SDK services.</summary>
internal sealed class InventoryController
{
    private sealed record Item(string Id, string Name, string Rarity, string Description, string Stats);
    private static readonly Item[] Items =
    [
        new("rune", "Moonstone rune", "RARE / TRINKET", "A cool blue stone carved with the marks of the valley's keepers.", "+12 spirit / lantern range +8%"),
        new("leaf", "Elderwood charm", "UNCOMMON / TALISMAN", "An evergreen leaf sealed in amber. It rustles even when the air is still.", "+6 vitality / wind resistance +5%"),
        new("flask", "Dewdrop flask", "COMMON / SUPPLIES", "Fresh water gathered beneath the guardian oak before sunrise.", "Restores 30 energy / 3 charges"),
        new("compass", "Wayfinder compass", "RARE / TOOL", "Its needle points toward the next lantern rather than north.", "+10 exploration / reveals nearby trails"),
        new("feather", "Silverwing feather", "EPIC / RELIC", "A feather left behind by a visitor from above the cloud line.", "+18 agility / drifting speed +10%"),
        new("coin", "Keeper's token", "COMMON / KEEPSAKE", "A brass token passed between those who watch over the valley.", "+2 luck / accepted at woodland camps")
    ];
    private readonly UiView view;
    private readonly Action<string> log;
    private readonly HashSet<string> discarded = [];
    private Item selected = Items[0];
    private string pending = "";
    private UiDocument Document => view.Document;
    private UiDialog Confirmation => Document.GetElement("item-dialog").Dialog!;
    public InventoryController(UiView view, Action<string> log)
    {
        this.view = view;
        this.log = log;
        foreach (var item in Items) Document.GetElement($"item-{item.Id}").Clicked += _ => Select(item);
        Document.GetElement("equip-item").Clicked += _ => Confirm("equip");
        Document.GetElement("discard-item").Clicked += _ => Confirm("discard");
        Document.GetElement("confirm-item").Clicked += _ => Confirmation.Close(pending);
        Document.GetElement("cancel-item").Clicked += _ => Confirmation.Close("cancel");
        Confirmation.Closed += Complete;
        Document.GetElement("reset-inventory").Clicked += _ => Reset();
        Document.GetElement("inventory-search").TextInput!.Changed += _ => Filter();
        Select(selected);
    }
    public void Confirm(string action)
    {
        if (action is not ("equip" or "discard")) throw new ArgumentException("Action must be equip or discard.", nameof(action));
        if (discarded.Contains(selected.Id)) return;
        pending = action;
        Document.GetElement("dialog-title").Text = $"{(action == "equip" ? "Equip" : "Discard")} {selected.Name}?";
        Document.GetElement("dialog-copy").Text = action == "equip"
            ? "This item will become your active equipment. The valley can wait while you decide."
            : "This removes the item from this demo inventory. Reset inventory can bring it back.";
        Document.GetElement("confirm-item").Text = action == "equip" ? "Equip item" : "Discard item";
        Document.GetElement("confirm-item").SetClass("danger", action == "discard");
        Document.GetElement("cancel-item").SetClass("danger", false);
        view.Input.Focus(Document.GetElement(action == "equip" ? "equip-item" : "discard-item"));
        Confirmation.ShowModal();
        log($"Confirmation opened: {action}");
    }
    private void Select(Item item)
    {
        selected = item;
        foreach (var entry in Items) Document.GetElement($"item-{entry.Id}").SetClass("chosen", entry == item);
        Document.GetElement("item-name").Text = item.Name;
        Document.GetElement("item-rarity").Text = item.Rarity;
        Document.GetElement("item-description").Text = item.Description;
        Document.GetElement("item-stats").Text = item.Stats;
        Document.GetElement("equip-item").Disabled = discarded.Contains(item.Id);
        Document.GetElement("discard-item").Disabled = discarded.Contains(item.Id);
    }
    private void Complete(UiDialog dialog)
    {
        if (dialog.ReturnValue == "equip")
        {
            foreach (var item in Items) Document.GetElement($"item-{item.Id}").SetClass("equipped", item == selected);
            Document.GetElement("equipment-status").Text = $"Equipped: {selected.Name} / {Document.GetElement("equipment-slot").Select!.Value}";
        }
        else if (dialog.ReturnValue == "discard")
        {
            discarded.Add(selected.Id);
            var slot = Document.GetElement($"item-{selected.Id}");
            slot.Disabled = true;
            slot.SetClass("equipped", false);
            Document.GetElement("equipment-status").Text = $"Discarded: {selected.Name}";
            Select(selected);
        }
        log($"Dialog closed: {dialog.ReturnValue}");
    }
    private void Filter()
    {
        var query = Document.GetElement("inventory-search").TextInput!.Value.Trim();
        var matches = 0;
        foreach (var item in Items)
        {
            var visible = item.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || item.Rarity.Contains(query, StringComparison.OrdinalIgnoreCase);
            Document.GetElement($"item-{item.Id}").Hidden = !visible;
            if (visible) matches++;
        }
        Document.GetElement("search-results").Text = $"{matches} of {Items.Length} items / name or rarity";
    }
    private void Reset()
    {
        discarded.Clear();
        Document.GetElement("inventory-search").TextInput!.Value = "";
        Filter();
        foreach (var item in Items)
        {
            var slot = Document.GetElement($"item-{item.Id}");
            slot.Disabled = false;
            slot.SetClass("equipped", false);
        }
        Document.GetElement("equipment-status").Text = "No item equipped.";
        Select(Items[0]);
        log("Inventory restored");
    }
}
