using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using CliRelay.Attributes;
using CliRelay.Configs;

namespace CliRelay;

public class CliFunctionManager
{
    private ConcurrentDictionary<string, MethodInfo> _methods = new ConcurrentDictionary<string, MethodInfo>();

    private bool IsTargetCliMethod(MethodInfo methodInfo, [NotNullWhen(true)] out CliFunctionAttribute? attribute)
    {
        if (!methodInfo.IsStatic)
        {
            attribute = null;
            return false;
        }
            
        var attr = methodInfo.GetCustomAttribute<CliFunctionAttribute>();
        if (attr == null)
        {
            attribute = null;
            return false;
        }
        
        var parameters = methodInfo.GetParameters();
        if (!IsTargetParameters(parameters))
        {
            attribute = null;
            return false;
        }
        
        attribute = attr;
        return true;

        bool IsTargetParameters(ParameterInfo[] parameterInfos)
        {
            if (parameterInfos.Length != 2)
            {
                return false;
            }
            
            var first = parameterInfos[0];
            var second = parameterInfos[1];
            return first.ParameterType == typeof(string[]) && second.ParameterType == typeof(RuntimeConfig);
        }
    }
    
    private void ScanCommandsForType(Type type)
    {
        var methods =  type.GetMethods();
        foreach (var methodInfo in methods)
        {
            if (IsTargetCliMethod(methodInfo, out var attr))
            {
                _methods.TryAdd(attr.Name, methodInfo);
            }
        }
    }
    
    private void ScanCommandsForAssembly(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            ScanCommandsForType(type);
        }
    }
    
    public void ScanCommands()
    {
        var assemblies = AssemblyLoadContext.Default.Assemblies;
        foreach (var assembly in assemblies)
        {
            ScanCommandsForAssembly(assembly);
        }
    }

    public MethodInfo? GetMethod(string name) => _methods.GetValueOrDefault(name);
}