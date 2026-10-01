using Xunit;

namespace GuiShark.Tests;

public sealed class PasswordTests
{
    [Fact]
    public void MasksByGraphemeAndRevealDoesNotChangeValue()
    {
        using var ui = new UiScenario("<input id='secret' type='password'>");
        var secret = ui.Edit("secret");
        secret.Value = "a\U0001F469\u200D\U0001F680e\u0301";
        Assert.Equal("\u2022\u2022\u2022", ui.Element("secret").Text);
        secret.ShowPassword = true;
        Assert.Equal(secret.Value, ui.Element("secret").Text);
        secret.ShowPassword = false;
        Assert.Equal("\u2022\u2022\u2022", ui.Element("secret").Text);
        Assert.Equal("a\U0001F469\u200D\U0001F680e\u0301", secret.Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CopyAndCutNeverExportPasswordEvenWhenRevealed(bool revealed)
    {
        using var ui = new UiScenario("<input id='secret' type='password' value='private'>");
        var clipboard = new TestClipboard();
        ui.Input.Clipboard = clipboard;
        ui.Focus("secret");
        var secret = ui.Edit("secret");
        secret.ShowPassword = revealed;
        secret.SelectAll();
        ui.Press(UiKey.C, command: true);
        ui.Press(UiKey.X, command: true);
        Assert.Equal("private", secret.Value);
        Assert.Equal("untouched", clipboard.Text);
        Assert.Equal(0, clipboard.Writes);
    }

    [Fact]
    public void PasteRemainsSupportedAndUndoRestoresSecret()
    {
        using var ui = new UiScenario("<input id='secret' type='password' value='private'>");
        var clipboard = new TestClipboard { Text = "new secret" };
        ui.Input.Clipboard = clipboard;
        ui.Focus("secret");
        ui.Edit("secret").SelectAll();
        ui.Press(UiKey.V, command: true);
        Assert.Equal("new secret", ui.Edit("secret").Value);
        Assert.Equal(1, clipboard.Reads);
        ui.Press(UiKey.Z, command: true);
        Assert.Equal("private", ui.Edit("secret").Value);
    }
}
