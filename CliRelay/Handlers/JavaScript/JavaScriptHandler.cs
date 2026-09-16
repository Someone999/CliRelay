using CliRelay.Configs;
using Jint;

namespace CliRelay.Handlers.JavaScript;

public class JavaScriptHandler(RuntimeConfig runtimeConfig) : ICliHandler
{
    private readonly Engine _engine = new Engine();

    private void ExecuteJavaScript(string script)
    {
        _engine.Execute(script);
    }

    private void ExecuteJavaScriptFile(string file)
    {
        var content = File.ReadAllText(file);
        ExecuteJavaScript(content);
    }
    
    public void Handle(CliHandlerContext context)
    {
        var handler = context.Payload;
        if (handler == null)
        {
            return;
        }

        var type = handler["type"]?.ToString();
        if (string.IsNullOrEmpty(type))
        {
            return;
        }

        switch (type)
        {
            case "code":
                var code = handler["code"]?.ToString();
                if (string.IsNullOrEmpty(code))
                {
                    return;
                }
                
                ExecuteJavaScript(code);
                break;
            case "file":
                var fileName = handler["file"]?.ToString();
                if (string.IsNullOrEmpty(fileName))
                {
                    return;
                }
                
                ExecuteJavaScriptFile(fileName);
                break;
            default:
                throw new NotSupportedException("Not supported handler type: " + type);
        }
    }
}