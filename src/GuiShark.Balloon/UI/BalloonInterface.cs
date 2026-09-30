using GuiShark.OpenGL;

namespace GuiShark.Balloon;

internal sealed class BalloonInterface : IDisposable
{
    private readonly HtmlOverlay hud;
    private readonly Dictionary<GameMode, HtmlOverlay> panels;
    private GameMode mode;
    private float refresh;
    private HtmlOverlay Active => mode == GameMode.Flying ? hud : panels[mode];

    public BalloonInterface(string assetsPath, Action<GameMode> transition, Action restart, Action guide, Action quit)
    {
        var assets = new DirectoryAssetSource(assetsPath);
        // Font files are copied into this demo's output, even when HTML is loaded from the source tree.
        var fontsPath = Path.Combine(AppContext.BaseDirectory, "Assets/fonts");
        Fonts = new(Path.Combine(fontsPath, "Lato-Regular.ttf"), Path.Combine(fontsPath, "Lato-Bold.ttf"));
        hud = new("hud.html", assets, Fonts);
        panels = new()
        {
            [GameMode.Menu] = new("menu.html", assets, Fonts),
            [GameMode.Paused] = new("pause.html", assets, Fonts),
            [GameMode.Complete] = new("complete.html", assets, Fonts)
        };
        panels[GameMode.Menu].Bind("play", () => transition(GameMode.Flying));
        panels[GameMode.Menu].Bind("quit", quit);
        hud.Bind("pause", () => transition(GameMode.Paused));
        hud.Bind("guide", guide);
        panels[GameMode.Paused].Bind("resume", () => transition(GameMode.Flying));
        panels[GameMode.Paused].Bind("restart", restart);
        panels[GameMode.Paused].Bind("menu", () => transition(GameMode.Menu));
        panels[GameMode.Complete].Bind("again", restart);
        panels[GameMode.Complete].Bind("menu", () => transition(GameMode.Menu));
    }

    private FontBook Fonts { get; }

    public void SetMode(GameMode next)
    {
        foreach (var overlay in panels.Values.Append(hud)) overlay.View.Input.Cancel();
        mode = next;
        refresh = 0;
        Console.WriteLine($"Interface: {mode}");
    }

    public void Update(Expedition expedition, float dt)
    {
        refresh -= dt;
        if (refresh > 0) return;
        refresh = .2f;
        hud.Text("progress", $"{expedition.Collected:00} / 06");
        hud.Text("altitude", $"{expedition.Flight.Altitude * 10:0} m");
        hud.Text("distance", $"{expedition.Flight.DistanceTravelled * 10:0} m travelled");
        hud.Text("message", expedition.Message);
        hud.Text("course", expedition.Flight.Arrived ? "HOVERING" : "ON COURSE");
        panels[GameMode.Complete].Text("summary", $"6 lanterns discovered / {expedition.Time:0} seconds aloft");
    }

    public bool PointerMove(float x, float y) => Active.View.Input.PointerMove(x, y) || mode != GameMode.Flying;
    public bool PointerDown(float x, float y) => Active.View.Input.PointerDown(x, y) || mode != GameMode.Flying;
    public bool PointerUp(float x, float y) => Active.View.Input.PointerUp(x, y) || mode != GameMode.Flying;
    public bool KeyDown(UiKey key, bool shift, bool repeat) => Active.View.Input.KeyDown(key, shift, repeat);
    public bool KeyUp(UiKey key) => Active.View.Input.KeyUp(key);
    public void Cancel() => Active.View.Input.Cancel();
    public void Resize(int width, int height)
    {
        foreach (var overlay in panels.Values.Append(hud)) overlay.Resize(width, height);
    }
    public void Render(int width, int height)
    {
        if (mode != GameMode.Menu) hud.Render(width, height);
        if (mode != GameMode.Flying) panels[mode].Render(width, height);
    }
    public void Dispose()
    {
        foreach (var overlay in panels.Values.Append(hud)) overlay.Dispose();
        Fonts.Dispose();
    }
}
