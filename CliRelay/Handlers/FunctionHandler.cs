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
        var funcTuple = _functionManager.GetMethod(funcName);
        if (funcTuple == null)
        {
            throw new MissingMethodException($"Function \"{funcName}\" not found");
        }
        
        var (pCount, method) =  funcTuple.Value;
        object?[] passArgs = pCount switch
        {
            1 => [args],
            2 => [args, config],
            _ => throw new InvalidOperationException("Invalid function parameters count")
        };

        method.Invoke(config, passArgs);
    }
    
    public static IHandler Instance { get; } = new FunctionHandler();
}