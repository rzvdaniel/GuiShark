using GuiShark.OpenGL;
using Xunit;

namespace GuiShark.Tests;

public sealed class BidirectionalTextTests
{
    private const string Faces = """
        @font-face { font-family: Lato; src: url('fonts/Lato-Regular.ttf'); }
        @font-face { font-family: Arabic; src: url('fonts/NotoSansArabic.ttf'); }
        @font-face { font-family: Hebrew; src: url('fonts/NotoSansHebrew.ttf'); }
        """;
    private static FontBook Fonts() => new(HtmlLoader.Load($"<html><head><style>{Faces}</style></head><body></body></html>",
        new DirectoryAssetSource(AppContext.BaseDirectory)), "Lato");

    [Theory]
    [InlineData("שלום")]
    [InlineData("مرحبا")]
    public void RtlCaretsMoveVisuallyAndSelectionUndoKeepsLogicalText(string value)
    {
        using var fonts = Fonts();
        using var backend = new SkiaTextBackend(fonts);
        using var ui = new UiScenario($"<input id='text' dir='rtl' value='{value}'>", metrics: backend);
        ui.Focus("text");
        ui.Press(UiKey.Home);
        Assert.Equal(value.Length, ui.Edit("text").Caret);
        var x = ui.Edit("text").CaretBounds.X;
        ui.Press(UiKey.Right, shift: true);
        Assert.Equal(value.Length - 1, ui.Edit("text").Caret);
        Assert.True(ui.Edit("text").CaretBounds.X > x);
        Assert.Equal(value[^1..], ui.Edit("text").SelectedText);
        ui.Press(UiKey.Backspace);
        Assert.Equal(value[..^1], ui.Edit("text").Value);
        ui.Press(UiKey.Z, command: true);
        Assert.Equal(value, ui.Edit("text").Value);
        Assert.Equal(value[^1..], ui.Edit("text").SelectedText);
        ui.Press(UiKey.End);
        Assert.Equal(0, ui.Edit("text").Caret);
    }

    [Fact]
    public void MixedTextHasTwoCaretLocationsAtADirectionBoundaryAndDisjointSelection()
    {
        using var fonts = Fonts();
        using var backend = new SkiaTextBackend(fonts);
        const string text = "abc אבג def";
        var map = backend.CreateCaretMap(TextLineContext.Whole(text), 24, false, "Lato");
        Assert.True(map.X(new TextCaretPosition(4)) > map.X(new TextCaretPosition(4, true)));
        Assert.True(map.X(4) > map.X(5));
        Assert.True(map.X(8) < map.X(9));
        Assert.Equal(2, map.Selection(0, 5).Count);
        Assert.Equal(backend.MeasureWidth(TextLineContext.Whole(text), 24, false, "Lato"), map.Width, 3);
    }

    [Fact]
    public void AutoDirectionUsesFullParagraphWhenAWrappedLineContainsOnlyDigits()
    {
        using var fonts = Fonts();
        using var backend = new SkiaTextBackend(fonts);
        const string text = "שלום 12345";
        var map = backend.CreateCaretMap(new TextLineContext(text, 5, 5, UiTextDirection.Auto), 24, false, "Lato");
        Assert.True(map.RightToLeft);
        Assert.True(map.X(0) < map.X(5));
    }

    [Fact]
    public void DirectionInheritsAndExplicitAlignmentCanOverrideStart()
    {
        using var ui = new UiScenario("<section dir='rtl'><input id='text'><input id='latin' dir='ltr'></section>");
        Assert.Equal(UiTextDirection.RightToLeft, ui.Element("text").TextDirection);
        Assert.Equal(UiTextDirection.LeftToRight, ui.Element("latin").TextDirection);
        Assert.Equal(80, TextAlignmentLayout.Offset(100, 20, TextAlignment.Start, true));
        Assert.Equal(0, TextAlignmentLayout.Offset(100, 20, TextAlignment.Left, true));
    }

    [Theory]
    [InlineData("שלום Alice 42")]
    [InlineData("مرحبا Alice 42")]
    public void DigitsStayLeftToRightWithinAnRtlParagraph(string text)
    {
        using var fonts = Fonts();
        using var backend = new SkiaTextBackend(fonts);
        var map = backend.CreateCaretMap(TextLineContext.Whole(text, UiTextDirection.RightToLeft), 24, false, "Lato");
        var digit = text.IndexOf('4');
        Assert.True(map.X(digit) < map.X(digit + 1));
        Assert.True(map.X(new TextCaretPosition(digit + 2, true)) > map.X(digit + 1));
    }

    [Fact]
    public void RtlWordSelectionAndMaskedPasswordsKeepTheirContracts()
    {
        using var fonts = Fonts();
        using var backend = new SkiaTextBackend(fonts);
        using var ui = new UiScenario("<input id='text' dir='rtl' value='שלום עולם'><input id='secret' dir='rtl' type='password' value='שלום'>", metrics: backend);
        ui.Focus("text");
        ui.Press(UiKey.End);
        ui.Press(UiKey.Left, shift: true, command: true);
        Assert.Equal("שלום", ui.Edit("text").SelectedText.TrimEnd());
        ui.Focus("secret");
        ui.Press(UiKey.Home);
        Assert.Equal(0, ui.Edit("secret").Caret);
        ui.Press(UiKey.Right);
        Assert.Equal(1, ui.Edit("secret").Caret);
        Assert.Equal("••••", ui.Element("secret").Text);
    }

    [Fact]
    public void EmptySelectionsAndSupplementaryGraphemesHaveValidCoordinates()
    {
        using var fonts = Fonts();
        using var backend = new SkiaTextBackend(fonts);
        var map = backend.CreateCaretMap(TextLineContext.Whole("אב 👩‍🚀 xyz", UiTextDirection.RightToLeft), 24, false, "Lato");
        Assert.Empty(map.Selection(0, 0));
        Assert.True(float.IsFinite(map.X(map.SelectionEdge(0, 0, true))));
        Assert.Equal(map.X(3), map.X(4));
    }

    [Theory]
    [InlineData("start", true)]
    [InlineData("left", false)]
    public void RtlAlignmentAndPointerCoordinatesAgree(string alignment, bool rightAligned)
    {
        using var fonts = Fonts();
        using var backend = new SkiaTextBackend(fonts);
        using var ui = new UiScenario("<input id='text' dir='rtl' value='שלום'>",
            $"input {{ width: 300px; text-align: {alignment}; }}", backend);
        var element = ui.Element("text");
        ui.Focus("text");
        ui.Press(UiKey.End);
        var width = backend.MeasureWidth(TextLineContext.Whole("שלום", UiTextDirection.RightToLeft), 14, false, "Lato");
        var x = element.ContentBounds.X + (rightAligned ? element.ContentBounds.Width : width);
        Assert.Equal(x, ui.Edit("text").CaretBounds.X, 3);
        ui.Input.PointerDown(x, element.ContentBounds.Y + 4);
        ui.Input.PointerUp(x, element.ContentBounds.Y + 4);
        Assert.Equal(0, ui.Edit("text").Caret);
    }

    [Fact]
    public void CompositionCancellationRestoresTheClickedBidiCaretAffinity()
    {
        using var fonts = Fonts();
        using var backend = new SkiaTextBackend(fonts);
        using var ui = new UiScenario("<input id='text' value='abc אבג def'>", metrics: backend);
        var map = backend.CreateCaretMap(TextLineContext.Whole("abc אבג def"), 14, false, "Lato");
        var element = ui.Element("text");
        var x = element.ContentBounds.X + map.X(new TextCaretPosition(4, true));
        ui.Input.PointerDown(x, element.ContentBounds.Y + 4);
        ui.Input.PointerUp(x, element.ContentBounds.Y + 4);
        Assert.Equal(4, ui.Edit("text").Caret);
        ui.Input.UpdateComposition("שלום", 4);
        ui.View.Update();
        Assert.NotEmpty(ui.Edit("text").CompositionRects);
        ui.Edit("text").CancelComposition();
        ui.View.Update();
        Assert.Equal(x, ui.Edit("text").CaretBounds.X, 3);
    }

    [Theory]
    [InlineData("אב\u2066Alice 42\u2069ג")]
    [InlineData("אבe\u0301ג")]
    [InlineData("אב (42) xyz")]
    public void UnicodeIsolatesMarksAndBracketsKeepFiniteGraphemeCoordinates(string text)
    {
        using var fonts = Fonts();
        using var backend = new SkiaTextBackend(fonts);
        var map = backend.CreateCaretMap(TextLineContext.Whole(text, UiTextDirection.Auto), 24, false, "Lato");
        foreach (var index in System.Globalization.StringInfo.ParseCombiningCharacters(text).Append(text.Length))
            Assert.True(float.IsFinite(map.X(index)));
        Assert.True(map.Width > 0);
    }

    [Fact]
    public void ArrowAcrossParagraphsEntersTheNextParagraphAtItsLogicalStart()
    {
        using var fonts = Fonts();
        using var backend = new SkiaTextBackend(fonts);
        using var ui = new UiScenario("<textarea id='text' dir='auto'>שלום\nHello</textarea>", metrics: backend);
        ui.Focus("text");
        ui.Press(UiKey.Home);
        Assert.Equal(4, ui.Edit("text").Caret);
        ui.Press(UiKey.Left);
        Assert.Equal(5, ui.Edit("text").Caret);
        ui.Press(UiKey.Right);
        Assert.Equal(6, ui.Edit("text").Caret);
    }

    [Fact]
    public void EmptyTextStillValidatesTheFontFamily()
    {
        using var fonts = Fonts();
        using var backend = new SkiaTextBackend(fonts);
        Assert.Throws<InvalidOperationException>(() => backend.MeasureWidth("", 24, false, "Missing"));
        Assert.Throws<InvalidOperationException>(() => backend.CreateCaretMap("", 24, false, "Missing"));
    }
}
