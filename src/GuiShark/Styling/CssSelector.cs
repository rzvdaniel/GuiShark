using System.Text.RegularExpressions;

namespace GuiShark;

internal sealed class CssSelector
{
    private readonly string[] parts;
    public (int Ids, int Classes, int Types) Specificity { get; }

    public CssSelector(string selector)
    {
        parts = selector.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || parts.Any(p => !Regex.IsMatch(p, @"^(\*|[a-z][\w-]*)?([.#:][\w-]+)*$", RegexOptions.None, ParserLimits.RegexTimeout)))
            throw new FormatException($"Unsupported selector: {selector}");
        foreach (var part in parts)
        {
            foreach (Match token in Regex.Matches(part, @"[.#:][\w-]+|^[a-z][\w-]*", RegexOptions.None, ParserLimits.RegexTimeout))
            {
                if (token.Value[0] == ':' && token.Value is not (":hover" or ":active" or ":focus" or ":disabled" or ":checked" or ":selected"))
                    throw new FormatException($"Unsupported pseudo-class: {token.Value}");
                var current = Specificity;
                Specificity = token.Value[0] switch
                {
                    '#' => (current.Ids + 1, current.Classes, current.Types),
                    '.' or ':' => (current.Ids, current.Classes + 1, current.Types),
                    _ => (current.Ids, current.Classes, current.Types + 1)
                };
            }
        }
    }

    public bool Matches(UiElement element) => MatchPart(element, parts.Length - 1);

    private bool MatchPart(UiElement element, int index)
    {
        if (!MatchCompound(element, parts[index])) return false;
        if (index == 0) return true;
        for (var ancestor = element.Parent; ancestor != null; ancestor = ancestor.Parent)
            if (MatchPart(ancestor, index - 1)) return true;
        return false;
    }

    private static bool MatchCompound(UiElement element, string part)
    {
        foreach (Match token in Regex.Matches(part, @"[.#:][\w-]+|^[a-z][\w-]*", RegexOptions.None, ParserLimits.RegexTimeout))
        {
            var value = token.Value;
            var matches = value[0] switch
            {
                '#' => element.Id == value[1..],
                '.' => element.HasClass(value[1..]),
                ':' => value switch
                {
                    ":hover" => element.IsHovered,
                    ":active" => element.IsPressed,
                    ":focus" => element.IsFocused,
                    ":disabled" => element.Disabled,
                    ":checked" => element.Control?.Checked == true,
                    ":selected" => element.IsSelected || element.Parent?.Select?.SelectedOption == element,
                    _ => false
                },
                _ => element.Tag == value
            };
            if (!matches) return false;
        }
        return true;
    }
}
