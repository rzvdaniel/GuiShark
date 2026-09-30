namespace GuiShark;

public enum UiControlKind { Checkbox, Radio, Range, Progress }

/// <summary>Form state independent of the window and rendering backend.</summary>
public sealed class UiControl
{
    private readonly UiElement element;
    private bool isChecked;
    private float value;
    internal UiControl(UiElement element, UiControlKind kind, string name, float minimum, float maximum, float step)
    {
        this.element = element;
        Kind = kind; Name = name; Minimum = minimum; Maximum = maximum; Step = step;
        value = minimum;
    }
    public UiControlKind Kind { get; }
    public string Name { get; }
    public float Minimum { get; }
    public float Maximum { get; }
    public float Step { get; }
    public bool IsInteractive => Kind != UiControlKind.Progress;
    public float Fraction => (Value - Minimum) / (Maximum - Minimum);
    public bool Checked
    {
        get => isChecked;
        set
        {
            if (isChecked == value) return;
            isChecked = value;
            if (value && Kind == UiControlKind.Radio && Name.Length > 0)
            {
                var root = element;
                while (root.Parent != null) root = root.Parent;
                foreach (var peer in root.DescendantsAndSelf())
                    if (peer != element && peer.Control is { Kind: UiControlKind.Radio } control && control.Name == Name)
                        control.Checked = false;
            }
            Notify();
        }
    }
    public float Value
    {
        get => value;
        set
        {
            if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            var next = Math.Clamp(value, Minimum, Maximum);
            if (Kind == UiControlKind.Range && Step > 0)
                next = Math.Clamp(Minimum + MathF.Round((next - Minimum) / Step, MidpointRounding.AwayFromZero) * Step, Minimum, Maximum);
            if (this.value == next) return;
            this.value = next;
            Notify();
        }
    }
    /// <summary>Raised for user interaction and programmatic changes.</summary>
    public event Action<UiControl>? Changed;
    internal void Activate()
    {
        if (Kind == UiControlKind.Checkbox) Checked = !Checked;
        if (Kind == UiControlKind.Radio) Checked = true;
    }
    private void Notify() { element.Invalidate(); Changed?.Invoke(this); }
}
