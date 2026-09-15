using System.CommandLine;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CliRelay.Configs;
using CliRelay.Configs.Resolvers;
using CliRelay.Handlers;
using Jint;
using Scriban;
using Scriban.Runtime;
using Serilog;

namespace CliRelay;

class Program
{
    static void Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var configFileOption = new Option<string>("configFile","-c", "--config-file");
        var configItemNameOption = new Option<string>("itemName", "-i", "--item-name");
        var dryRunOption = new  Option<bool>("dryRun", "-d", "--dry-run");
        var verboseOption = new  Option<bool>("verbose", "-v", "--verbose");
        var arguments = new Argument<string[]>("arguments");
        
        var rootCommand = new RootCommand("Relay commands")
        {
            Description = "Relay commands"
        };
        
        rootCommand.Options.Add(configFileOption);
        rootCommand.Options.Add(configItemNameOption);
        rootCommand.Options.Add(dryRunOption);
        rootCommand.Options.Add(verboseOption);
        rootCommand.Arguments.Add(arguments);
        
        var parseResult = rootCommand.Parse(args);
        GlobalConfig.Verbose = parseResult.GetValue<bool>("verbose");
        var configFile = parseResult.GetRequiredValue<string>("configFile");
        var loggerCfg = new LoggerConfiguration();
        loggerCfg.WriteTo.Console();
        if (GlobalConfig.Verbose)
        {
            loggerCfg.MinimumLevel.Verbose();
        }
        else
        {
            loggerCfg.MinimumLevel.Information();
        }
        
        Log.Logger = loggerCfg.CreateLogger();
        Log.Logger.Verbose("[Main] Loading config for {0}...", configFile);
        var content = File.ReadAllText(configFile);
        Log.Logger.Verbose("[Main] File loaded");
        Log.Logger.Verbose("[Main] Config file loaded");
        JsonRawConfigResolver resolver = new JsonRawConfigResolver();
        var cfg = resolver.Resolve(content);
        Log.Logger.Verbose("[Main] Config ok");
        var itemName = parseResult.GetRequiredValue<string>("itemName");
        Log.Logger.Verbose("[Main] Config item ok");
        var progArgs = parseResult.GetValue<string[]>("arguments") ?? [];
        var innerArgs = ProgramCommandArguments.Parse(progArgs);
        Log.Logger.Verbose("[Main] Passthrough arguments parsed");
        var loader = new RawConfigItemLoader();
        var generator = new DefaultRuntimeConfigGenerator();
        var item = loader.LoadConfig((cfg, itemName), innerArgs);
        if (item == null)
        {
            Log.Logger.Error("Failed to  load config item");
            return;
        }
        var runtimeConfig = generator.Generate(item, innerArgs);
        Log.Logger.Verbose("[Main] Config merge ok");
        var templateContext = new TemplateContext();
        templateContext.PushGlobal(new ScriptObject
        {
            ["args"] = runtimeConfig.Arguments.AsReadOnly(),
            ["env"] =  runtimeConfig.Environment.AsReadOnly(),
            ["consts"] = runtimeConfig.Consts.AsReadOnly(),
            ["vars"] = runtimeConfig.CustomVariables.AsReadOnly(),
            ["active_process"] = Global.ActiveProcess,
            ["history_processes"] = Global.HistoryProcesses
        });
        
        
        var isDryRun = parseResult.GetValue<bool>("dryRun");
        if (isDryRun)
        {
            DryRun(runtimeConfig);
            return;
        }
        
        CliHandlerManager handlerManager = new();
        CliFunctionManager functionManager = new();
        functionManager.ScanCommands();
        handlerManager.ScanHandlers();
        handlerManager.RegisterHandler("command", new CommandCliHandler(templateContext));
        Log.Logger.Verbose("[Main] Command scanned");
        foreach (var handlerItem in runtimeConfig.Items)
        {
            var context = new CliHandlerContext(runtimeConfig, handlerItem.Data);
            var handler = handlerManager.GetCliHandler(handlerItem.Type);
            Log.Logger.Verbose("[Main::ExecutionLoop] Start: Command type: {0}",  handlerItem.Type);
            if (handler == null)
            {
                throw new KeyNotFoundException("Specified handler not found.");
            }
            
            handler.Handle(context);
            Log.Logger.Verbose("[Main::ExecutionLoop] End");
        }
    }

    private static void DryRun(RuntimeConfig runtimeConfig)
    {
        Console.WriteLine("----Dry run----");
        StringBuilder builder = new StringBuilder();
        
        foreach (var handlerItem in runtimeConfig.Items)
        {
            builder.AppendLine(handlerItem.Type);
            builder.AppendLine("    ");
            builder.AppendLine(handlerItem.Data?.ToString());
        }
        
        Console.WriteLine(builder);
        Console.WriteLine("----Dry run complete----");
        Console.ReadKey(true);
    }
}