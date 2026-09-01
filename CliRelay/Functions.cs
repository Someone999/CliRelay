using CliRelay.Attributes;
using CliRelay.Configs;

namespace CliRelay;

public static class Functions
{
    private static void ExitIf(bool condition, params string[] messages)
    {
        if (!condition)
        {
            return;
        }

        foreach (var message in messages)
        {
            Console.WriteLine(message);
        }
        
        Environment.Exit(1);
    }
    
    [CliFunction("pause")]
    public static void Pause(string[] args, RuntimeConfig config)
    {
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }
    
    [CliFunction("sleep")]
    public static void Sleep(string[] args)
    {
        ExitIf(args.Length == 0, "Usage: @sleep <ms: int>", "Terminating...");
        ExitIf(!int.TryParse(args[0], out var ms), "Usage: @sleep <ms: int>", "Terminating...");
        Thread.Sleep(ms);
    }
    
    [CliFunction("out")]
    public static void WriteLine(string[] args)
    {
        Console.WriteLine(string.Join(' ', args));
    }
    
    [CliFunction("warn")]
    public static void WriteWarn(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(string.Join(' ', args));
        Console.ResetColor();
    }
    
    [CliFunction("err")]
    public static void WriteErr(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(string.Join(' ', args));
        Console.ResetColor();
    }

    [CliFunction("exit")]
    public static void Exit(string[] args)
    {
        if (args.Length == 0)
        {
            Environment.Exit(0);
        }
        
        if (!int.TryParse(args[0], out var exitCode))
        {
            ExitIf(true, "Usage: @exit <code>", "Terminating...");
        }
       
        Environment.Exit(exitCode);
    }

    [CliFunction("set")]
    public static void Set(string[] args, RuntimeConfig config)
    {
        ExitIf(args.Length != 2, "Usage: @set <name> <value>", "Terminating...");
        var name = args[0];
        var value = args[1];
        config.CustomVariables[name] = value;
    }
    
    [CliFunction("setEnv")]
    public static void SetEnv(string[] args, RuntimeConfig config)
    {
        ExitIf(args.Length != 2, "Usage: @set <name> <value>", "Terminating...");
        var name = args[0];
        var value = args[1];
        config.Environment[name] = value;
    }
}