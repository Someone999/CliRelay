using System.Diagnostics.CodeAnalysis;
using System.Text;
using HsManCommonLibrary.Reader;
using HsManLexer.Rules;
using HsManLexer.Tokens;

namespace CliRelay.LexerRules;

public class FunctionNameLexerRule : ILexerRule
{
    static FunctionNameLexerRule()
    {
        CharacterRuleSet = new CharacterRuleSet();
        CharacterRuleSet.UseNoNumberFirstCharacter();
        CharacterRuleSet.UseAsciiCharset();
        CharacterRuleSet.AddInvalidChar(' ');
    }
    private static readonly CharacterRuleSet CharacterRuleSet;
    public bool TryParse(SeekableStringReader reader, [NotNullWhen(true)] out Token? token)
    {
        reader.SavePosition();
        var position = reader.Position;
        var first = reader.PeekChar();
        if (first != '@')
        {
            token = null;
            return false;
        }
        
        reader.ConsumeChars(1);
        StringBuilder builder = new StringBuilder();
        bool isFirst = true;
        while (!reader.EndOfString)
        {
            var ch =  reader.PeekChar();
            if (!CharacterRuleSet.IsValid(ch, isFirst))
            {
                break;
            }

            if (isFirst)
            {
                isFirst = false;
            }
            
            builder.Append(ch);
            reader.ConsumeChars(1);
        }

        if (builder.Length == 0)
        {
            reader.RestorePosition();
            token = null;
            return false;
        }
        
        token = new FunctionNameToken(position, builder.ToString());
        return true;
    }
}