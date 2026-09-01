using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

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
    
    public static void LaunchProcess(string commandStr, Dictionary<string, object?> config)
    {
        var tokens = Lexers.CliArgumentLexer.Tokenize(commandStr).Select(t => t.Text).ToList();
        var cmd = tokens[0];
        bool createWindow = HasNonNullValue(config, "createWindow", out bool v) && v;
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
        
        var process = Process.Start(processStartInfo);
        if (process == null)
        {
            Console.WriteLine("Failed to launch process.");
            return;
        }
        
        Console.InputEncoding = encoding;
        Console.OutputEncoding = encoding;
        
        process.EnableRaisingEvents = true;
        process.Exited += (sender, args) =>
        {
            if (process.ExitCode == 0)
            {
                return;
            }
            
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Process failed with exit code: " + process.ExitCode);
        };

        if (HasNonNullValue<bool>(config, "waitForExit", out var waitForExit) && waitForExit)
        {
            process.WaitForExit();
        }
    }
}