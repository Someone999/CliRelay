using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace CliRelay.Configs;

public class RawConfig
{
    [JsonPropertyName("global")]
    public RawConfig? GlobalConfig { get; private set; }
    
    [JsonPropertyName("env")]
    public Dictionary<string, string?> Environment { get; init; } = new Dictionary<string, string?>();

    [JsonPropertyName("consts")]
    public Dictionary<string, string?> Consts { get; init; } = new Dictionary<string, string?>();

    [JsonPropertyName("config")]
    public Dictionary<string, object?> Config { get; init; } = new Dictionary<string, object?>();

    [JsonPropertyName("argMap")]
    public Dictionary<string, string?> Map { get; init; } = new Dictionary<string, string?>();

    [JsonPropertyName("commands")]
    public List<string> Commands { get; init; } = new List<string>();
    
    [JsonPropertyName("import")]
    public List<ImportItem> ImportItems { get; init; } = new List<ImportItem>();
    
    public static RawConfig? FromJsonObject(JsonObject root, string itemName)
    {
        var item = root[itemName]?.AsObject();
        var configModel = item?.Deserialize<RawConfig>();
        if (configModel == null || item == null)
        {
            return null;
        }
        
        configModel.GlobalConfig = root["global"]?.AsObject().Deserialize<RawConfig>();
        return configModel;
    }
}