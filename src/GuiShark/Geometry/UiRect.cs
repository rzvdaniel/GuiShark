namespace GuiShark;

public readonly record struct UiRect(float X, float Y, float Width, float Height)
{
    public float Right => X + Width;
    public float Bottom => Y + Height;
    public bool Contains(float x, float y) => x >= X && x < Right && y >= Y && y < Bottom;

    public UiRect Inset(Insets inset) => new(X + inset.Left, Y + inset.Top,
        Math.Max(0, Width - inset.Horizontal), Math.Max(0, Height - inset.Vertical));

    public UiRect Intersect(UiRect other)
    {
        var x = Math.Max(X, other.X);
        var y = Math.Max(Y, other.Y);
        return new(x, y, Math.Max(0, Math.Min(Right, other.Right) - x),
            Math.Max(0, Math.Min(Bottom, other.Bottom) - y));
    }
}
