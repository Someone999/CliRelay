using System.Reflection;

namespace CliRelay.Reflections;

public interface ITypeFilter<in T> where T: MemberInfo
{
    bool IsTargetMember(Type type, T obj, Dictionary<string, object?> data);
}