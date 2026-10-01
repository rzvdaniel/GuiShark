using Xunit;

namespace GuiShark.Tests;

public sealed class ResourceOwnershipTests
{
    [Fact]
    public void ReplacingMetricsAndDisposingViewDoesNotDisposeBorrowedMetrics()
    {
        using var original = new BorrowedMetrics();
        using var replacement = new BorrowedMetrics();
        using var ui = new UiScenario("<input id='text'>", metrics: original);
        ui.View.SetTextMetrics(replacement);
        ui.View.Dispose();
        ui.View.Dispose();
        Assert.False(original.Disposed);
        Assert.False(replacement.Disposed);
        Assert.Throws<ObjectDisposedException>(() => ui.View.Update());
        Assert.Throws<ObjectDisposedException>(() => ui.View.SetTextMetrics(original));
    }

    [Fact]
    public void DisposalCancelsPendingActivationAndDetachesDialogs()
    {
        using var ui = new UiScenario("<button id='button'>Go</button><dialog id='dialog'><button>Close</button></dialog>");
        var clicked = 0;
        var button = ui.Element("button");
        button.Clicked += _ => clicked++;
        ui.Focus("button");
        ui.Input.KeyDown(UiKey.Enter);
        Assert.True(button.IsPressed);
        ui.View.Dispose();
        Assert.False(button.IsPressed);
        Assert.Equal(0, clicked);
        Assert.Throws<InvalidOperationException>(() => ui.Element("dialog").Dialog!.ShowModal());
    }

    private sealed class BorrowedMetrics : ITextMetrics, IDisposable
    {
        public bool Disposed { get; private set; }
        public float MeasureWidth(string text, float fontSize, bool bold) => text.Length * fontSize / 2;
        public void Dispose() => Disposed = true;
    }
}
