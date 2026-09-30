namespace GuiShark.Demo;

/// <summary>Application behavior lives in the host, never in the parser or renderer.</summary>
internal sealed class DemoController
{
    private readonly UiDocument document;
    private int count;
    public bool DarkTheme { get; private set; }

    public DemoController(UiDocument document)
    {
        this.document = document;
        document.GetElement("increment").Clicked += _ => Increment();
        document.GetElement("theme").Clicked += _ => ToggleTheme();
        document.GetElement("reset").Clicked += _ => Reset();
    }

    private void Increment()
    {
        count++;
        document.GetElement("count").Text = count.ToString("00");
        document.GetElement("reset").Disabled = false;
        SetStatus($"Button clicked. Your C# callback has run {count} {(count == 1 ? "time" : "times")}.");
    }

    private void ToggleTheme()
    {
        DarkTheme = !DarkTheme;
        document.Root.SetClass("dark", DarkTheme);
        document.GetElement("theme").Text = DarkTheme ? "Switch to light" : "Switch to dark";
        document.GetElement("reset").Disabled = false;
        SetStatus(DarkTheme ? "Dark appearance applied. Same HTML, different CSS." : "Light appearance applied. Make yourself at home.");
    }

    private void Reset()
    {
        count = 0;
        DarkTheme = false;
        document.Root.SetClass("dark", false);
        document.GetElement("count").Text = "00";
        document.GetElement("theme").Text = "Switch to dark";
        document.GetElement("reset").Disabled = true;
        SetStatus("All fresh again. Try the buttons or use your keyboard.");
    }

    private void SetStatus(string text)
    {
        document.GetElement("status").Text = text;
        Console.WriteLine(text);
    }
}
