using Xunit;

namespace GuiShark.Tests;

public sealed class ModalTests
{
    private const string Html = "<button id='opener'>Open</button><dialog id='dialog'><button id='first' autofocus>First</button><button id='last'>Last</button></dialog>";

    [Fact]
    public void FocusStaysInsideModalAndReturnsToOpenerOnEscape()
    {
        using var ui = new UiScenario(Html);
        ui.Focus("opener");
        var dialog = ui.Element("dialog").Dialog!;
        dialog.ShowModal();
        Assert.Same(ui.Element("first"), ui.Input.Focused);
        ui.Press(UiKey.Tab);
        Assert.Same(ui.Element("last"), ui.Input.Focused);
        ui.Press(UiKey.Tab);
        Assert.Same(ui.Element("first"), ui.Input.Focused);
        Assert.Throws<ArgumentException>(() => ui.Focus("opener"));
        ui.Press(UiKey.Escape);
        Assert.False(dialog.IsOpen);
        Assert.Same(ui.Element("opener"), ui.Input.Focused);
    }

    [Fact]
    public void EscapeHonorsCancellationPolicyAndReportsCancelOnce()
    {
        using var ui = new UiScenario(Html);
        var dialog = ui.Element("dialog").Dialog!;
        var cancellations = 0;
        dialog.Cancelled += _ => cancellations++;
        dialog.CloseOnEscape = false;
        dialog.ShowModal();
        ui.Press(UiKey.Escape);
        Assert.True(dialog.IsOpen);
        Assert.Equal(0, cancellations);
        dialog.CloseOnEscape = true;
        ui.Input.KeyDown(UiKey.Escape);
        ui.Input.KeyDown(UiKey.Escape, repeat: true);
        ui.Input.KeyUp(UiKey.Escape);
        Assert.Equal(1, cancellations);
        Assert.Equal("cancel", dialog.ReturnValue);
    }

    [Fact]
    public void HiddenOpenerIsNotRestoredOnClose()
    {
        using var ui = new UiScenario(Html);
        ui.Focus("opener");
        var dialog = ui.Element("dialog").Dialog!;
        dialog.ShowModal();
        ui.Element("opener").Hidden = true;
        dialog.Close();
        Assert.False(dialog.IsOpen);
        Assert.Null(ui.Input.Focused);
    }
}
