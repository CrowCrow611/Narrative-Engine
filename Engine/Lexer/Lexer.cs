namespace Engine.Lexer;

public class Lexer {
    private readonly string _source;
    private int _pos;
    private int _line;
    private int _col;

    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        ["scene"] = TokenType.KwScene,
        ["beat"] = TokenType.KwBeat,
        ["choice"] = TokenType.KwChoice,
        ["require"] = TokenType.KwRequire,
        ["effect"] = TokenType.KwEffect,
        ["tag"] = TokenType.KwTag,
        ["systems"] = TokenType.KwSystems,
        ["game_config"] = TokenType.KwGameConfig,
        ["graph"] = TokenType.KwGraph,
        ["node"] = TokenType.KwNode,
        ["chapter"] = TokenType.KwChapter,
        ["voice"] = TokenType.KwVoice,
        ["emotion"] = TokenType.KwEmotion,
    };

    public Lexer(string source) {
        _source = source;
        _pos = 0;
        _line = 1;
        _col = 1;
    }

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (!AtEnd()) {
            SkipWSACOM();
            if (AtEnd()) break;

            var token = NextToken();
            tokens.Add(token);
        }

        tokens.Add(new Token(TokenType.EndOfFile, "", _line, _col));
        return tokens;
    }

    private Token NextToken() {
        int line = _line;
        int col = _col;
        char c = Current();

        if (c == '"') return ReadString(line, col);
        if (char.IsDigit(c)) return ReadNumber(line, col);
        if (char.IsLetter(c) || c == '_') return ReadWord(line, col);

        if (c == '-' && Peek() == '>') { Advance(); Advance(); return Tok(TokenType.Arrow, "->", line, col); }
        if (c == '&' && Peek() == '&') { Advance(); Advance(); return Tok(TokenType.And, "&&", line, col); }
        if (c == '|' && Peek() == '|') { Advance(); Advance(); return Tok(TokenType.Or, "||", line, col); }
        if (c == '!' && Peek() == '=') { Advance(); Advance(); return Tok(TokenType.NotEq, "!=", line, col); }
        if (c == '=' && Peek() == '=') { Advance(); Advance(); return Tok(TokenType.EqEq, "==", line, col); }
        if (c == '<' && Peek() == '=') { Advance(); Advance(); return Tok(TokenType.LtEq, "<=", line, col); }
        if (c == '>' && Peek() == '=') { Advance(); Advance(); return Tok(TokenType.GtEq, ">=", line, col); }

        switch (c) {
            case '{': Advance(); return Tok(TokenType.LBrace, "{", line, col);
            case '}': Advance(); return Tok(TokenType.RBrace, "}", line, col);
            case '[': Advance(); return Tok(TokenType.LBracket, "[", line, col);
            case ']': Advance(); return Tok(TokenType.RBracket, "]", line, col);
            case '(': Advance(); return Tok(TokenType.LParen, "(", line, col);
            case ')': Advance(); return Tok(TokenType.RParen, ")", line, col);
            case ':': Advance(); return Tok(TokenType.Colon, ":", line, col);
            case ',': Advance(); return Tok(TokenType.Comma, ",", line, col);
            case '.': Advance(); return Tok(TokenType.Dot, ".", line, col);
            case '!': Advance(); return Tok(TokenType.Not, "!", line, col);
            case '<': Advance(); return Tok(TokenType.Lt, "<", line, col);
            case '>': Advance(); return Tok(TokenType.Gt, ">", line, col);
            default: Advance(); return Tok(TokenType.Unknown, c.ToString(), line, col);
        }
    }

    private Token ReadString(int line, int col) {
        Advance();
        var sb = new System.Text.StringBuilder();
        while (!AtEnd() && Current() != '"') {
            if (Current() == '\\' && Peek() == '"') {
                Advance();
                sb.Append('"');
                Advance();
            }
            else 
            {
                sb.Append(Current());
                Advance();
            }
        }
        if (!AtEnd()) Advance();
        return Tok(TokenType.StringLiteral, sb.ToString(), line, col);
    }

    private Token ReadNumber(int line, int col) {
        var sb = new System.Text.StringBuilder();
        while (!AtEnd() && char.IsDigit(Current())) { sb.Append(Current()); Advance(); }

        if (!AtEnd() && Current() == '.' && char.IsDigit(Peek()))
        {
            sb.Append('.'); Advance();
            while (!AtEnd() && char.IsDigit(Current())) { sb.Append(Current()); Advance(); }
            return Tok(TokenType.Float, sb.ToString(), line, col);
        }

        return Tok(TokenType.Integer, sb.ToString(), line, col);
    }

    private Token ReadWord(int line, int col) {
        var sb = new System.Text.StringBuilder();
        while (!AtEnd() && (char.IsLetterOrDigit(Current()) || Current() == '_')) {
            sb.Append(Current());
            Advance();
        }
        var word = sb.ToString();
        if (Keywords.TryGetValue(word, out var kwType))
            return Tok(kwType, word, line, col);
        return Tok(TokenType.Identifier, word, line, col);
    } 
    private void SkipWSACOM() {
        while (!AtEnd()) {

            if (Current() == '\n') {_line++; _col  = 1; _pos++; continue; }
            if (char.IsWhiteSpace(Current())) { Advance(); continue; }

            if (Current() == '/' && Peek() == '/') {
                while (!AtEnd() && Current() != '\n') Advance();
                continue;
            }

            break;
        }
    }

    private char Current() => _source[_pos];
    private char Peek() => _pos + 1 <_source.Length ? _source[_pos + 1] : '\0';
    private bool AtEnd() => _pos >= _source.Length;

    private void Advance() {
        if (!AtEnd()) { _pos++; _col++; }
    }

    private static Token Tok(TokenType type, string value, int line, int col)
        => new(type, value, line, col);
}