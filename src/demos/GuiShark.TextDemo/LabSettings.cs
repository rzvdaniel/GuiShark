using GuiShark.OpenGL;

namespace GuiShark.TextDemo;

internal sealed class LabSettings
{
    public int Mode { get; set; } // 0 compares all; 1..4 select a backend.
    public float Size { get; set; } = 14;
    public float Density { get; set; } = 1;
    public bool Snap { get; set; } = true;
    public TextHinting Hinting { get; set; } = TextHinting.Normal;
    public TextSampling Sampling { get; set; } = TextSampling.Linear;
    public bool Bold { get; set; }
    public bool Shadow { get; set; }
    public bool Fractional { get; set; } = true;
    public int Background { get; set; }
    public int Color { get; set; }
    public int Zoom { get; set; } = 2;
    public TextRenderOptions TextOptions => new(Snap, Hinting, Sampling);
    public string Foreground => Color switch
    {
        1 => "#e9ba70", 2 => "#77d5c3", _ => Background == 1 ? "#152433" : "#ecf2fa"
    };
    public string BackgroundName => Background switch { 1 => "Light", 2 => "Moving hills", _ => "Dark" };
}
