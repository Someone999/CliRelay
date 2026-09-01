using System.Diagnostics;
using CliRelay.Configs;

namespace CliRelay.Handlers;

public enum ProcessConfigHandleStage
{
    Pre, Post
}

public interface IProcessConfigHandler
{
    ProcessConfigHandleStage Stage { get; }
    void HandleConfig(string configKey, Dictionary<string, string?> config);
}