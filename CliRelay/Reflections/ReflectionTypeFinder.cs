using System.Reflection;

namespace CliRelay.Reflections;

public interface ITypeFilter<in T> where T: MemberInfo
{
    bool IsTargetMember(Type type, T obj, Dictionary<string, object?> data);
}

public class ReflectionTypeFinder<T>(IEnumerable<ITypeFilter<T>> filters) where T: MemberInfo
{
    public List<Assembly> ReflectionAssemblies { get; } = new List<Assembly>();

    private IEnumerable<T?> FindNextInType(Type type, Dictionary<string, object?> context)
    {
        var members = type.GetMembers();
        foreach (var memberInfo in members)
        {
            if (memberInfo is not T typedMember)
            {
                continue;
            }

            var isTargetMember = filters.All(typeFilter => typeFilter.IsTargetMember(type, typedMember, context));
            if (!isTargetMember)
            {
                continue;
            }

            yield return typedMember;
        }
    }

    private IEnumerable<T?> FindNextInAssembly(Assembly assembly, Dictionary<string, object?> context)
    {
        return assembly.GetTypes().SelectMany(s => FindNextInType(s, context));
    }
    
    public IEnumerable<T> GetFilteredMembers(Dictionary<string, object?> context)
    {
        foreach (var reflectionAssembly in ReflectionAssemblies)
        {
            foreach (var item in FindNextInAssembly(reflectionAssembly, context))
            {
                if (item == null)
                {
                    continue;
                }
                
                yield return item;
            }
        }
    }
}