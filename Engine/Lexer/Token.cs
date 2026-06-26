namespace Engine.Lexer;

public record Token(
    TokenType Type,
    string Value,
    int Line,
    int Column
)
{
    public override string ToString() =>
        $"[{Type} {Value.Replace("\n", "\\n")!} {Line}:{Column}]";
}