namespace GuiShark;

internal static class ParserLimits
{
    // Bound each regex operation on document input; never use an infinite timeout.
    public static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);
}
