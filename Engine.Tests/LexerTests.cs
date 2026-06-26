using Engine.Lexer;

using LexerClass = Engine.Lexer.Lexer;

namespace Engine.Tests;

public class LexerTests {
    private static List<Token> Lex(string source) => new LexerClass(source).Tokenize();

    [Fact]
    public void SimpleScene_ProdCorSeq() {
        var tokens = Lex("scene tavern_entrance { }");

        Assert.Equal(TokenType.KwScene, tokens[0].Type);
        Assert.Equal(TokenType.Identifier, tokens[1].Type);
        Assert.Equal("tavern_entrance", tokens[1].Value);
        Assert.Equal(TokenType.LBrace, tokens[2].Type);
        Assert.Equal(TokenType.RBrace, tokens[3].Type);
        Assert.Equal(TokenType.EndOfFile, tokens[4].Type);
    }

    [Fact]
    public void StringLiteral_CapurInnrText() {
        var tokens = Lex("\"Hello World\"");

        Assert.Equal(TokenType.StringLiteral, tokens[0].Type);
        Assert.Equal("Hello World", tokens[0].Value);
    }

    [Fact]
    public void Arrow_TokenizCorect() {
        var tokens = Lex("forest -> cave");

        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal(TokenType.Arrow, tokens[1].Type);
        Assert.Equal(TokenType.Identifier, tokens[2].Type);
    }

    [Fact]
    public void ConditionOperators_AllRecog() {
        var tokens = Lex("&& || ! == != < <= > >=");

        Assert.Equal(TokenType.And, tokens[0].Type);
        Assert.Equal(TokenType.Or, tokens[1].Type);
        Assert.Equal(TokenType.Not, tokens[2].Type);
        Assert.Equal(TokenType.EqEq, tokens[3].Type);
        Assert.Equal(TokenType.NotEq, tokens[4].Type);
        Assert.Equal(TokenType.Lt, tokens[5].Type);
        Assert.Equal(TokenType.LtEq, tokens[6].Type);
        Assert.Equal(TokenType.Gt, tokens[7].Type);
        Assert.Equal(TokenType.GtEq, tokens[8].Type);
    }

    [Fact]
    public void LineComment_IsSkipped() {
        var tokens = Lex("scene // this is a comment\ntavern");

        Assert.Equal(TokenType.KwScene, tokens[0].Type);
        Assert.Equal(TokenType.Identifier, tokens[1].Type);
        Assert.Equal("tavern", tokens[1].Value);
    }

    [Fact]
    public void LineAndCol_TrackedCorrectly() {
        var tokens = Lex("scene\ntaverns");

        Assert.Equal(1, tokens[0].Line);
        Assert.Equal(1, tokens[0].Column);
        Assert.Equal(2, tokens[1].Line);
        Assert.Equal(1, tokens[1].Column);
    }

    [Fact]
    public void UnknownCharacter_ProducesUnknowToken() {
        var tokens = Lex("@");
        Assert.Equal(TokenType.Unknown, tokens[0].Type);
        Assert.Equal("@", tokens[0].Value);
    }
}