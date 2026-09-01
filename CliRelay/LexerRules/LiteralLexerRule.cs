using System.Diagnostics.CodeAnalysis;
using System.Text;
using HsManCommonLibrary.Reader;
using HsManLexer.Rules;
using HsManLexer.Tokens;

namespace CliRelay.LexerRules;

public class LiteralLexerRule(CharacterRuleSet ruleSet) : ILexerRule
{
    public bool TryParse(SeekableStringReader reader, [NotNullWhen(true)] out Token? token)
    {
        var ch = reader.PeekChar();
        if (!ruleSet.IsValid(ch, true) ||  ch == '"')
        {
            token = null;
            return false;
        }
        
        StringBuilder builder = new();
        var pos = reader.Position;
        while (!reader.EndOfString)
        {
            ch = reader.PeekChar();
            if (!ruleSet.IsValid(ch, false))
            {
                break;
            }

            if (ch == '"')
            {
                throw new FormatException($"Unexpected '\"' at {reader.Position + 1}");
            }
            
            builder.Append(ch);
            reader.ConsumeChars(1);
        }

        if (builder.Length == 0)
        {
            token = null;
            return false;
        }
        
        token = new LiteralToken(pos, builder.ToString());
        return true;
    }
}