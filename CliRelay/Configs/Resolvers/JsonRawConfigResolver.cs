using System.Text.Json;

namespace CliRelay.Configs.Resolvers;

public class JsonRawConfigResolver : IConfigResolver<RawConfig>
{
    public Dictionary<string, RawConfig> Resolve(string content)
    {
        return  JsonSerializer.Deserialize<Dictionary<string, RawConfig>>(content) 
                ?? throw new InvalidDataException("JSON could not be parsed as Dictionary<string, RawConfig>");
    }
}