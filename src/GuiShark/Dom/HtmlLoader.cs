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
        return new UiDocument(Convert(dom.Body ?? throw new FormatException("Missing body.")), rules, assets, fonts, theme);
    }

    private static UiElement Convert(IElement source)
    {
        if (source.LocalName is not ("body" or "div" or "section" or "main" or "header" or "footer" or "p" or "span" or "h1" or "h2" or "h3" or "button" or "img" or "input" or "progress" or "label" or "select" or "option" or "dialog" or "textarea"))
            throw new FormatException($"Unsupported HTML element: <{source.LocalName}>");
        var text = Regex.Replace(string.Concat(source.ChildNodes.Where(n => n.NodeType == NodeType.Text).Select(n => n.TextContent)), @"\s+", " ").Trim();
        if (text.Length > 0 && source.Children.Length > 0)
            throw new FormatException("Mixed inline text and child elements are not supported. Wrap text in a span.");
        var element = new UiElement(source.LocalName, source.Id ?? "", text, source.ClassName ?? "", source.HasAttribute("disabled"))
        {
            ImageSource = source.GetAttribute("src"),
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
        if (element.Tag == "dialog")
        {
            if (source.HasAttribute("open")) throw new FormatException("Open dialogs through ShowModal after creating a UiView.");
            element.Dialog = new(element);
        }
        ControlLoader.Load(element, source);
        element.Children = source.Children.Select(Convert).ToArray();
        foreach (var child in element.Children) child.Parent = element;
        if (element.Tag == "select")
        {
            if (source.HasAttribute("multiple") || source.HasAttribute("size")) throw new FormatException("Only single-choice dropdown selects are supported.");
            if (element.Children.Any(e => e.Tag != "option")) throw new FormatException("A select accepts only option children.");
            var index = Array.FindLastIndex(element.Children.ToArray(), e => e.IsSelected);
            if (index < 0) index = Array.FindIndex(element.Children.ToArray(), e => !e.Disabled && !e.Hidden);
            element.Select = new(element, index);
        }
        return element;
    }
}
