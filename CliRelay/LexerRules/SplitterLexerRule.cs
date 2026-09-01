using System.Diagnostics.CodeAnalysis;
using HsManCommonLibrary.Reader;
using HsManLexer.Lexers;
using HsManLexer.Rules;
using HsManLexer.Tokens;

namespace CliRelay.LexerRules;

public class SplitterLexerRule(HashSet<char> splitterChars) : ILexerRule
{
    public bool TryParse(SeekableStringReader reader, [NotNullWhen(true)] out Token? token)
    {
        var pos = reader.Position;
        var c = reader.PeekChar();
        if (splitterChars.Contains(c))
        {
            token = new SplitterToken(pos);
            reader.Read();
            return true;
        }

        token = null;
        return false;
    }
}