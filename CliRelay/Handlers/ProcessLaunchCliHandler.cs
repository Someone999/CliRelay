using CliRelay.Configs;
using CliRelay.Processes;

namespace CliRelay.Handlers;

public class ProcessLaunchCliHandler : ICliHandler
{
    private ProcessLaunchCliHandler()
    {
    }
    public void Handle(CliHandlerContext context)
    {
        var command = context.CustomData["command"] as string;
        if (string.IsNullOrEmpty(command))
        {
            return;
        }
        
        ProcessLauncher.LaunchProcess(command, context.RuntimeConfig.Config);
    }
    
    public static ICliHandler Instance { get; } = new ProcessLaunchCliHandler();
}