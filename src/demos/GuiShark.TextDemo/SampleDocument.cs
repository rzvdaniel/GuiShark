using System.Globalization;

namespace GuiShark.TextDemo;

internal static class SampleDocument
{
    public static UiDocument Create(IAssetSource assets, LabSettings settings, float effectiveDensity, float height = float.MaxValue)
    {
        var compact = height < 105;
        var matrix = compact
            ? string.Concat(new[] { "tiny", "small", "normal", "medium", "large", "display" }
                .Select(size => $"<div class='column' style='width: 14%;'><p class='{size}'>A</p></div>"))
            : "<div class='column'><p class='tiny'>10: Café</p><p class='large'>Aa Gg</p></div>"
                + "<div class='column'><p class='small'>12: 012345</p><p class='display'>Ag</p></div>"
                + "<div class='column'><p class='normal'>14: naïve</p><p class='medium'>Aa Gg</p></div>";
        var html = assets.ReadText("sample.html")
            .Replace("{{matrix}}", matrix)
            .Replace("{{padding}}", compact ? "0" : "8")
            .Replace("{{color}}", settings.Foreground)
            .Replace("{{accent}}", settings.Background == 1 ? "#6d4c12" : "#e9ba70")
            .Replace("{{hint-display}}", effectiveDensity >= 2 || compact ? "none" : "block")
            .Replace("{{button-display}}", height < 170 ? "none" : "block")
            .Replace("{{button-background}}", settings.Background == 1 ? "#d4deda" : "#173230")
            .Replace("{{weight}}", settings.Bold ? "700" : "400")
            .Replace("{{shadow}}", settings.Shadow ? "1px 1px #000000bb" : "none")
            .Replace("{{size}}", settings.Size.ToString(CultureInfo.InvariantCulture))
            // Deliberately half a physical pixel, regardless of density.
            .Replace("{{offset}}", (settings.Fractional ? .5f / effectiveDensity : 0).ToString(CultureInfo.InvariantCulture));
        return HtmlLoader.Load(html, assets);
    }
}
