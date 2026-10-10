using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using CliRelay.Handlers;
using CliRelay.Utils;
using Serilog;

namespace CliRelay.Processes;


public static class ProcessLauncher
{
    private static bool HasNonNullValue<T>(Dictionary<string, object?> arguments, string key,
        [NotNullWhen(true)] out T? value)
    {
        
        if (!arguments.TryGetValue(key, out var s))
        {
            value = default;
            return false;
        }
        
        try
        {
            if (s is JsonElement jsonElement)
            {
                value = jsonElement.Deserialize<T>();
                return value != null;
            }

            if (s is not T t)
            {
                value = default;
                return false;
            }
            
            value = t;
            return true;
        }
        catch (Exception)
        {
            value = default;
            return false;
        }
    }

    
    
    public static void LaunchProcess(string commandStr, CliHandleContext handleContext)
    {
        var config = handleContext.RuntimeConfig.Config;
        var tokens = Lexers.CliArgumentLexer.Tokenize(commandStr).Select(t => t.Text).ToList();
        var cmd = tokens[0];
        bool createWindow = true;
        if(HasNonNullValue(config, "createWindow", out bool v))
        {
            createWindow = v;
        }
        
        var processStartInfo = new ProcessStartInfo(cmd)
        {
            UseShellExecute = false,
            CreateNoWindow = !createWindow,
        };
        
        for (int i = 1; i < tokens.Count; i++)
        {
            processStartInfo.ArgumentList.Add(tokens[i]);
        }

        if (HasNonNullValue<string>(config, "workingDirectory", out var workingDir))
        {
            processStartInfo.WorkingDirectory = workingDir;
        }

        Encoding encoding = Encoding.UTF8;
        if (HasNonNullValue<string>(config, "encoding", out var encodingName))
        {
            encoding = Encoding.GetEncoding(encodingName);
        }
        
        if (HasNonNullValue<RedirectionInfo>(config, "redirect", out var redirect))
        {
            if (redirect.RedirectStandardInput)
            {
                processStartInfo.RedirectStandardInput = true;
                processStartInfo.StandardInputEncoding = encoding;
            }
            
            if (redirect.RedirectStandardOutput)
            {
                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.StandardOutputEncoding = encoding;
            }
            
            if (redirect.RedirectStandardError)
            {
                processStartInfo.RedirectStandardError = true;
                processStartInfo.StandardErrorEncoding = encoding;
            }
        }
        else
        {
            Console.InputEncoding = encoding;
            Console.OutputEncoding = encoding;
        }
        
        DictionaryUtils.Merge(processStartInfo.Environment, handleContext.RuntimeConfig.Environment); 
        var process = Process.Start(processStartInfo);
        if (process == null)
        {
            Log.Logger.Error("Failed to launch process.");
            return;
        }

        Log.Logger.Verbose("[ProcessLaunchCliHandler] Process ok");
        process.EnableRaisingEvents = true;
        process.Exited += (sender, args) =>
        {
            Log.Logger.Verbose("[ProcessLaunchCliHandler] Exited code: {0}]", process.ExitCode);
            if (process.ExitCode == 0)
            {
                if (GlobalConfig.Verbose)
                {
                    Console.ReadKey(false);
                }
                
                return;
            }
            
            Log.Logger.Error("Process failed with exit code: {0}", process.ExitCode);
            Global.ActiveProcess.Clear();
        };

        var lastProcess = Global.ActiveProcess;
        if (lastProcess.HasValue)
        {
            Global.HistoryProcesses.Add(lastProcess.Value);
        }
        
        Global.ActiveProcess.SetValue(new ProcessWrapper(process));
        if (!HasNonNullValue<bool>(config, "waitForExit", out var waitForExit) || !waitForExit)
        {
            return;
        }
        
        Log.Logger.Verbose("[ProcessLaunchCliHandler] Waiting for exit...]");
        process.WaitForExit();
    }
}