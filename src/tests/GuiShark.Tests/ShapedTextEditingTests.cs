using GuiShark.OpenGL;
using Xunit;

namespace GuiShark.Tests;

public sealed class ShapedTextEditingTests
{
    [Fact]
    public void GeometryUsesCompleteLineCoordinatesForMouseCaretAndSelection()
    {
        using var ui = new UiScenario("<input id='text' value='ab'>", metrics: new ContextMetrics());
        var edit = ui.Edit("text");
        var bounds = ui.Element("text").ContentBounds;
        ui.Input.PointerDown(bounds.X + 4, bounds.Y + 4);
        ui.Input.PointerUp(bounds.X + 4, bounds.Y + 4);
        Assert.Equal(1, edit.Caret);
        Assert.Equal(bounds.X + 4, edit.CaretBounds.X);
        ui.Press(UiKey.Right, shift: true);
        Assert.Equal("b", edit.SelectedText);
        Assert.Equal(16, edit.SelectionBounds.Width);
    }

    [Fact]
    public void LigatureCaretsDivideTheShapedClusterInsteadOfReshapingPrefixes()
    {
        using var fonts = Fonts();
        using var backend = new SkiaTextBackend(fonts);
        var map = backend.CreateCaretMap("office", 32, false, "");
        var step = (map.X(4) - map.X(1)) / 3;
        Assert.True(step > 0);
        Assert.Equal(map.X(1) + step, map.X(2), 3);
        Assert.Equal(map.X(1) + step * 2, map.X(3), 3);
        Assert.Equal(backend.MeasureWidth("office", 32, false), map.X(6), 3);
        Assert.True(Math.Abs(map.X(2) - backend.MeasureWidth("of", 32, false)) > 0.01f);
    }

    [Theory]
    [InlineData("Ae\u0301B", 1, 3)]
    [InlineData("A\U0001F600B", 1, 3)]
    [InlineData("A\U0001F469\u200D\U0001F680B", 1, 6)]
    public void ShapedEditingPreservesGraphemesAndUndo(string text, int start, int end)
    {
        using var fonts = Fonts();
        using var backend = new SkiaTextBackend(fonts);
        using var ui = new UiScenario($"<input id='text' value='{text}'>", metrics: backend);
        ui.Focus("text");
        var edit = ui.Edit("text");
        edit.Select(start, 0);
        ui.Press(UiKey.Right, shift: true);
        Assert.Equal(end, edit.Caret);
        Assert.Equal(text[start..end], edit.SelectedText);
        ui.Press(UiKey.Backspace);
        Assert.Equal("AB", edit.Value);
        ui.Press(UiKey.Z, command: true);
        Assert.Equal(text, edit.Value);
        var map = backend.CreateCaretMap(text, 24, false, "");
        Assert.Equal(map.X(start), map.X(start + 1));
        Assert.Contains(map.Nearest((map.X(start) + map.X(end)) / 2), new[] { start, end });
    }

    [Fact]
    public void PasswordCoordinatesMapBulletsBackToOriginalUtf16Offsets()
    {
        using var ui = new UiScenario("<input id='text' type='password' value='e&#769;x'>");
        ui.Focus("text");
        ui.Press(UiKey.Right);
        Assert.Equal(2, ui.Edit("text").Caret);
        ui.Press(UiKey.Right, shift: true);
        Assert.Equal("x", ui.Edit("text").SelectedText);
        Assert.Equal(ui.Element("text").Style.FontSize / 2, ui.Edit("text").SelectionBounds.Width);
    }

    private static FontBook Fonts() => new(Path.Combine(AppContext.BaseDirectory, "fonts", "Lato-Regular.ttf"),
        Path.Combine(AppContext.BaseDirectory, "fonts", "Lato-Bold.ttf"));

    [Fact]
    public void VerticalMovementRetainsTheShapedCoordinate()
    {
        using var ui = new UiScenario("<textarea id='text'>ab\nab</textarea>", metrics: new ContextMetrics());
        ui.Focus("text");
        ui.Edit("text").Select(1, 0);
        ui.Press(UiKey.Down);
        Assert.Equal(4, ui.Edit("text").Caret);
        Assert.Equal(ui.Element("text").ContentBounds.X + 4, ui.Edit("text").CaretBounds.X);
    }

    [Fact]
    public void PartialLigatureSelectionAndCompositionUseTheSameCoordinates()
    {
        using var fonts = Fonts();
        using var backend = new SkiaTextBackend(fonts);
        using var ui = new UiScenario("<input id='text' value='office'>", metrics: backend);
        ui.Focus("text");
        var map = backend.CreateCaretMap("office", ui.Element("text").Style.FontSize, false, "");
        var edit = ui.Edit("text");
        var bounds = ui.Element("text").ContentBounds;
        ui.Input.PointerDown(bounds.X + map.X(2), bounds.Y + 4);
        ui.Input.PointerUp(bounds.X + map.X(2), bounds.Y + 4);
        Assert.Equal(2, edit.Caret);
        edit.Select(2, 1);
        ui.View.Update();
        Assert.Equal(map.X(3) - map.X(2), edit.SelectionBounds.Width, 3);
        edit.SelectAll();
        ui.Input.UpdateComposition("office", 2);
        ui.View.Update();
        Assert.Equal(edit.TextBounds.X + map.X(2), edit.CaretBounds.X, 3);
        Assert.Equal(map.X(6), edit.CompositionRects.Single().Width, 3);
    }

    [Fact]
    public void WrappedLinesUseTheirOwnShapingContext()
    {
        using var fonts = Fonts();
        using var backend = new SkiaTextBackend(fonts);
        using var ui = new UiScenario("<textarea id='text'>office office office</textarea>",
            "textarea { width: 100px; font-size: 24px; }", backend);
        ui.Focus("text");
        ui.Press(UiKey.Home);
        ui.Press(UiKey.End);
        var edit = ui.Edit("text");
        Assert.True(edit.DisplayLines.Count > 1);
        var firstLine = edit.DisplayLines[0];
        var width = backend.CreateCaretMap(firstLine, 24, false, "").X(firstLine.Length);
        Assert.Equal(edit.TextBounds.X + width, edit.CaretBounds.X, 3);
        edit.SelectAll();
        ui.View.Update();
        Assert.Equal(width, edit.SelectionRects[0].Width, 3);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void RasterScaleKeepsCaretCoordinatesInLogicalUnits(float scale)
    {
        using var fonts = Fonts();
        using var backend = new SkiaTextBackend(fonts);
        backend.Configure(scale, new());
        var map = backend.CreateCaretMap("office", 24, false, "");
        Assert.Equal(backend.MeasureWidth("office", 24, false), map.X(6), 3);
        Assert.Equal((map.X(1) + map.X(4)) / 2, (map.X(2) + map.X(3)) / 2, 3);
    }

    private sealed class ContextMetrics : ITextMetrics
    {
        public float MeasureWidth(string text, float fontSize, bool bold) => text.Length * 10;
        public TextCaretMap CreateCaretMap(string text, float fontSize, bool bold, string family) =>
            text == "ab" ? new(text, [0, 4, 20]) : TextCaretMap.Measure(text, prefix => prefix.Length * 10);
    }
}
