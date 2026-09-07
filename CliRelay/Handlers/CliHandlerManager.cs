using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CliRelay.Attributes;

namespace CliRelay.Handlers;

public class CliHandlerManager
{
    private ConcurrentDictionary<string, ICliHandler> _cliHandlers = new ConcurrentDictionary<string, ICliHandler>();

    private bool IsTargetType(Type type, [NotNullWhen(true)] out CliHandlerAttribute? attribute)
    {
        if (type.IsNotPublic || type.IsAbstract || type.IsInterface)
        {
            attribute = null;
            return false;
        }

        var attr = type.GetCustomAttribute<CliHandlerAttribute>();
        if (attr == null)
        {
            attribute = null;
            return false;
        }
        
        attribute = attr;
        return true;
    }
    
    private void ScanForAssembly(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (!IsTargetType(type, out var attribute))
            {
                continue;
            }

            var name = attribute.Name;
            var cons = type.GetConstructor([]);
            if (cons == null)
            {
                continue;
            }

            var ins = cons.Invoke(null);
            if (ins is not ICliHandler handler)
            {
                continue;
            }
            
            _cliHandlers.TryAdd(name, handler);
        }
    }

    public void ScanHandlers()
    {
        ScanForAssembly(Assembly.GetExecutingAssembly());
    }
    
    public ICliHandler? GetCliHandler(string name) => _cliHandlers.GetValueOrDefault(name);

    public void RegisterHandler(string name, ICliHandler handler)
    {
        _cliHandlers.TryAdd(name, handler);
    }
}