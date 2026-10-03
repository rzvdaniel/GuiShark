using OpenTK.Windowing.GraphicsLibraryFramework;

namespace GuiShark.Workspace;

internal sealed class WorkspaceUiVerification(WorkspaceController controller, WorkspaceLayout layout, WorkspaceChrome chrome, WorkspaceInput input)
{
    private int step;
    public bool Update()
    {
        switch (step)
        {
            case 0: Click("+ New space"); break;
            case 1: Submit("Verification space"); break;
            case 2:
                Require(controller.State.ActiveSpace.Name == "Verification space", "create space");
                Click("+");
                break;
            case 3: Submit("Scratch tab"); break;
            case 4:
                Require(controller.State.ActiveTab.Name == "Scratch tab", "create tab");
                controller.Change(() =>
                {
                    controller.State.AddTab("Extra one");
                    controller.State.AddTab("Extra two");
                    controller.State.AddTab("Extra three");
                });
                step = 9;
                return false;
            case 9: Click("Choose tab…"); step = 10; return false;
            case 10: Click("Scratch tab"); step = 11; return false;
            case 11:
                Require(controller.State.ActiveTab.Name == "Scratch tab", "overflow tab chooser");
                Click("Close space");
                step = 5;
                return false;
            case 5: Submit("close"); break;
            case 6:
                Require(controller.State.Spaces.Count == 2, "close space");
                var split = layout.Splits[0];
                input.PointerDown(split.Divider.X + 2, split.Divider.Y + 2, MouseButton.Left, false);
                input.PointerMove(split.Bounds.X + split.Bounds.Width * .55f, split.Bounds.Y + 2);
                input.PointerUp(split.Bounds.X + split.Bounds.Width * .55f, split.Bounds.Y + 2, MouseButton.Left, false);
                Require(controller.State.ActiveTab.Root.Ratio > .54f, "drag divider");
                Click("Zoom pane");
                break;
            case 7:
                Require(layout.Panes.Count == 1, "zoom pane");
                Click("Restore panes");
                break;
            case 8:
                Require(layout.Panes.Count == 4, "restore panes");
                Console.WriteLine("VERIFY: real chrome controls created a space/tab, confirmed close, dragged divider, zoomed/restored");
                return true;
        }
        step++;
        return false;
    }
    private UiRect restartButton;
    public bool TryBeginRestart()
    {
        chrome.View.Update();
        var button = chrome.View.Document.Root.DescendantsAndSelf().FirstOrDefault(element => element.IsButton && element.Text == "Restart app");
        if (button is null) return false;
        restartButton = button.Bounds;
        input.PointerDown(restartButton.X + restartButton.Width / 2, restartButton.Y + restartButton.Height / 2, MouseButton.Left, false);
        return true;
    }
    public void EndRestart() => input.PointerUp(restartButton.X + restartButton.Width / 2,
        restartButton.Y + restartButton.Height / 2, MouseButton.Left, false);

    private void Submit(string value)
    {
        Require(controller.Prompt is not null, "prompt did not open");
        chrome.View.Input.KeyDown(UiKey.A, command: true);
        chrome.View.Input.TextInput(value);
        Click("Confirm");
    }
    private void Click(string text)
    {
        chrome.View.Update();
        var button = chrome.View.Document.Root.DescendantsAndSelf().First(element => element.IsButton && element.Text == text);
        var bounds = button.Bounds;
        var x = bounds.X + bounds.Width / 2;
        var y = bounds.Y + bounds.Height / 2;
        input.PointerMove(x, y);
        input.PointerDown(x, y, MouseButton.Left, false);
        input.PointerUp(x, y, MouseButton.Left, false);
    }
    private static void Require(bool condition, string action)
    {
        if (!condition) throw new InvalidOperationException($"VERIFY FAIL: chrome {action}");
    }
}
