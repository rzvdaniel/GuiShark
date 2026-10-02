using System.Text.Json;

namespace GuiShark.AppProtocol;

public sealed record AppMessage(string Type, string? Id = null, string? Value = null)
{
    public const int Version = 1;

    public string ToJson() => JsonSerializer.Serialize(this);

    public static AppMessage Parse(string json) =>
        JsonSerializer.Deserialize<AppMessage>(json) ?? throw new InvalidDataException("Empty application message.");
}
