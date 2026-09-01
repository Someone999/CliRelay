using System.Collections;

namespace CliRelay;

public class ProgramCommandArguments : IEnumerable<(string, object)>
{
    private Dictionary<string, List<string>> _cmdArgs = new Dictionary<string, List<string>>();

    public object? this[string key]
    {
        get
        {
            var args =  _cmdArgs.GetValueOrDefault(key);
            if (args == null)
            {
                return null;
            }
            
            return args.Count == 1 ? args[0] : args; 
        }
    }
    public string? GetArgument(string key)
    {
        if (!_cmdArgs.TryGetValue(key, out var list))
        {
            return null;
        }

        return list.Count == 1 
            ? list[0] 
            : throw new InvalidOperationException($"Count of arguments for {key} is more than one.");
    }

    public IReadOnlyList<string>? GetArguments(string key)
    {
        return _cmdArgs.GetValueOrDefault(key);
    }

    public static ProgramCommandArguments Parse(string[] args)
    {
        ProgramCommandArguments programCommandArguments = new ProgramCommandArguments();
        Dictionary<string, List<string>> result = programCommandArguments._cmdArgs;
        string? lastKey = null;

        foreach (var arg in args)
        {
            if (arg.StartsWith("--"))
            {
                if (lastKey != null && NoArgumentForKey(lastKey))
                {
                    throw new FormatException($"Value is missing for key \"{lastKey}\"");
                }
            
                lastKey = arg[2..];
                
                if (!result.ContainsKey(lastKey))
                {
                    result[lastKey] = new List<string>();
                }
                continue;
            }
            
            if (lastKey == null)
            {
                continue; 
            }
            
            result[lastKey].Add(arg);
        }
        
        return NoArgumentForKey(lastKey)
            ? throw new FormatException($"Value is missing for key \"{lastKey}\"") 
            : programCommandArguments;

        bool NoArgumentForKey(string? key)
        {
            if (key == null)
            {
                return false;
            }
            
            return result.TryGetValue(key, out var list) && list.Count == 0;
        }
    }

    public IEnumerator<KeyValuePair<string, List<string>>> GetRawEnumerator()
    {
        return _cmdArgs.GetEnumerator();
    }


    public IEnumerator<(string, object)> GetEnumerator()
    {
        foreach (var cmdArg in _cmdArgs)
        {
            if (cmdArg.Value.Count == 1)
            {
                yield return (cmdArg.Key, cmdArg.Value[0]);
            }
            else
            {
                yield return (cmdArg.Key, cmdArg.Value);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}