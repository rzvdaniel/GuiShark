using System.Text.RegularExpressions;

namespace GuiShark;

internal sealed record CssRule(CssSelector Selector, IReadOnlyDictionary<string, string> Declarations);

internal static class CssParser
{
    // A deliberately bounded grammar: flat rules, simple selectors and plain declaration values.
    public static List<CssRule> Parse(string css, List<FontFace> fonts)
    {
        css = Regex.Replace(css, @"/\*.*?\*/", "", RegexOptions.Singleline, ParserLimits.RegexTimeout);
        css = Regex.Replace(css, @"@font-face\s*\{([^{}]*)\}", match =>
        {
            fonts.Add(FontFaceParser.Parse(match.Groups[1].Value));
            return "";
        }, RegexOptions.None, ParserLimits.RegexTimeout);
        if (css.Contains('@')) throw new FormatException("CSS at-rules are not supported.");
        var rules = new List<CssRule>();
        var consumed = 0;
        foreach (Match match in Regex.Matches(css, @"([^{}]+)\{([^{}]*)\}", RegexOptions.None, ParserLimits.RegexTimeout))
        {
            if (!string.IsNullOrWhiteSpace(css[consumed..match.Index])) throw new FormatException("Malformed CSS rule.");
            var declarations = ParseDeclarations(match.Groups[2].Value);
            foreach (var selector in match.Groups[1].Value.Split(','))
                rules.Add(new(new CssSelector(selector.Trim()), declarations));
            consumed = match.Index + match.Length;
        }
        if (!string.IsNullOrWhiteSpace(css[consumed..])) throw new FormatException("Malformed CSS rule.");
        return rules;
    }

    public static IReadOnlyDictionary<string, string> ParseDeclarations(string text)
    {
        var result = new Dictionary<string, string>();
        foreach (var declaration in text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var colon = declaration.IndexOf(':');
            if (colon < 1) throw new FormatException($"Invalid CSS declaration: {declaration}");
            var name = declaration[..colon].Trim().ToLowerInvariant();
            var value = declaration[(colon + 1)..].Trim();
            if (value.Contains('!')) throw new FormatException("!important is not supported.");
            // Validate once, rather than hiding misspellings or unsupported properties.
            StyleProperty.Apply(new UiStyle(), name, value);
            result[name] = value;
        }
        return result;
    }
}
