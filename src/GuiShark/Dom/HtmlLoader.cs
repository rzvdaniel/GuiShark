using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System.Text.RegularExpressions;

namespace GuiShark;

public static class HtmlLoader
{
    public static UiDocument Load(string html, IAssetSource assets, UiTheme theme = UiTheme.None)
    {
        using var dom = new HtmlParser().ParseDocument(html);
        var rules = new List<CssRule>();
        var fonts = new List<FontFace>();
        foreach (var node in dom.QuerySelectorAll("style, link[rel=stylesheet]"))
        {
            var css = node.LocalName == "style" ? node.TextContent : assets.ReadText(node.GetAttribute("href") ?? "");
            rules.AddRange(CssParser.Parse(css, fonts));
        }
        var root = Convert(dom.Body ?? throw new FormatException("Missing body."));
        root.Direction ??= ParseDirection(dom.DocumentElement?.GetAttribute("dir"));
        return new UiDocument(root, rules, assets, fonts, theme);
    }

    private static UiElement Convert(IElement source)
    {
        if (source.LocalName is not ("body" or "div" or "section" or "main" or "header" or "footer" or "p" or "span" or "h1" or "h2" or "h3" or "button" or "img" or "input" or "progress" or "label" or "select" or "option" or "dialog" or "textarea"))
            throw new FormatException($"Unsupported HTML element: <{source.LocalName}>");
        var text = Regex.Replace(string.Concat(source.ChildNodes.Where(n => n.NodeType == NodeType.Text).Select(n => n.TextContent)), @"\s+", " ", RegexOptions.None, ParserLimits.RegexTimeout).Trim();
        if (text.Length > 0 && source.Children.Length > 0)
            throw new FormatException("Mixed inline text and child elements are not supported. Wrap text in a span.");
        var element = CreateElement(source, text);
        if (element.Tag == "dialog")
        {
            if (source.HasAttribute("open")) throw new FormatException("Open dialogs through ShowModal after creating a UiView.");
            element.Dialog = new(element);
        }
        ControlLoader.Load(element, source);
        element.Children = source.Children.Select(Convert).ToArray();
        foreach (var child in element.Children) child.Parent = element;
        if (element.Tag == "select") LoadSelect(element, source);
        return element;
    }
    private static UiElement CreateElement(IElement source, string text)
    {
        return new UiElement(source.LocalName, source.Id ?? "", text, source.ClassName ?? "", source.HasAttribute("disabled"))
        {
            ImageSource = source.GetAttribute("src"),
            Direction = ParseDirection(source.GetAttribute("dir")),
            Role = source.GetAttribute("role"),
            AutoFocus = source.HasAttribute("autofocus"),
            TooltipText = source.GetAttribute("title"),
            TooltipTargetId = source.GetAttribute("aria-describedby"),
            Hidden = source.HasAttribute("hidden"),
            ControlTargetId = source.GetAttribute("aria-controls"),
            IsSelected = source.GetAttribute("aria-selected") == "true" || source.HasAttribute("selected"),
            OptionValue = source.GetAttribute("value") ?? text,
            InlineStyle = CssParser.ParseDeclarations(source.GetAttribute("style") ?? "")
        };
    }

    private static UiTextDirection? ParseDirection(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        null or "" => null,
        "ltr" => UiTextDirection.LeftToRight,
        "rtl" => UiTextDirection.RightToLeft,
        "auto" => UiTextDirection.Auto,
        _ => throw new FormatException("dir accepts ltr, rtl or auto.")
    };

    private static void LoadSelect(UiElement element, IElement source)
    {
        if (source.HasAttribute("multiple") || source.HasAttribute("size")) throw new FormatException("Only single-choice dropdown selects are supported.");
        if (element.Children.Any(e => e.Tag != "option")) throw new FormatException("A select accepts only option children.");
        var index = Array.FindLastIndex(element.Children.ToArray(), e => e.IsSelected);
        if (index < 0) index = Array.FindIndex(element.Children.ToArray(), e => !e.Disabled && !e.Hidden);
        element.Select = new(element, index);
    }

}
