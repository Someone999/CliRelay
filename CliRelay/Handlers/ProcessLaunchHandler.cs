using CliRelay.Configs;
using CliRelay.Processes;

namespace CliRelay.Handlers;

public class ProcessLaunchHandler : IHandler
{
    private ProcessLaunchHandler()
    {
    }
    public void Handle(string command, RuntimeConfig config)
    {
        ProcessLauncher.LaunchProcess(command, config.Config);
    }
    
    public static IHandler Instance { get; } = new ProcessLaunchHandler();
}