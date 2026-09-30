namespace GuiShark;

internal static class HitTester
{
    public static UiElement? Hit(UiElement element, float x, float y, bool overlay = false)
    {
        if (element.IsOverlay && !overlay) return null;
        if (element.Style.Hidden || !element.Clip.Contains(x, y)) return null;
        for (var i = (element.Select != null ? 0 : element.Children.Count) - 1; i >= 0; i--)
        {
            var hit = Hit(element.Children[i], x, y);
            if (hit != null) return hit;
        }
        return element.Parent == null || !element.Style.PointerEvents ? null : element;
    }

    public static UiElement? Interactive(UiElement? element)
    {
        for (; element != null; element = element.Parent)
            if (element.IsInteractive) return element;
            else if (element.LabelTarget != null) return element.LabelTarget;
        return null;
    }

    public static bool CanFocus(UiElement element) => element.IsInteractive && element.Style.PointerEvents && !element.Disabled &&
        !Ancestors(element).Any(e => e.Style.Hidden || e.Hidden || e.Disabled);

    public static bool CanActivate(UiElement element) => CanFocus(element) && element.Clip.Width > 0 && element.Clip.Height > 0;

    private static IEnumerable<UiElement> Ancestors(UiElement element)
    {
        for (var node = element; node != null; node = node.Parent) yield return node;
    }
}
