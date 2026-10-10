using System.CommandLine;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CliRelay.Configs;
using CliRelay.Configs.Resolvers;
using CliRelay.Configs.Resolvers.Generators;
using CliRelay.Configs.Resolvers.Loaders;
using CliRelay.Handlers;
using Scriban;
using Scriban.Runtime;
using Serilog;

namespace CliRelay;
public record ResolvedHandlerItem(ICliHandler Handler, CliHandleContext HandleContext, HandlerItem HandlerItem);

class Program
{
    private static IEnumerable<ResolvedHandlerItem> ResolveHandlerItems(RuntimeConfig runtimeConfig, CliHandlerManager handlerManager)
    {
        foreach (var handlerItem in runtimeConfig.Items)
        {
            var context = new CliHandleContext(runtimeConfig, handlerItem.Data);
            var handler = handlerManager.GetCliHandler(handlerItem.Type);
            if (handler == null)
            {
                throw new KeyNotFoundException("Specified handler not found.");
            }
            
            yield return new ResolvedHandlerItem(handler, context, handlerItem);
        }
    }
    
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
        
        
        CliHandlerManager handlerManager = new();
        CliFunctionManager functionManager = new();
        functionManager.ScanCommands();
        Log.Logger.Verbose("[Main] Command scanned");
        handlerManager.ScanHandlers();
        handlerManager.RegisterHandler("command", new CommandCliHandler(templateContext));
        var isDryRun = parseResult.GetValue<bool>("dryRun");
        if (isDryRun)
        {
            DryRun(runtimeConfig, handlerManager);
            return;
        }

        foreach (var (handler, context, handlerItem) in ResolveHandlerItems(runtimeConfig, handlerManager))
        {
            Log.Logger.Verbose("[Main::ExecLoop] Handler: {0} Start", handlerItem.Type);
            handler.Handle(context);
            Log.Logger.Verbose("[Main::ExecLoop] Handler: {0} End", handlerItem.Type);
        }
        
        
        
    }

    private static void DryRun(RuntimeConfig runtimeConfig, CliHandlerManager handlerManager)
    {
        Log.Logger.Information("Dry run");

        var entries = new List<DryRunEntry>();

        foreach (var (handler, context, _) in ResolveHandlerItems(runtimeConfig, handlerManager))
        {
            entries.AddRange(handler.GetDryRunEntries(context));
        }

        RenderDryRun(entries);
        Log.Logger.Information("Dry run complete. Press any key to continue...");
        Console.ReadKey(true);
    }

    private static void RenderDryRun(List<DryRunEntry> entries)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            sb.Append($"[{i + 1}] [{e.HandlerName}] {e.Verb} -> {e.Content}");
            if (e.Note != null)
            {
                sb.AppendLine($"Note: {e.Note}");
            }
            
            if (i + 1 < entries.Count)
            {
                sb.AppendLine();
            }
        }
        
        Console.WriteLine(sb);
    }
}