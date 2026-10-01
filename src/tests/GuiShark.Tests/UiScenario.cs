using System.Globalization;

namespace GuiShark.Tests;

internal sealed class UiScenario : IDisposable
{
    public UiView View { get; }
    public UiDocument Document => View.Document;
    public UiInput Input => View.Input;

    public UiScenario(string body, string css = "", ITextMetrics? metrics = null)
    {
        var html = $"<html><head><style>{css}</style></head><body>{body}</body></html>";
        var document = HtmlLoader.Load(html, new TestAssets(), UiTheme.Neutral);
        View = new(document, metrics ?? new FixedTextMetrics());
        View.Resize(800, 400);
        View.Update();
    }

    public UiElement Element(string id) => Document.GetElement(id);
    public UiTextInput Edit(string id) => Element(id).TextInput ?? throw new InvalidOperationException("Expected a text input.");
    public void Focus(string id) => Input.Focus(Element(id));
    public void Press(UiKey key, bool shift = false, bool command = false)
    {
        Input.KeyDown(key, shift, command: command);
        Input.KeyUp(key);
    }
    public void Dispose() => View.Dispose();
}

internal sealed class FixedTextMetrics : ITextMetrics
{
    public float MeasureWidth(string text, float fontSize, bool bold) => StringInfo.ParseCombiningCharacters(text).Length * fontSize / 2;
}

internal sealed class TestAssets : IAssetSource
{
    public string ReadText(string relativePath) => throw new FileNotFoundException("Unexpected asset read.", relativePath);
    public Stream Open(string relativePath) => throw new FileNotFoundException("Unexpected asset read.", relativePath);
}

internal sealed class TestClipboard : IUiClipboard
{
    public string Text { get; set; } = "untouched";
    public int Reads { get; private set; }
    public int Writes { get; private set; }
    public string GetText() { Reads++; return Text; }
    public void SetText(string text) { Writes++; Text = text; }
}
