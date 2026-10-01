using Xunit;

namespace GuiShark.Tests;

public sealed class TextEditingTests
{
    [Fact]
    public void UndoAndRedoRestoreReplacementAndSelection()
    {
        using var ui = new UiScenario("<input id='text' value='old text'>");
        ui.Focus("text");
        var edit = ui.Edit("text");
        edit.SelectAll();
        ui.Input.TextInput("river");
        ui.Press(UiKey.Z, command: true);
        Assert.Equal("old text", edit.Value);
        Assert.Equal("old text", edit.SelectedText);
        ui.Press(UiKey.Y, command: true);
        Assert.Equal("river", edit.Value);
        Assert.Equal(0, edit.SelectionLength);
    }

    [Fact]
    public void CommandShiftArrowsSelectWordsAndPlainArrowCollapsesSelection()
    {
        using var ui = new UiScenario("<input id='text' value=\"keeper's lantern\">");
        ui.Focus("text");
        ui.Press(UiKey.End);
        ui.Press(UiKey.Left, shift: true, command: true);
        Assert.Equal("lantern", ui.Edit("text").SelectedText);
        ui.Press(UiKey.Left);
        Assert.Equal(0, ui.Edit("text").SelectionLength);
        ui.Press(UiKey.Home);
        ui.Press(UiKey.Right, shift: true, command: true);
        Assert.Equal("keeper's", ui.Edit("text").SelectedText.TrimEnd());
    }

    [Fact]
    public void DoubleClickSelectsAWord()
    {
        using var ui = new UiScenario("<input id='text' value='green valley'>");
        var content = ui.Element("text").ContentBounds;
        var x = content.X + 14;
        var y = content.Y + 5;
        ui.Input.PointerDown(x, y);
        ui.Input.PointerUp(x, y);
        ui.Input.PointerDown(x, y);
        ui.Input.PointerUp(x, y);
        Assert.Equal("green", ui.Edit("text").SelectedText);
    }

    [Theory]
    [InlineData("e\u0301")]
    [InlineData("\U0001F469\u200D\U0001F680")]
    public void BackspaceRemovesOneWholeGrapheme(string grapheme)
    {
        using var ui = new UiScenario("<input id='text'>");
        ui.Focus("text");
        ui.Input.TextInput("a" + grapheme);
        ui.Press(UiKey.Backspace);
        Assert.Equal("a", ui.Edit("text").Value);
        ui.Press(UiKey.Z, command: true);
        Assert.Equal("a" + grapheme, ui.Edit("text").Value);
    }

    [Fact]
    public void MaximumLengthNeverSplitsAnInsertedGrapheme()
    {
        using var ui = new UiScenario("<input id='text' maxlength='2'>");
        ui.Focus("text");
        ui.Input.TextInput("a\U0001F469\u200D\U0001F680");
        Assert.Equal("a", ui.Edit("text").Value);
    }

    [Fact]
    public void ReadOnlyInputAllowsCopyButRejectsMutations()
    {
        using var ui = new UiScenario("<input id='text' readonly value='protected'>");
        var clipboard = new TestClipboard();
        ui.Input.Clipboard = clipboard;
        ui.Focus("text");
        ui.Edit("text").SelectAll();
        ui.Press(UiKey.C, command: true);
        Assert.Equal("protected", clipboard.Text);
        clipboard.Text = "replacement";
        ui.Press(UiKey.X, command: true);
        ui.Press(UiKey.V, command: true);
        ui.Press(UiKey.Delete);
        ui.Input.TextInput("changed");
        Assert.Equal("protected", ui.Edit("text").Value);
        Assert.False(ui.Edit("text").CanUndo);
        Assert.Equal(1, clipboard.Writes);
    }

    [Fact]
    public void TextareaScrollsToCaretAndRestoresNewlineThroughUndo()
    {
        using var ui = new UiScenario("<textarea id='notes'>One\nTwo\nThree\nFour\nFive</textarea>", "textarea { height: 65px; }");
        ui.Focus("notes");
        ui.Press(UiKey.End, command: true);
        ui.Press(UiKey.Enter);
        ui.Input.TextInput("Six");
        ui.View.Update();
        Assert.EndsWith("\nSix", ui.Edit("notes").Value);
        Assert.True(ui.Element("notes").Scroll.Offset > 0);
        ui.Press(UiKey.Z, command: true);
        Assert.EndsWith("\n", ui.Edit("notes").Value);
        ui.Press(UiKey.Z, command: true);
        Assert.EndsWith("Five", ui.Edit("notes").Value);
    }
}
