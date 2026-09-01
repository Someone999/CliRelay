using System.CommandLine;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CliRelay.Configs;
using CliRelay.Handlers;
using Scriban;
using Scriban.Runtime;

namespace CliRelay;

class Program
{
    static void Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var configFileOption = new Option<string>("configFile","-c", "--config-file");
        var configItemNameOption = new Option<string>("itemName", "-i", "--item-name");
        var arguments = new Argument<string[]>("arguments");
        
        var rootCommand = new RootCommand("Relay commands")
        {
            Description = "Relay commands"
        };
        
        rootCommand.Options.Add(configFileOption);
        rootCommand.Options.Add(configItemNameOption);
        rootCommand.Arguments.Add(arguments);
        
        var r = rootCommand.Parse(args);
        var configFile = r.GetRequiredValue<string>("configFile");
        var content = File.ReadAllText(configFile);
        var node = JsonNode.Parse(content);
        if (node is not JsonObject jsonObject)
        {
            Console.WriteLine("Invalid json file");
            return;
        }

        var itemName = r.GetRequiredValue<string>("itemName");
        var rawConfig = RawConfig.FromJsonObject(jsonObject, itemName);
        if (rawConfig is null)
        {
            Console.WriteLine("Failed to parse config.");
            return;
        }
        
        var progArgs = r.GetValue<string[]>("arguments") ?? [];
        var innerArgs = ProgramCommandArguments.Parse(progArgs);
        
        var runtimeConfig = ConfigLoader.ResolveConfig(rawConfig, innerArgs);
        var templateContext = new TemplateContext();
        templateContext.PushGlobal(new ScriptObject
        {
            ["args"] = runtimeConfig.Arguments.AsReadOnly(),
            ["env"] =  runtimeConfig.Environment.AsReadOnly(),
            ["consts"] = runtimeConfig.Consts.AsReadOnly(),
            ["vars"] = runtimeConfig.CustomVariables.AsReadOnly()
        });
        
        foreach (var command in runtimeConfig.Commands)
        {
            var renderedCommand = Template.Parse(command).Render(templateContext);
            if (string.IsNullOrEmpty(renderedCommand))
            {
                continue;
            }
            
            if (command.StartsWith('@'))
            {
                FunctionHandler.Instance.Handle(renderedCommand, runtimeConfig);
            }
            else
            {
                ProcessLaunchHandler.Instance.Handle(renderedCommand, runtimeConfig);
            }
        }
    }
}