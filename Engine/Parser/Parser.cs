using Engine.AST;
using Engine.Lexer;

namespace Engine.Parser;

public class Parser
{
    private readonly List<Token> _tokens;
    private int _pos;
    private readonly List <ParseException> _errors = new();

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
        _pos = 0;
    }

    private Token Peek() => _tokens[_pos];
    private Token Advance() => _tokens[_pos++];
    private bool Check(TokenType type) => Peek().Type == type;
    private bool AtEnd() => Peek().Type == TokenType.EndOfFile;

    private Token Expect(TokenType type)
    {
        var token = Peek();
        if (token.Type != type)
        {
            var suggestion = Suggest(type, token);
            throw new ParseException(
                $"excepted {Describe(type)} but found '{token.Value}'{suggestion}", token.Line, token.Column);
        }
        return Advance();
    }

    private Token ExceptRecover(TokenType type)
    {
        try { return Expect(type); }
        catch (ParseException ex)
        {
            _errors.Add(ex);
            return Peek();
        }
    }

    private void Synchronize(params TokenType[] stopAt)
    {
        while (!AtEnd())
        {
            if (stopAt.Contains(Peek().Type)) return;
            Advance();
        }
    }

    public (List<StoryNode> Nodes, List<ParseException> Errors) ParseFile()
    {
        var nodes = new List<StoryNode>();

        while (!AtEnd())
        {
            try
            {
                var token = Peek();
                nodes.Add(token.Type switch
                {
                    TokenType.KwScene => ParseScene(),
                    TokenType.KwGraph => ParseGraph(),
                    TokenType.KwChapter => ParseChapter(),
                    _ => throw new ParseException(
                        $"unexpected '{token.Value}' - expected 'scene', 'graph', or 'chapter'",
                        token.Line, token.Column)
                });
            }
            catch (ParseException ex)
            {
                _errors.Add(ex);
                Synchronize(
                    TokenType.KwScene,
                    TokenType.KwGraph,
                    TokenType.KwChapter,
                    TokenType.EndOfFile);
            }
        }

        return (nodes, _errors);
    }

    private StoryNode ParseScene()
    {
        var kw = Expect(TokenType.KwScene);
        var id = Expect(TokenType.Identifier);
        Expect(TokenType.LBrace);

        var children = new List<StoryNode>();

        while (!Check(TokenType.RBrace) && !AtEnd())
        {
            try
            {
                if (Check(TokenType.KwBeat))
                    children.Add(ParseBeat());
                else if (Check(TokenType.KwChoice))
                    children.Add(ParseChoice());
                else
                {
                    var t = Peek();
                    throw new ParseException(
                        $"unxepted '{t.Value}' inside scene '{id.Value}'" + $" - expected 'beat' or 'choice'", 
                        t.Line, t.Column);
                }
            }
            catch (ParseException ex)
            {
                _errors.Add(ex);
                Synchronize(
                    TokenType.KwBeat,
                    TokenType.KwChoice,
                    TokenType.RBrace);
            }
        }

        Expect(TokenType.RBrace);

        return new StoryNode(
            Id: id.Value,
            Kind: NodeKind.Scene,
            Text: null,
            Branches: [],
            Connections: [],
            Tags: [],
            Condition: null,
            Effects: [],
            Children: children,
            SourceLine: kw.Line,
            SourceColumn: kw.Column
        );
    }

    private StoryNode ParseBeat()
    {
        var kw = Expect(TokenType.KwBeat);

        string? authorId = null;
        if (Check(TokenType.Identifier))
            authorId = Advance().Value;

        var text = Expect(TokenType.StringLiteral);
        var (tags, effects, condition) = ParseInlineMeta();

        return new StoryNode(
            Id: authorId ?? $"beat_{kw.Line}_{kw.Column}",
            Kind: NodeKind.Beat,
            Text: text.Value,
            Branches: [],
            Connections: [],
            Tags: tags,
            Condition: condition,
            Effects: effects,
            Children: [],
            SourceLine: kw.Line,
            SourceColumn: kw.Column
        );
    }

    private StoryNode ParseChoice()
    {
        var kw = Expect(TokenType.KwChoice);

        string? authorId = null;
        if (Check(TokenType.Identifier))
            authorId = Advance().Value;

            Expect(TokenType.LBrace);

            var branches = new List<ChoiceBranch>();

            while (!Check(TokenType.RBrace) && !AtEnd())
        {
            try
            {
                var text = Expect(TokenType.StringLiteral);
                string? condition = null;

                if (Check(TokenType.LBracket))
                {
                    Advance();
                    Expect(TokenType.KwRequire);
                    Expect(TokenType.Colon);
                    condition = ParseRawCondition();
                    ValidateCondition(condition, Peek());
                    Expect(TokenType.RBracket);
                }

                Expect(TokenType.Arrow);
                var target = Expect(TokenType.Identifier);
                branches.Add(new ChoiceBranch(text.Value, target.Value, condition));
            }
            catch (ParseException ex)
            {  
                _errors.Add(ex);
                Synchronize(TokenType.StringLiteral, TokenType.RBrace);
            }
        }

        Expect(TokenType.RBrace);

        return new StoryNode(
            Id: authorId ?? $"choice_{kw.Line}_{kw.Column}",
            Kind: NodeKind.Choice,
            Text: null,
            Branches: branches,
            Connections: [],
            Tags: [],
            Condition: null,
            Effects: [],
            Children: [],
            SourceLine: kw.Line,
            SourceColumn: kw.Column
        );
    }

    private StoryNode ParseGraph()
    {
        var kw = Expect(TokenType.KwGraph);
        var id = Expect(TokenType.Identifier);
        Expect(TokenType.LBrace);

        var children = new List<StoryNode>();

        while (!Check(TokenType.RBrace) && !AtEnd())
        {
            try { children.Add(ParseGraphNode()); }
            catch (ParseException ex)
            {
                _errors.Add(ex);
                Synchronize(TokenType.KwNode, TokenType.RBrace);
            }
        }

        Expect(TokenType.RBrace);

        return new StoryNode(
            Id: id.Value,
            Kind: NodeKind.Scene,
            Text: null,
            Branches: [],
            Connections: [],
            Tags: [],
            Condition: null,
            Effects: [],
            Children: children,
            SourceLine: kw.Line,
            SourceColumn: kw.Column
        );
    }

    private StoryNode ParseGraphNode()
    {
        var kw = Expect(TokenType.KwNode);
        var id = Expect(TokenType.Identifier);
        var connections = new List<string>();

        if (Check(TokenType.Arrow))
        {
            Advance();
            connections.Add(Expect(TokenType.Identifier).Value);
            while (Check(TokenType.Comma))
            {
                Advance();
                connections.Add(Expect(TokenType.Identifier).Value);
            }
        }

        return new StoryNode(
            Id: id.Value,
            Kind: NodeKind.Graph,
            Text: null,
            Branches: [],
            Connections: connections,
            Tags: [],
            Condition: null,
            Effects: [],
            Children: [],
            SourceLine: kw.Line,
            SourceColumn: kw.Column
        );
    }

    private StoryNode ParseChapter()
    {
        var kw = Expect(TokenType.KwChapter);
        var title = Expect(TokenType.StringLiteral);
        Expect(TokenType.LBrace);

        var children = new List<StoryNode>();

        while (!Check(TokenType.RBrace) && !AtEnd())
        {
            try
            {
                if (Check(TokenType.KwScene))
                    children.Add(ParseScene());
                else
                {
                    var t = Peek();
                    throw new ParseException(
                        $"unexpected '{t.Value}' insdie chapter '{title.Value}'" + $"- expected 'scene'",
                        t.Line, t.Column);
                }
            }
            catch (ParseException ex)
            {
                _errors.Add(ex);
                Synchronize(TokenType.KwScene, TokenType.RBrace);
            }
        }

        Expect(TokenType.RBrace);

        return new StoryNode(
            Id: $"chapter_{kw.Line}",
            Kind: NodeKind.Chapter,
            Text: title.Value,
            Branches: [],
            Connections: [],
            Tags: [],
            Condition: null,
            Effects: [],
            Children: children,
            SourceLine: kw.Line,
            SourceColumn: kw.Column 
        );
    }

    private (List<string> Tags, List<string> Effects, string? Condition) 
        ParseInlineMeta()
    {
        var tags = new List<string>();
        var effects = new List<string>();
        string? condition = null;

        if (!Check(TokenType.LBracket)) return (tags, effects, condition);

        Advance(); 

        while (!Check(TokenType.RBracket) && !AtEnd())
        {
            var key = Peek();

            if (key.Type == TokenType.KwTag)
            {
                Advance();
                Expect(TokenType.Colon);
                var val = Advance();
                tags.Add($"tag:{val.Value}");
            }
            else if (key.Type == TokenType.KwEffect)
            {
                Advance();
                Expect(TokenType.Colon);
                var val = Expect(TokenType.StringLiteral);
                effects.Add(val.Value);
            }
            else if (key.Type == TokenType.KwRequire)
            {
                Advance();
                Expect(TokenType.Colon);
                condition = ParseRawCondition();
                ValidateCondition(condition, key);
            }
            else if (key.Type == TokenType.KwVoice)
            {
                Advance();
                Expect(TokenType.Colon);
                var val = Advance();
                tags.Add($"speaker:{val.Value}");
            }
            else if (key.Type == TokenType.KwEmotion)
            {
                Advance();
                Expect(TokenType.Colon);
                var val = Advance();
                tags.Add($"emotion:{val.Value}");
            }
            else
            {
                _errors.Add(new ParseException(
                    $"unknown tag key '{key.Value}'",
                    key.Line, key.Column));
                Synchronize(TokenType.Comma, TokenType.RBracket);
            }

            if (Check(TokenType.Comma)) Advance();
        }

        Expect(TokenType.RBracket);
        return (tags, effects, condition);
    }

    private string ParseRawCondition()
    {
        var sb = new System.Text.StringBuilder();
        while (!Check(TokenType.RBracket) && !AtEnd())
        {
            sb.Append(Peek().Value);
            sb.Append(' ');
            Advance();
        }
        return sb.ToString().Trim();
    }

    private void ValidateCondition(string condition, Token source)
    {
        if (string.IsNullOrWhiteSpace(condition))
        {
            _errors.Add(new ParseException(
                "condition is empty", source.Line, source.Column));
            return;
        }

        var parts = condition.Split(' ',
            StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0) return;

        var prefix = parts[0].Split('.')[0];
        if (prefix != "flag" && prefix != "counter" && prefix != "timer" && prefix != "skill" && prefix != "rep") 
            _errors.Add(new ParseException(
                $"condition '{condition}' must start with 'flag.', " +
                $"'counter.', or 'timer.' = got '{parts[0]}'",
                source.Line, source.Column));
    }

    private static string Describe(TokenType type) => type switch
    {
        TokenType.Identifier => "an identifier (e.g my_scene)",
        TokenType.StringLiteral => "a quoted string (e.g \"hello\")",
        TokenType.LBrace => "'{'",
        TokenType.RBrace => "'}'",
        TokenType.LBracket => "'['",
        TokenType.RBracket => "']'",
        TokenType.Arrow => "'->'",
        TokenType.Colon => "':'",
        TokenType.KwScene => "'scene'",
        TokenType.KwBeat => "'beat'",
        TokenType.KwChoice => "'choice'",
        TokenType.KwGraph => "'graph'",
        TokenType.KwNode => "'node'",
        TokenType.KwChapter => "'chapter'",
        TokenType.KwRequire => "'require'",
        _ => $"'{type}'"
    };

    private static string Suggest(TokenType expected, Token actual) => 
        (expected, actual.Type) switch
        {
            (TokenType.LBrace, TokenType.Colon) => " - did you forgot '{' after the scene name?",
            (TokenType.Arrow, TokenType.Colon) => " - did you mean '->' insted of ':' ?",
            (TokenType.Identifier, TokenType.StringLiteral) => " - scene and node IDs must not be quoted",
            (TokenType.StringLiteral, TokenType.Identifier) => " - beat text must be in quotes",
            _ => ""
        };
}