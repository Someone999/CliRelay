using CliRelay.LexerRules;
using HsManLexer.Lexers;
using HsManLexer.Rules;

namespace CliRelay;

public static class Lexers
{
    private static readonly Lazy<ILexer> ArgumentLexerLazy = new Lazy<ILexer>(() =>
    {
        var chRuleset = new CharacterRuleSet();
        chRuleset.AddInvalidChar(' ');
        chRuleset.AddInvalidFirstChar(' ');
        chRuleset.AddInvalidFirstChar('"');
        
        RuleBasedLexer lexer = new RuleBasedLexer()
        {
            Rules =
            {
                new FunctionNameLexerRule(),
                new SplitterLexerRule([' ']),
                new LiteralLexerRule(chRuleset),
                new OnlyQuoteEscapeStringLexerRule(),
            }
        };
        
        return lexer;
    });
    
    private static readonly Lazy<ILexer> CliArgumentLexerLazy = new Lazy<ILexer>(() =>
    {
        var chRuleset = new CharacterRuleSet();
        chRuleset.AddInvalidChar(' ');
        chRuleset.AddInvalidFirstChar(' ');
        chRuleset.AddInvalidFirstChar('"');
        
        RuleBasedLexer lexer = new RuleBasedLexer()
        {
            Rules =
            {
                new SplitterLexerRule([' ']),
                new LiteralLexerRule(chRuleset),
                new OnlyQuoteEscapeStringLexerRule(),
                
            }
        };
        
        return lexer;
    });
    
    public static ILexer ArgumentLexer => ArgumentLexerLazy.Value;
    public static ILexer CliArgumentLexer => CliArgumentLexerLazy.Value;
}