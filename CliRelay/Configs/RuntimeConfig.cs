namespace CliRelay.Configs;

public class RuntimeConfig
{
    public Dictionary<string, object?> Config { get; } = new Dictionary<string, object?>();
    public Dictionary<string, string?> Environment { get; init; } = new Dictionary<string, string?>();
    public Dictionary<string, string?> Consts { get; init; } = new Dictionary<string, string?>();
    public Dictionary<string, object> Arguments { get; init; } = new();
    public Dictionary<string, string> CustomVariables { get; init; } = new Dictionary<string, string>(); 
    public List<string> Commands { get; init; } = new List<string>();

    public void MergeConfig(RuntimeConfig config, bool overwrite = true)
    {
        Merge(config.Consts, ConfigType.Consts, overwrite);
        foreach (var kvp in config.Config)
        {
            if (!Config.TryAdd(kvp.Key, kvp.Value) && overwrite)
            {
                Config[kvp.Key] = kvp.Value;
            }
        }
        
        Merge(config.Environment, ConfigType.Environment, overwrite);
    }
    
    public void MergeConfig(Dictionary<string, object?> dict, bool overwrite = true)
    {
        var targetDict = Config;

        foreach (var kvp in dict)
        {
            if (!targetDict.TryAdd(kvp.Key, kvp.Value) && overwrite)
            {
                targetDict[kvp.Key] = kvp.Value;
            }
        }
    }
    
    public void Merge(Dictionary<string, string?> dict, ConfigType type, bool overwrite = true)
    {
        var targetDict = type switch
        {
            ConfigType.Environment => Environment,
            ConfigType.Consts => Consts,
            _ => throw new NotSupportedException()
        };

        foreach (var kvp in dict)
        {
            if (!targetDict.TryAdd(kvp.Key, kvp.Value) && overwrite)
            {
                targetDict[kvp.Key] = kvp.Value;
            }
        }
    }
}