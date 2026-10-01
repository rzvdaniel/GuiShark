using Xunit;

namespace GuiShark.Tests;

public sealed class ParserTests
{
    [Fact]
    public void CommentsFontFacesSelectorsAndWhitespaceRetainTheirBehavior()
    {
        const string css = """
            /* a multiline
               comment */
            @font-face { font-family: Woodland; src: url(font.ttf); }
            .grove p.label, #message { color: #123456; font-family: Woodland; }
            .grove p.label:focus { color: #ffffff; }
            """;
        using var ui = new UiScenario("<div class='grove'><p id='message' class='label'>  Hello \n woodland  </p></div>", css);
        Assert.Equal("Hello woodland", ui.Element("message").Text);
        Assert.Equal(UiColor.Parse("#123456"), ui.Element("message").Style.Color);
        Assert.Equal("Woodland", ui.Element("message").Style.FontFamily);
        Assert.Single(ui.Document.FontFaces);
    }

    [Theory]
    [InlineData("p > span { color: white; }")]
    [InlineData("p:unknown { color: white; }")]
    [InlineData("p { color: white; ")]
    public void UnsupportedOrMalformedStylesRemainRejected(string css)
    {
        Assert.Throws<FormatException>(() => HtmlLoader.Load($"<html><head><style>{css}</style></head><body><p>Text</p></body></html>", new TestAssets()));
    }
}
