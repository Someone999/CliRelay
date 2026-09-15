using CliRelay.Utils;
using Scriban;
using Scriban.Runtime;

namespace CliRelay.Configs.Resolvers;

public class DefaultRuntimeConfigGenerator : IRuntimeConfigGenerator<RawConfig>
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


    private RuntimeConfig ResolveConfig(RawConfig rawConfig, ProgramCommandArguments arguments)
    {
        Dictionary<string, string?> tmpConsts = new(); 
        Dictionary<string, string?> tmpEnv = new(); 
        var runtimeConfig = new RuntimeConfig();
        
        // 记录已加载的文件路径，防止循环引用（死循环）
        HashSet<string> visitedFiles = new(StringComparer.OrdinalIgnoreCase);

        // 1. 初始化系统环境变量
        DictionaryUtils.Merge(tmpEnv, EnvironmentVariablesUtils.GetSystemEnvironmentVariables(), false);

        // 2. 先递归加载【顶级的 global 块】里带的导入和常量
        if (rawConfig.GlobalConfig != null)
        {
            ResolveConfigRecursive(rawConfig.GlobalConfig, tmpConsts, tmpEnv, runtimeConfig, visitedFiles);
        }

        // 3. 再递归加载【当前目标命令块】自己的导入和配置
        ResolveConfigRecursive(rawConfig, tmpConsts, tmpEnv, runtimeConfig, visitedFiles);

        // 4. 处理命令行参数映射
        RawConfigLoadTools.MapArguments(rawConfig, runtimeConfig, arguments);
        
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

        DictionaryUtils.Merge(runtimeConfig.Environment, renderedEnv);
        DictionaryUtils.Merge(runtimeConfig.Consts, renderedConsts);
        
        runtimeConfig.Items.AddRange(rawConfig.Items);
        
        return runtimeConfig;
    }

    /// <summary>
    /// 递归解析辅助方法：处理单个 RawConfig 及其 ImportItems
    /// </summary>
    private void ResolveConfigRecursive(
        RawConfig currentRawConfig, 
        Dictionary<string, string?> tmpConsts, 
        Dictionary<string, string?> tmpEnv, 
        RuntimeConfig runtimeConfig, 
        HashSet<string> visitedFiles)
    {
        // 1. 先递归处理当前配置中的导入项
        foreach (var importItem in currentRawConfig.ImportItems)
        {
            var item = RawConfigLoadTools.LoadRawConfig(importItem, visitedFiles);
            if (item == null)
            {
                continue;
            }
            
            // 递归调用，把导入文件的配置合并进来
            ResolveConfigRecursive(item, tmpConsts, tmpEnv, runtimeConfig, visitedFiles);
        }

        // 2. 收集当前层的 Consts、Environment 和 Config 结构
        DictionaryUtils.Merge(tmpConsts, currentRawConfig.Consts);
        DictionaryUtils.Merge(tmpEnv, currentRawConfig.Environment);
        runtimeConfig.MergeConfig(currentRawConfig.Config);
    }

    /// <summary>
    /// 对外公开的主入口
    /// </summary>
    public RuntimeConfig Generate(RawConfig content, ProgramCommandArguments arguments)
    {
        return ResolveConfig(content, arguments);
    }
}