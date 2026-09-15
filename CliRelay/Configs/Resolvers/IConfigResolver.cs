using System.Collections;
using System.Text.Json.Nodes;

namespace CliRelay.Configs.Resolvers;

public interface IConfigResolver<T>
{
    public Dictionary<string, T>? Resolve(string content);
}