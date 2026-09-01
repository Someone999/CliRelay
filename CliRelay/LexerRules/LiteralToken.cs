using HsManLexer.Tokens;

namespace CliRelay.LexerRules;

public class LiteralToken(long position, string text) : Token(position, text, TokenType.Create<string>("Literal"));