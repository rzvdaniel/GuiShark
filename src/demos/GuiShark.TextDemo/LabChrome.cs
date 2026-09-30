using System.Globalization;
using System.Net;
using System.Text;
using GuiShark.OpenGL;

namespace GuiShark.TextDemo;

/// <summary>HTML controls and explanatory labels; sample rendering belongs to TextPane.</summary>
internal sealed class LabChrome : IDisposable
{
    public UiView View { get; }
    private readonly OpenGlUiRenderer renderer;
    private readonly LabSettings settings;
    private readonly Action changed;
    private static readonly string[] modes = ["Compare all", "Skia baseline", "Skia aligned", "FreeType", "MSDF"];

    public LabChrome(IAssetSource assets, FontBook fonts, LabSettings settings, int width, int height,
        IReadOnlyList<PaneLayout> layouts, IReadOnlyDictionary<int, string> errors, float nativeScale, Action changed)
    {
        this.settings = settings; this.changed = changed;
        var html = new StringBuilder("<html><head><link rel='stylesheet' href='chrome.css'></head><body>");
        html.Append("<h1 class='title'>GuiShark / Text Lab</h1><p class='subtitle'>One font. Three portable approaches. An original baseline. Inspect the pixels and choose for your game.</p>");
        html.Append("<div class='row' id='row1'>");
        for (var i = 0; i < modes.Length; i++) html.Append($"<button id='mode{i}'>{modes[i]}</button>");
        html.Append("<button id='smaller'>Size −</button><button id='larger'>Size +</button><button id='weight'></button></div>");
        html.Append("<div class='row' id='row2'><button id='density'></button><button id='snap'></button><button id='hint'></button><button id='filter'></button><button id='offset'></button></div>");
        html.Append("<div class='row' id='row3'><button id='background'></button><button id='color'></button><button id='shadow'></button><button id='zoom'></button><button id='reset'>Reset</button><span id='readout' style='padding: 6px; font-size: 13px; color: #9fb2c8;'></span></div>");
        foreach (var layout in layouts)
        {
            var c = layout.Card;
            // The host paints only the sample and zoom interiors over these HTML cards.
            html.Append($"<div class='card' style='{Rect(c)}'></div>");
            Label(html, "name", "", BackendCatalog.Name(layout.Index), c.X + 12, c.Y + 5, c.Width - 24, 27);
            Label(html, "detail", "", BackendCatalog.Detail(layout.Index), c.X + 12, c.Y + 33, c.Width - 24, 19);
            Label(html, "legend", "", "Sizes 10 · 12 · 14 · 18 · 24 · 36px  |  Inspect selected size above", c.X + 12, c.Bottom - 91, c.Width - 24, 17);
            Label(html, "stats", $"stats{layout.Index}", "", c.X + 12, c.Bottom - 23, c.Width - 24, 19);
            if (errors.TryGetValue(layout.Index, out var error))
                Label(html, "unavailable", "", "Unavailable: " + error, layout.Sample.X + 8, layout.Sample.Y + 10, layout.Sample.Width - 16, layout.Sample.Height - 20);
        }
        html.Append($"<p class='footer'>Nearest pixel magnifier · Simulated density × native framebuffer scale {nativeScale:0.##} · Simple Latin layout; no complex shaping · F12 capture · Esc close</p></body></html>");
        View = new(HtmlLoader.Load(html.ToString(), assets), fonts);
        View.Resize(width, height);
        renderer = new(View, fonts);
        BindControls(); Refresh();
    }

    private static string Rect(UiRect rect) => FormattableString.Invariant($"left: {rect.X}px; top: {rect.Y}px; width: {rect.Width}px; height: {rect.Height}px;");
    private static void Label(StringBuilder html, string type, string id, string text, float x, float y, float width, float height) =>
        html.Append($"<p class='{type}' id='{id}' style='{Rect(new(x, y, width, height))}'>{WebUtility.HtmlEncode(text)}</p>");

    private void Bind(string id, Action update) => View.Document.GetElement(id).Clicked += _ => { update(); Refresh(); changed(); };
    private void BindControls()
    {
        for (var i = 0; i < modes.Length; i++) { var mode = i; Bind($"mode{i}", () => settings.Mode = mode); }
        Bind("smaller", () => settings.Size = Math.Max(8, settings.Size - 2));
        Bind("larger", () => settings.Size = Math.Min(48, settings.Size + 2));
        Bind("weight", () => settings.Bold = !settings.Bold);
        Bind("density", () => settings.Density = settings.Density switch { 1 => 1.25f, 1.25f => 1.5f, 1.5f => 2, _ => 1 });
        Bind("snap", () => settings.Snap = !settings.Snap);
        Bind("hint", () => settings.Hinting = (TextHinting)(((int)settings.Hinting + 1) % 4));
        Bind("filter", () => settings.Sampling = settings.Sampling == TextSampling.Linear ? TextSampling.Nearest : TextSampling.Linear);
        Bind("offset", () => settings.Fractional = !settings.Fractional);
        Bind("background", () => settings.Background = (settings.Background + 1) % 3);
        Bind("color", () => settings.Color = (settings.Color + 1) % 3);
        Bind("shadow", () => settings.Shadow = !settings.Shadow);
        Bind("zoom", () => settings.Zoom = settings.Zoom == 8 ? 2 : settings.Zoom * 2);
        Bind("reset", () =>
        {
            settings.Size = 14; settings.Density = 1; settings.Snap = true; settings.Hinting = TextHinting.Normal;
            settings.Sampling = TextSampling.Linear; settings.Fractional = true; settings.Bold = settings.Shadow = false;
            settings.Background = settings.Color = 0; settings.Zoom = 2;
        });
    }
    private void Set(string id, string text) => View.Document.GetElement(id).Text = text;
    public void Refresh()
    {
        for (var i = 0; i < modes.Length; i++) View.Document.GetElement($"mode{i}").SetClass("selected", settings.Mode == i);
        Set("weight", settings.Bold ? "Weight: Bold" : "Weight: Regular");
        Set("density", $"Density: {settings.Density.ToString("0.##", CultureInfo.InvariantCulture)}×");
        Set("snap", $"Pixel snap: {(settings.Snap ? "On" : "Off")}");
        Set("hint", $"Hinting: {settings.Hinting}"); Set("filter", $"Filter: {settings.Sampling}");
        Set("offset", settings.Fractional ? "Origin: +½ device px" : "Origin: Integer");
        Set("background", "Background: " + settings.BackgroundName); Set("color", $"Color: {new[] { "Adaptive", "Gold", "Mint" }[settings.Color]}");
        Set("shadow", $"Shadow: {(settings.Shadow ? "On" : "Off")}"); Set("zoom", $"Magnifier: {settings.Zoom}×");
        Set("readout", $"Lato TTF · {settings.Size:0.#}px selected");
    }
    public void Statistics(TextPane pane)
    {
        var s = pane.Statistics;
        Set($"stats{pane.Index}", FormattableString.Invariant($"{s.DrawCalls} text draws · {s.Uploads} uploads · {s.Cache.Entries} cache entries · GPU {s.GpuBytes / 1048576f:0.0} MiB · CPU paint {pane.Milliseconds:0.00}ms"));
    }
    public void Render(int width, int height) => renderer.Render(width, height);
    public void Dispose() { renderer.Dispose(); View.Dispose(); }
}
