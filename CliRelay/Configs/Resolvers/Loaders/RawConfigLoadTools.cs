namespace CliRelay.Configs.Resolvers;

static class RawConfigLoadTools
{
    private static readonly JsonRawConfigResolver Resolver = new JsonRawConfigResolver();
    public static RawConfig? LoadRawConfig(ImportItem importItem, HashSet<string> visitedFiles)
    {
        var file = importItem.File;
            
        // 规范化路径以防重复引用判定失败
        var fullPath = Path.GetFullPath(file);
        if (!visitedFiles.Add(fullPath))
        {
            // 已经加载过该文件，直接跳过（防止循环导入死循环）
            return null;
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Imported config file not found: {fullPath}");
        }

        var jsonObject = Resolver.Resolve(File.ReadAllText(fullPath));
        var item = jsonObject[importItem.Type];
        return item;
    }

    public static void MapArguments(RawConfig rawConfig, 
        RuntimeConfig runtimeConfig, ProgramCommandArguments arguments)
    {
        HashSet<string> mappedArguments = new HashSet<string>();
        foreach (var kvp in rawConfig.Map)
        {
            var scriptArgumentKey = kvp.Key;
            var realArgumentKey = kvp.Value;
            
            if (string.IsNullOrEmpty(realArgumentKey))
            {
                continue;
            }
            
            var realArgument = arguments[realArgumentKey];
            if (realArgument == null)
            {
                continue;
            }
            
            if (!mappedArguments.Add(realArgumentKey))
            {
                throw new InvalidDataException($"Duplicate argument key: {realArgumentKey}");
            }
            
            runtimeConfig.Arguments[scriptArgumentKey] = realArgument;
        }

        foreach (var (key, val) in arguments)
        {
            if (mappedArguments.Contains(key))
            {
                continue;
            }
            
            runtimeConfig.Arguments[key] = val;
        }
    }
}