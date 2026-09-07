using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace CliRelay;

public class HandlerItem
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "unknown";
    
    [JsonPropertyName("data")]
    public JsonNode? Data { get; init; }
}