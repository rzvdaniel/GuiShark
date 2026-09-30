using GuiShark.OpenGL;

namespace GuiShark.Balloon;

/// <summary>One HTML document attached to the existing host context through the public SDK.</summary>
internal sealed class HtmlOverlay : IDisposable
{
    public UiView View { get; }
    private readonly OpenGlUiRenderer renderer;
    public UiDocument Document => View.Document;

    public HtmlOverlay(string file, IAssetSource assets, FontBook fonts)
    {
        View = new(HtmlLoader.Load(assets.ReadText(file), assets), fonts);
        renderer = new(View, fonts);
    }

    public void Resize(int width, int height) => View.Resize(width, height);
    public void Render(int width, int height) => renderer.Render(width, height);
    public void Bind(string id, Action callback) => Document.GetElement(id).Clicked += _ => callback();
    public void Text(string id, string text) => Document.GetElement(id).Text = text;
    public void Dispose() { renderer.Dispose(); View.Dispose(); }
}
