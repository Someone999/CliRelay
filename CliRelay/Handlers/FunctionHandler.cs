using CliRelay.Configs;
using CliRelay.LexerRules;
using HsManLexer.Lexers;
using HsManLexer.Rules;

namespace CliRelay.Handlers;

public class FunctionHandler : IHandler
{
    private CliFunctionManager _functionManager;
    private static readonly ILexer Lexer = Lexers.ArgumentLexer;
    

    private FunctionHandler()
    {
        _functionManager = new CliFunctionManager();
        _functionManager.ScanCommands();
    }
    
    public void Handle(string command, RuntimeConfig config)
    {
        var tokens = Lexer.Tokenize(command).ToList();
        var funcName =  tokens[0].Text;
        var args = tokens.GetRange(1, tokens.Count - 1).Select(t => t.Text).ToArray();
        var func = _functionManager.GetMethod(funcName);
        if (func == null)
        {
            throw new MissingMethodException($"Function \"{funcName}\" not found");
        }

        func.Invoke(null, [args, config]);
    }
    
    public static IHandler Instance { get; } = new FunctionHandler();
}