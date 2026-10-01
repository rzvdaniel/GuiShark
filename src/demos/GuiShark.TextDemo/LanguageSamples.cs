namespace GuiShark.TextDemo;

internal sealed record LanguageSample(string Name, string Family, string Text, string ShortText);

internal static class LanguageSamples
{
    public static IReadOnlyList<LanguageSample> All { get; } =
    [
        new("Latin", "Lato", "Illl 1O0 | AV fi", "Café naïve"),
        new("CJK", "NotoJP", "緑の丘へ。こんにちは、世界！", "東京の空"),
        new("Indic", "NotoIndic", "नमस्ते दुनिया", "नमस्ते"),
        new("Arabic", "NotoArabic", "مرحباً بالعالم", "العالم"),
        new("Mixed RTL", "NotoArabic", "Hello 123 مرحباً بالعالم", "Hello مرحباً")
    ];
    public static int Parse(string name) => name switch
    {
        "latin" => 0, "cjk" => 1, "indic" => 2, "arabic" => 3, "mixed" => 4,
        _ => throw new ArgumentException("Sample: latin, cjk, indic, arabic or mixed.")
    };
}
