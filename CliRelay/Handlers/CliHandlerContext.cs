using System.Text.Json.Nodes;
using CliRelay.Configs;

namespace CliRelay.Handlers;

public class CliHandlerContext(RuntimeConfig runtimeRuntimeConfig, JsonNode? payload)
{
    public RuntimeConfig RuntimeConfig { get; } = runtimeRuntimeConfig;
    public Dictionary<string, object> CustomData { get; } = new();
    public JsonNode? Payload { get; } = payload;
}
