using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;
using Scriban;
using Scriban.Runtime;

namespace CliRelay.Configs;

public static class ConfigLoader
{
    private static Dictionary<string, string?> RenderStringsDict(Dictionary<string, string?> commands, TemplateContext context)
    {
        Dictionary<string, string?> rendered = new();
        foreach (var cmd in commands)
        {
            var templateKey = cmd.Key;
            var templateStr = cmd.Value;
            if (templateStr == null)
            {
                continue;
            }
            
            var template = Template.Parse(templateStr);
            rendered.Add(templateKey, template.Render(context));
        }
        
        return rendered;
    }

    private static Dictionary<string, string?> GetSystemEnvironmentVariables()
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

    private static void Merge(Dictionary<string, string?> left, Dictionary<string, string?> right, bool overwrite = true)
    {
        if (right.Count == 0)
        {
            return;
        }

        foreach (var kvp in right)
        {
            if (!left.TryAdd(kvp.Key, kvp.Value) && overwrite)
            {
                left[kvp.Key] = kvp.Value;
            }
        }
    }

    private static void MergeNonTemplateAndRemove(Dictionary<string, string?> left, Dictionary<string, string?> right, bool overwrite = true)
    {
        var keysToRemove = new List<string>();
        foreach (var kvp in right)
        {
            var templateStr = kvp.Value;
            if (string.IsNullOrEmpty(templateStr))
            {
                continue;
            }

            var isTemplate = templateStr.Contains("{{", StringComparison.Ordinal);
            if (isTemplate)
            {
                continue;
            }
            
            if (!left.TryAdd(kvp.Key, kvp.Value) && overwrite)
            {
                left[kvp.Key] = kvp.Value;
            }
            
            keysToRemove.Add(kvp.Key);
        }

        foreach (var key in keysToRemove)
        {
            right.Remove(key);
        }
    }

    /// <summary>
    /// 对外公开的主入口
    /// </summary>
    public static RuntimeConfig ResolveConfig(RawConfig rawConfig, ProgramCommandArguments arguments)
    {
        Dictionary<string, string?> tmpConsts = new(); 
        Dictionary<string, string?> tmpEnv = new(); 
        var runtimeConfig = new RuntimeConfig();
        
        // 记录已加载的文件路径，防止循环引用（死循环）
        HashSet<string> visitedFiles = new(StringComparer.OrdinalIgnoreCase);

        // 1. 初始化系统环境变量
        Merge(tmpEnv, GetSystemEnvironmentVariables(), false /*overwrite*/);

        // 2. 先递归加载【顶级的 global 块】里带的导入和常量
        if (rawConfig.GlobalConfig != null)
        {
            ResolveConfigRecursive(rawConfig.GlobalConfig, tmpConsts, tmpEnv, runtimeConfig, visitedFiles);
        }

        // 3. 再递归加载【当前目标命令块】自己的导入和配置
        ResolveConfigRecursive(rawConfig, tmpConsts, tmpEnv, runtimeConfig, visitedFiles);

        // 4. 处理命令行参数映射
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
        
        // 5. 最终的无模板变量处理与整体渲染
        MergeNonTemplateAndRemove(runtimeConfig.Consts, tmpConsts);
        MergeNonTemplateAndRemove(runtimeConfig.Environment, tmpEnv);
        
        var templateContext = new TemplateContext();
        templateContext.PushGlobal(new ScriptObject
        {
            ["args"] = runtimeConfig.Arguments,
            ["env"] =  runtimeConfig.Environment,
            ["consts"] = runtimeConfig.Consts,
            ["vars"] = runtimeConfig.CustomVariables
        });

        var renderedEnv = RenderStringsDict(tmpEnv, templateContext);
        var renderedConsts = RenderStringsDict(tmpConsts, templateContext);
        
        Merge(runtimeConfig.Environment, renderedEnv);
        Merge(runtimeConfig.Consts, renderedConsts);
        
        runtimeConfig.Commands.AddRange(rawConfig.Commands);
        
        return runtimeConfig;
    }

    /// <summary>
    /// 递归解析辅助方法：处理单个 RawConfig 及其 ImportItems
    /// </summary>
    private static void ResolveConfigRecursive(
        RawConfig currentRawConfig, 
        Dictionary<string, string?> tmpConsts, 
        Dictionary<string, string?> tmpEnv, 
        RuntimeConfig runtimeConfig, 
        HashSet<string> visitedFiles)
    {
        // 1. 先递归处理当前配置中的导入项
        foreach (var importItem in currentRawConfig.ImportItems)
        {
            var file = importItem.File;
            
            // 规范化路径以防重复引用判定失败
            var fullPath = Path.GetFullPath(file);
            if (!visitedFiles.Add(fullPath))
            {
                // 已经加载过该文件，直接跳过（防止循环导入死循环）
                continue;
            }

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Imported config file not found: {fullPath}");
            }

            var content = File.ReadAllText(fullPath);
            var jsonObject = JsonSerializer.Deserialize<JsonObject>(content);
            if (jsonObject == null)
            {
                continue;
            }
            
            var importConfigModel = RawConfig.FromJsonObject(jsonObject, importItem.ItemName);
            if (importConfigModel == null)
            {
                continue;
            }

            // 递归调用，把导入文件的配置合并进来
            ResolveConfigRecursive(importConfigModel, tmpConsts, tmpEnv, runtimeConfig, visitedFiles);
        }

        // 2. 收集当前层的 Consts、Environment 和 Config 结构
        Merge(tmpConsts, currentRawConfig.Consts);
        Merge(tmpEnv, currentRawConfig.Environment);
        runtimeConfig.MergeConfig(currentRawConfig.Config);
    }
}