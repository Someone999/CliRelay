using System.Text.Json.Nodes;
using CliRelay.Configs;

namespace CliRelay.Handlers;

public class CliHandleContext(RuntimeConfig runtimeRuntimeConfig, JsonObject? data)
{
    public RuntimeConfig RuntimeConfig { get; } = runtimeRuntimeConfig;
    public Dictionary<string, object> CustomData { get; } = new();
    public JsonObject? Data { get; } = data;
}
