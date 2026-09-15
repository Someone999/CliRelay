namespace CliRelay.Configs.Resolvers;

public class RawConfigItemLoader : IConfigItemLoader<(Dictionary<string, RawConfig>, string), RawConfig>
{
    public RawConfig? LoadConfig((Dictionary<string, RawConfig>?, string) content, ProgramCommandArguments arguments)
    {
        var (cfgTable, itemName) =  content;
        if (cfgTable == null)
        {
            return null;
        }
        
        if (!cfgTable.TryGetValue(itemName, out var rawConfig))
        {
            return null;
        }
        
        if (cfgTable.TryGetValue("global",  out var globalConfig))
        {
            rawConfig.GlobalConfig = globalConfig;
        }

        return rawConfig;
    }
}