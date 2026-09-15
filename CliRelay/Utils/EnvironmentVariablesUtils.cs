using System.Collections;

namespace CliRelay.Utils;

public static class EnvironmentVariablesUtils
{
    public static Dictionary<string, string?> GetSystemEnvironmentVariables()
    {
        var proxyDictionary = new Dictionary<string, string?>();
        var env = Environment.GetEnvironmentVariables();
        foreach (DictionaryEntry entry in env)
        {
            var key = entry.Key;
            var value = entry.Value;
            if (key is not string k || value is not string v)
            {
                continue;
            }
            
            proxyDictionary[k] = v;
        }
        
        return proxyDictionary;
    }
}