using System.Runtime.ExceptionServices;
using System.Text.Json.Nodes;
using CliRelay.Attributes;
using Scriban;

namespace CliRelay.Handlers;

[CliHandler("command")]
public class CommandCliHandler(TemplateContext templateContext) : ICliHandler
{
    private record Commands(string RenderedCommand, string RawCommand);
    private IEnumerable<Commands> GetCommands(CliHandleContext context)
    {
        var payload = context.Data?.AsObject();

        var commands = payload?["commands"]?.AsArray();
        if (commands == null)
        {
            yield break;
        }

        foreach (var command in commands)
        {
            var commandStr = command?.ToString();
            if (string.IsNullOrEmpty(commandStr))
            {
                continue;
            }

            var renderedCommand = Template.Parse(commandStr).Render(templateContext);
            context.CustomData["command"] = renderedCommand;
            context.CustomData["rawCommand"] = commandStr;
            if (string.IsNullOrEmpty(renderedCommand))
            {
                continue;
            }

            yield return new Commands(renderedCommand, commandStr);
        }
    }
    public void Handle(CliHandleContext context)
    {
        foreach (var (_, rawCommand) in GetCommands(context))
        {
            if (rawCommand.StartsWith('@'))
            {
                FunctionCliHandler.Instance.Handle(context);
            }
            else
            {
                ProcessLaunchCliHandler.Instance.Handle(context);
            }
        }
    }

    public IEnumerable<DryRunEntry> GetDryRunEntries(CliHandleContext context)
    {
        foreach (var (renderedCommand, commandStr) in GetCommands(context))
        {
            if (commandStr.StartsWith('@'))
            {
                yield return new DryRunEntry("Function", "Call", renderedCommand);
            }
            else
            {
                yield return new DryRunEntry("Process", "Launch", renderedCommand);
            }
        }
    }
}