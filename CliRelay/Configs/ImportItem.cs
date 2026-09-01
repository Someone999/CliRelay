using System.Text.Json.Serialization;

namespace CliRelay.Configs;

public class ImportItem
{
    [JsonPropertyName("file")]
    public string File { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("itemName")]
    public string ItemName { get; init; } = "";
}