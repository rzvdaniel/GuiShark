namespace GuiShark;

public readonly record struct Insets(float Top, float Right, float Bottom, float Left)
{
    public float Horizontal => Left + Right;
    public float Vertical => Top + Bottom;
    public static Insets All(float value) => new(value, value, value, value);
}
