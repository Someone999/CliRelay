using CliRelay.Configs;
using CliRelay.Processes;
using Serilog;
using Serilog.Core;

namespace CliRelay.Handlers;

public class ProcessLaunchCliHandler : ICliHandler
{
    private ProcessLaunchCliHandler()
    {
    }
    public void Handle(CliHandleContext context)
    {
        var command = context.CustomData["command"] as string;
        if (string.IsNullOrEmpty(command))
        {
            return;
        }
        
        Log.Logger.Verbose("[ProcessLaunchCliHandler] Start: {0}", command);
        ProcessLauncher.LaunchProcess(command, context);
        Log.Logger.Verbose("[ProcessLaunchCliHandler] End");
    }

    public IEnumerable<DryRunEntry> GetDryRunEntries(CliHandleContext context)
    {
        return [];
    }

    public static ICliHandler Instance { get; } = new ProcessLaunchCliHandler();
}