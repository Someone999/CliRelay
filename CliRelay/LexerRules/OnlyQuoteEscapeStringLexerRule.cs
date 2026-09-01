using System.Diagnostics.CodeAnalysis;
using System.Text;
using HsManCommonLibrary.Reader;
using HsManLexer.Rules;
using HsManLexer.Tokens;

namespace CliRelay.LexerRules;

public class OnlyQuoteEscapeStringLexerRule : ILexerRule
{
    public bool TryParse(SeekableStringReader reader, [NotNullWhen(true)] out Token? token)
    {
        StringBuilder builder = new StringBuilder();
        var p = reader.PeekChar();
        if (p != '"')
        {
            token = null;
            return false;
        }

        long pos = reader.Position;
        reader.Read();
        while (!reader.EndOfString)
        {
            p = reader.PeekChar();
            switch (p)
            {
                case '"':
                    reader.Read();
                    token = new Token(pos, builder.ToString(), TokenTypes.String);
                    return true;
                case '\\':
                    var next = reader.PeekChar(1);
                    if (next != '"')
                    {
                        builder.Append(reader.ReadChar());
                        break;
                    }
                    
                    reader.ConsumeChars(2);
                    builder.Append('"');
                    break;
                default:
                    builder.Append(p);
                    reader.Read();
                    break;
                
            }
        }
        
        throw new FormatException("Unclosed quotation mark");
    }
}