using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace CliRelay.Configs;

public class RawConfig
{
    [JsonPropertyName("global")]
    public RawConfig? GlobalConfig { get; internal set; }
    
    [JsonPropertyName("env")]
    public Dictionary<string, string?> Environment { get; init; } = new Dictionary<string, string?>();

    [JsonPropertyName("consts")]
    public Dictionary<string, string?> Consts { get; init; } = new Dictionary<string, string?>();

    [JsonPropertyName("config")]
    public Dictionary<string, object?> Config { get; init; } = new Dictionary<string, object?>();

    [JsonPropertyName("argMap")]
    public Dictionary<string, string?> Map { get; init; } = new Dictionary<string, string?>();

    [JsonPropertyName("items")]
    public List<HandlerItem> Items { get; init; } = new List<HandlerItem>();
    
    [JsonPropertyName("import")]
    public List<ImportItem> ImportItems { get; init; } = new List<ImportItem>();
    
}