using Xunit;

namespace GuiShark.Tests;

public sealed class CompositionTests
{
    [Fact]
    public void PreeditReplacesSelectionVisuallyAndCommitIsOneUndoableEdit()
    {
        using var ui = new UiScenario("<input id='text' value='old town'>");
        ui.Focus("text");
        var edit = ui.Edit("text");
        edit.Select(0, 3);
        var changes = 0;
        edit.Changed += _ => changes++;
        ui.Input.UpdateComposition("とうきょう", 3, 2);
        ui.View.Update();
        Assert.Equal("old town", edit.Value);
        Assert.Equal("とうきょう town", ui.Element("text").Text);
        Assert.Equal("old", edit.SelectedText);
        Assert.Equal(0, changes);
        Assert.False(edit.CanUndo);
        Assert.NotEmpty(edit.CompositionRects);
        ui.Input.UpdateComposition("東京", 2);
        ui.Input.TextInput("東京");
        Assert.Null(edit.Composition);
        Assert.Equal("東京 town", edit.Value);
        Assert.Equal(1, changes);
        ui.Press(UiKey.Z, command: true);
        Assert.Equal("old town", edit.Value);
        Assert.Equal("old", edit.SelectedText);
        Assert.False(edit.CanUndo);
        ui.Press(UiKey.Y, command: true);
        Assert.Equal("東京 town", edit.Value);
    }

    [Fact]
    public void CancelRestoresSelectionAndFocusChangeCannotCarryPreedit()
    {
        using var ui = new UiScenario("<input id='first' value='town'><input id='second'>");
        ui.Focus("first");
        var first = ui.Edit("first");
        first.SelectAll();
        ui.Input.UpdateComposition("東京", 1);
        ui.Press(UiKey.Escape);
        Assert.Null(first.Composition);
        Assert.Equal("town", first.SelectedText);
        Assert.Same(ui.Element("first"), ui.Input.Focused);
        ui.Input.UpdateComposition("東京", 2);
        ui.Focus("second");
        Assert.Null(first.Composition);
        Assert.Equal("town", first.Value);
        Assert.False(first.CanUndo);
        ui.Input.TextInput("new");
        Assert.Equal("new", ui.Edit("second").Value);
    }

    [Fact]
    public void CompositionOwnsEditingKeysAndEnterDoesNotSubmit()
    {
        using var ui = new UiScenario("<input id='text' value='old'>");
        ui.Focus("text");
        var edit = ui.Edit("text");
        edit.SelectAll();
        var submitted = false;
        edit.Submitted += _ => submitted = true;
        ui.Input.UpdateComposition("新", 1);
        ui.Press(UiKey.Backspace);
        ui.Press(UiKey.Left, shift: true);
        ui.Press(UiKey.Enter);
        Assert.Equal("old", edit.Value);
        Assert.Equal("old", edit.SelectedText);
        Assert.False(submitted);
        Assert.NotNull(edit.Composition);
    }

    [Fact]
    public void PreeditMayExceedMaximumButCommitNeverSplitsGraphemes()
    {
        using var ui = new UiScenario("<input id='text' maxlength='2'>");
        ui.Focus("text");
        ui.Input.UpdateComposition("a👩‍🚀", 1);
        Assert.NotNull(ui.Edit("text").Composition);
        ui.Input.TextInput("a👩‍🚀");
        Assert.Equal("a", ui.Edit("text").Value);
        Assert.Null(ui.Edit("text").Composition);
    }

    [Fact]
    public void ReadOnlyAndDisabledFieldsCannotKeepOrReceivePreedit()
    {
        using var ui = new UiScenario("<input id='text' value='safe'>");
        ui.Focus("text");
        var edit = ui.Edit("text");
        ui.Input.UpdateComposition("東京", 2);
        ui.View.Update();
        edit.ReadOnly = true;
        ui.View.Update();
        Assert.Null(edit.Composition);
        Assert.False(ui.Input.UpdateComposition("東京", 2));
        Assert.Equal("safe", edit.Value);
        edit.ReadOnly = false;
        ui.Input.UpdateComposition("東京", 2);
        ui.Element("text").Disabled = true;
        ui.View.Update();
        Assert.Null(edit.Composition);
        Assert.Null(ui.Input.Focused);
    }

    [Fact]
    public void PasswordPreeditIsMaskedAndNeverCopiedToClipboard()
    {
        using var ui = new UiScenario("<input id='text' type='password' value='old'>");
        var clipboard = new TestClipboard();
        ui.Input.Clipboard = clipboard;
        ui.Focus("text");
        ui.Edit("text").SelectAll();
        ui.Input.UpdateComposition("é👩‍🚀", 2);
        ui.View.Update();
        Assert.Equal("••", ui.Element("text").Text);
        ui.Press(UiKey.C, command: true);
        Assert.Equal(0, clipboard.Writes);
        Assert.Equal("old", ui.Edit("text").Value);
    }

    [Fact]
    public void EscapeCancelsCompositionBeforeClosingModal()
    {
        using var ui = new UiScenario("<dialog id='dialog'><input id='text'></dialog>");
        var dialog = ui.Element("dialog").Dialog!;
        dialog.ShowModal();
        ui.Focus("text");
        ui.Input.UpdateComposition("東京", 2);
        ui.Press(UiKey.Escape);
        Assert.True(dialog.IsOpen);
        Assert.Null(ui.Edit("text").Composition);
        ui.Press(UiKey.Escape);
        Assert.False(dialog.IsOpen);
    }

    [Fact]
    public void WrappedPreeditHasUnderlineOnEveryLineAndCancelRestoresGeometry()
    {
        using var ui = new UiScenario("<textarea id='text'>old</textarea>", "textarea { width: 70px; height: 100px; font-size: 16px; }");
        ui.Focus("text");
        ui.Edit("text").SelectAll();
        ui.Input.UpdateComposition("abcdefghijklmnop", 16);
        ui.View.Update();
        Assert.True(ui.Edit("text").CompositionRects.Count > 1);
        ui.Input.Cancel();
        ui.View.Update();
        Assert.Empty(ui.Edit("text").CompositionRects);
        Assert.Equal("old", ui.Edit("text").Value);
    }

    [Fact]
    public void InvalidCompositionOffsetsLeaveExistingStateUntouched()
    {
        using var ui = new UiScenario("<input id='text'>");
        ui.Focus("text");
        ui.Input.UpdateComposition("東京", 2);
        Assert.Throws<ArgumentOutOfRangeException>(() => ui.Input.UpdateComposition("東京", 2, 1));
        Assert.Equal(new UiComposition("東京", 2, 0), ui.Edit("text").Composition);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    [InlineData(2, int.MaxValue)]
    public void CompositionOffsetsCannotSplitUnicodeScalarsOrExceedText(int start, int length)
    {
        using var ui = new UiScenario("<input id='text'>");
        ui.Focus("text");
        Assert.Throws<ArgumentOutOfRangeException>(() => ui.Input.UpdateComposition("🌿", start, length));
        Assert.Null(ui.Edit("text").Composition);
    }

    [Fact]
    public void TabAndPointerCancelBeforeMovingFocusOrCaret()
    {
        using var ui = new UiScenario("<input id='first' value='old'><input id='second'>");
        ui.Focus("first");
        ui.Edit("first").SelectAll();
        ui.Input.UpdateComposition("long replacement", 16);
        var bounds = ui.Element("first").ContentBounds;
        ui.Input.PointerDown(bounds.X + 1, bounds.Y + 1);
        ui.Input.PointerUp(bounds.X + 1, bounds.Y + 1);
        Assert.Null(ui.Edit("first").Composition);
        Assert.Equal(0, ui.Edit("first").Caret);
        ui.Input.UpdateComposition("東京", 2);
        ui.Press(UiKey.Tab);
        Assert.Null(ui.Edit("first").Composition);
        Assert.Same(ui.Element("second"), ui.Input.Focused);
    }
}
