using HsManLexer.Tokens;

namespace CliRelay.LexerRules;

public class FunctionNameToken(long pos, string name) : Token(pos, name, TokenTypes.Identifier);