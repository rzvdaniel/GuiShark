using GuiShark.AppProtocol;
using GuiShark.ProcessHosting;

namespace GuiShark.AuroraProcess;

internal sealed class AuroraWindow(bool verify, bool embedded = false)
    : ProcessAppWindow("Aurora Monitor · GuiShark app", embedded)
{
    private readonly AuroraCounter counter = new();
    private double elapsed;
    private bool clicked;
    protected override bool RenderContinuously => false;
    protected override string? FrameValue => counter.Count.ToString();

    protected override void OnAppLoaded(UiView appView)
    {
        appView.Document.GetElement("increment").Clicked += _ =>
        {
            counter.Increment();
            RequestFrame();
            appView.Document.GetElement("count").Text = counter.Count.ToString();
            appView.Document.GetElement("message").Text = counter.Message;
            Send(new AppMessage("state", Value: counter.Count.ToString()));
        };
    }

    protected override void OnAppUpdate(UiView appView, double seconds)
    {
        elapsed += seconds;
        if (!verify || elapsed < .5) return;
        if (!clicked)
        {
            var button = appView.Document.GetElement("increment").Bounds;
            var x = button.X + button.Width / 2;
            var y = button.Y + button.Height / 2;
            appView.Input.PointerMove(x, y);
            appView.Input.PointerDown(x, y);
            appView.Input.PointerUp(x, y);
            clicked = true;
            return;
        }
        if (counter.Count != 1) throw new InvalidOperationException("Standalone Aurora click did not update its state.");
        Console.WriteLine("VERIFY PASS: standalone window rendered and handled a button click");
        Close();
    }
}
