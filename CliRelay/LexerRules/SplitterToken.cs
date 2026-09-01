using HsManLexer.Tokens;

namespace CliRelay.LexerRules;

public class SplitterToken(long position) : IgnoredToken(position);