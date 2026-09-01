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
    private ConcurrentDictionary<MethodInfo, int> _paramsCountCache = new ConcurrentDictionary<MethodInfo, int>();

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

        bool IsType<T>(ParameterInfo parameterInfo)
        {
            return parameterInfo.ParameterType == typeof(T);
        }
        
        bool IsTargetParameters(ParameterInfo[] parameterInfos)
        {
            if (parameterInfos.Length is 0 or > 2)
            {
                return false;
            }
            
            var first = parameterInfos[0];
            if (parameterInfos.Length == 1)
            {
                return IsType<string[]>(first);
            }
            
            var second = parameterInfos[1];
            return IsType<string[]>(first) && IsType<RuntimeConfig>(second);
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

    private int GetMethodParamsCount(MethodInfo methodInfo)
    {
        if (_paramsCountCache.TryGetValue(methodInfo, out var paramsCount))
        {
            return paramsCount;
        }
        
        var parameters =  methodInfo.GetParameters();
        _paramsCountCache.TryAdd(methodInfo, parameters.Length);
        return parameters.Length;
    }
    public (int, MethodInfo)? GetMethod(string name)
    {
        var method = _methods.GetValueOrDefault(name);
        if (method == null)
        {
            return null;
        }
        
        var paramsCount = GetMethodParamsCount(method);
        return (paramsCount, method);
    } 
}