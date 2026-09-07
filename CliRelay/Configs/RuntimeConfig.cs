namespace CliRelay.Configs;

public class RuntimeConfig
{
    public Dictionary<string, object?> Config { get; } = new Dictionary<string, object?>();
    public Dictionary<string, string?> Environment { get; init; } = new Dictionary<string, string?>();
    public Dictionary<string, string?> Consts { get; init; } = new Dictionary<string, string?>();
    public Dictionary<string, object> Arguments { get; init; } = new();
    public Dictionary<string, string> CustomVariables { get; init; } = new Dictionary<string, string>(); 
    public List<HandlerItem> Items { get; init; } = new List<HandlerItem>();

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
}