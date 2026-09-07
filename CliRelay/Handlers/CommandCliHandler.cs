using System.Text.Json.Nodes;
using CliRelay.Attributes;
using Scriban;

namespace CliRelay.Handlers;

[CliHandler("command")]
public class CommandCliHandler(TemplateContext templateContext) : ICliHandler
{
    public void Handle(CliHandlerContext context)
    {
        var commands = context.Payload?.AsArray();
        if (commands == null)
        {
            return;
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
            
            if (commandStr.StartsWith('@'))
            {
                FunctionCliHandler.Instance.Handle(context);
            }
            else
            {
                ProcessLaunchCliHandler.Instance.Handle(context);
            }
        }
        
    }
}