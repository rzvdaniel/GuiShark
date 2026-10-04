using System.Text.Json;

namespace GuiShark.AppProtocol;

public sealed record AppMessage(
    string Type,
    string? Id = null,
    string? Value = null,
    string? Key = null,
    string? Pixels = null,
    float X = 0,
    float Y = 0,
    float Delta = 0,
    bool Shift = false,
    bool Command = false,
    bool Repeat = false,
    int Width = 0,
    int Height = 0,
    int PixelWidth = 0,
    int PixelHeight = 0,
    long Sequence = 0,
    int BufferSlot = -1,
    long Generation = 0)
{
    public const int Version = 1;

    public string ToJson() => JsonSerializer.Serialize(this);

    public static AppMessage Parse(string json) =>
        JsonSerializer.Deserialize<AppMessage>(json) ?? throw new InvalidDataException("Empty application message.");
}
