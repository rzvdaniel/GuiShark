using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System.Text.RegularExpressions;

namespace GuiShark;

public static class HtmlLoader
{
    public static UiDocument Load(string html, IAssetSource assets)
    {
        using var dom = new HtmlParser().ParseDocument(html);
        var rules = new List<CssRule>();
        foreach (var node in dom.QuerySelectorAll("style, link[rel=stylesheet]"))
        {
            var css = node.LocalName == "style" ? node.TextContent : assets.ReadText(node.GetAttribute("href") ?? "");
            rules.AddRange(CssParser.Parse(css));
        }
        return new UiDocument(Convert(dom.Body ?? throw new FormatException("Missing body.")), rules, assets);
    }

    private static UiElement Convert(IElement source)
    {
        if (source.LocalName is not ("body" or "div" or "section" or "main" or "header" or "footer" or "p" or "span" or "h1" or "h2" or "h3" or "button" or "img"))
            throw new FormatException($"Unsupported HTML element: <{source.LocalName}>");
        var text = Regex.Replace(string.Concat(source.ChildNodes.Where(n => n.NodeType == NodeType.Text).Select(n => n.TextContent)), @"\s+", " ").Trim();
        if (text.Length > 0 && source.Children.Length > 0)
            throw new FormatException("Mixed inline text and child elements are not supported. Wrap text in a span.");
        var element = new UiElement(source.LocalName, source.Id ?? "", text, source.ClassName ?? "", source.HasAttribute("disabled"))
        {
            ImageSource = source.GetAttribute("src"),
            InlineStyle = CssParser.ParseDeclarations(source.GetAttribute("style") ?? "")
        };
        element.Children = source.Children.Select(Convert).ToArray();
        foreach (var child in element.Children) child.Parent = element;
        return element;
    }
}
