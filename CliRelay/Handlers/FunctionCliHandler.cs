using CliRelay.Configs;
using CliRelay.LexerRules;
using HsManLexer.Lexers;
using HsManLexer.Rules;
using Serilog;

namespace CliRelay.Handlers;

public class FunctionCliHandler : ICliHandler
{
    private CliFunctionManager _functionManager;
    private static readonly ILexer Lexer = Lexers.ArgumentLexer;
    

    private FunctionCliHandler()
    {
        _functionManager = new CliFunctionManager();
        _functionManager.ScanCommands();
    }
    
    public void Handle(CliHandlerContext context)
    {
        
        var command = context.CustomData["command"] as string;
        if (string.IsNullOrEmpty(command))
        {
            return;
        }
        Log.Logger.Verbose("[FunctionCliHandler] Start: {0}", command);
        var tokens = Lexer.Tokenize(command).ToList();
        Log.Logger.Verbose("[FunctionCliHandler] Tokenized");
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
            2 => [args, context.RuntimeConfig],
            _ => throw new InvalidOperationException("Invalid function parameters count")
        };
        
        Log.Logger.Verbose("[FunctionCliHandler] About to run");
        method.Invoke(null, passArgs);
        Log.Logger.Verbose("[FunctionCliHandler] End");
    }
    
    public static ICliHandler Instance { get; } = new FunctionCliHandler();
}