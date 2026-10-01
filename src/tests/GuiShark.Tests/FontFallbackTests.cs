using GuiShark.OpenGL;
using Xunit;

namespace GuiShark.Tests;

public sealed class FontFallbackTests
{
    private const string Declarations = """
        @font-face { font-family: Lato; src: url('fonts/Lato-Regular.ttf'); }
        @font-face { font-family: Lato; src: url('fonts/Lato-Bold.ttf'); font-weight: bold; }
        @font-face { font-family: NotoJP; src: url('fonts/NotoSansJP.ttf'); }
        @font-face { font-family: NotoIndic; src: url('fonts/NotoSansDevanagari.ttf'); }
        @font-face { font-family: NotoArabic; src: url('fonts/NotoSansArabic.ttf'); }
        """;

    [Theory]
    [InlineData("東京", "NotoJP", false)]
    [InlineData("क्षि", "NotoIndic", false)]
    [InlineData("مرحبا", "NotoArabic", false)]
    [InlineData("東京", "NotoJP", true)]
    [InlineData("क्षि", "NotoIndic", true)]
    public void MissingGraphemesUseALoadedFont(string text, string family, bool bold)
    {
        using var fonts = new FontBook(Document(Declarations), "Lato");
        using var backend = new SkiaTextBackend(fonts);
        Assert.Equal(backend.MeasureWidth(text, 24, false, family), backend.MeasureWidth(text, 24, bold, "Lato"), 3);
    }

    [Theory]
    [InlineData("東京", "NotoJP", true)]
    [InlineData("क्षि", "NotoIndic", true)]
    [InlineData("東京", "NotoJP", false)]
    [InlineData("क्षि", "NotoIndic", false)]
    public void MixedRunsHaveConsistentWidthCaretAndSelection(string text, string family, bool shaping)
    {
        using var fonts = new FontBook(Document(Declarations), "Lato");
        using var backend = new SkiaTextBackend(fonts, shaping);
        backend.Configure(1.5f, new());
        var value = "A" + text + "B";
        var latin = backend.MeasureWidth("A", 24, false, "Lato");
        var middle = backend.MeasureWidth(text, 24, false, family);
        var total = latin + middle + backend.MeasureWidth("B", 24, false, "Lato");
        Assert.Equal(total, backend.MeasureWidth(value, 24, false, "Lato"), 3);
        var map = backend.CreateCaretMap(value, 24, false, "Lato");
        Assert.Equal(latin, map.X(1), 3);
        Assert.Equal(latin + middle, map.X(text.Length + 1), 3);
        Assert.Equal(total, map.X(value.Length), 3);
        using var ui = new UiScenario($"<input id='text' value='{value}'>", "input { font-size: 24px; font-family: Lato; }", backend);
        ui.Edit("text").Select(1, text.Length);
        ui.View.Update();
        Assert.Equal(middle, ui.Edit("text").SelectionBounds.Width, 3);
    }

    [Fact]
    public void ExplicitStackPriorityAndUnavailableNamesAreRespected()
    {
        using var fonts = new FontBook(Document(Declarations), "Lato");
        using var backend = new SkiaTextBackend(fonts);
        Assert.Equal(backend.MeasureWidth("ABC", 24, false, "NotoJP"),
            backend.MeasureWidth("ABC", 24, false, "Missing, NotoJP, Lato"), 3);
        Assert.Throws<InvalidOperationException>(() => backend.MeasureWidth("ABC", 24, false, "Missing"));
    }

    [Fact]
    public void LoadingANewFallbackInvalidatesCachedCaretMaps()
    {
        using var fonts = new FontBook(Document(Declarations[..Declarations.IndexOf("@font-face { font-family: NotoJP", StringComparison.Ordinal)]), "Lato");
        using var backend = new SkiaTextBackend(fonts);
        var before = backend.CreateCaretMap("東京", 24, false, "Lato");
        fonts.Load(Document(Declarations));
        var after = backend.CreateCaretMap("東京", 24, false, "Lato");
        Assert.NotSame(before, after);
        Assert.Equal(backend.MeasureWidth("東京", 24, false, "NotoJP"), after.X(2), 3);
    }

    [Fact]
    public void VariationSelectorDoesNotPreventCjkFallback()
    {
        using var fonts = new FontBook(Document(Declarations), "Lato");
        using var backend = new SkiaTextBackend(fonts);
        Assert.Equal(backend.MeasureWidth("東\ufe0f", 24, false, "NotoJP"),
            backend.MeasureWidth("東\ufe0f", 24, false, "Lato"), 3);
    }

    [Theory]
    [InlineData("Lato, 'Noto JP'")]
    [InlineData("'Lato', \"Noto JP\"")]
    public void CssKeepsAnOrderedFamilyStack(string value)
    {
        using var ui = new UiScenario("<input id='text'>", $"input {{ font-family: {value}; }}");
        Assert.Equal("Lato, Noto JP", ui.Element("text").Style.FontFamily);
    }

    [Theory]
    [InlineData("Lato,")]
    [InlineData("'Lato")]
    [InlineData("Lato,, NotoJP")]
    public void MalformedStacksAreRejected(string value) => Assert.Throws<FormatException>(() => FontFamilyList.Parse(value));

    private static UiDocument Document(string css) => HtmlLoader.Load($"<html><head><style>{css}</style></head><body></body></html>",
        new DirectoryAssetSource(AppContext.BaseDirectory), UiTheme.Neutral);

    [Fact]
    public void LoadingFontsReplacesCachedRasterImages()
    {
        using var fonts = new FontBook(Document(Declarations[..Declarations.IndexOf("@font-face { font-family: NotoJP", StringComparison.Ordinal)]), "Lato");
        using var backend = new SkiaTextBackend(fonts);
        using var ui = new UiScenario("<p id='text'>東京</p>", "p { font-size: 24px; font-family: Lato; }", backend);
        var canvas = new ImageCanvas();
        backend.Draw(new(ui.Element("text"), 1, 1, 1), canvas);
        var before = canvas.Last;
        fonts.Load(Document(Declarations));
        ui.View.SetTextMetrics(backend);
        ui.View.Update();
        backend.Draw(new(ui.Element("text"), 1, 1, 1), canvas);
        Assert.NotSame(before, canvas.Last);
        Assert.Contains(canvas.Last!.Pixels, value => value != 0);
    }

    [Fact]
    public void FontBookMeasurementAlsoUsesLoadedFallbacks()
    {
        using var fonts = new FontBook(Document(Declarations), "Lato");
        using var backend = new SkiaTextBackend(fonts, shaping: false);
        Assert.Equal(backend.MeasureWidth("A東京B", 24, false, "Lato"), fonts.MeasureWidth("A東京B", 24, false, "Lato"), 3);
    }

    private sealed class ImageCanvas : ITextCanvas
    {
        public TextImage? Last { get; private set; }
        public void Draw(TextImage image, UiRect destination, UiRect source, UiColor color, float opacity, TextSampling sampling = TextSampling.Linear) => Last = image;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AssetRootsWithOrWithoutTrailingSeparatorsRemainContained(bool trailingSeparator)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "fonts");
        var assets = new DirectoryAssetSource(trailingSeparator ? path + Path.DirectorySeparatorChar : path);
        using var font = assets.Open("Lato-Regular.ttf");
        Assert.True(font.CanRead);
        Assert.Throws<IOException>(() => { using var escaped = assets.Open("../GuiShark.Tests.dll"); });
    }
}
