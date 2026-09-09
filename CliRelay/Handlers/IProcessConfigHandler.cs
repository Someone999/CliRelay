using System.Diagnostics;
using CliRelay.Configs;

namespace CliRelay.Handlers;

public interface IProcessConfigHandler
{
    ProcessConfigHandleStage Stage { get; }
    void HandleConfig(string configKey, Dictionary<string, string?> config);
}