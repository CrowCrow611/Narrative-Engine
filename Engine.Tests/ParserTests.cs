using Engine.AST;
using Engine.Lexer;
using ParserClass = Engine.Parser.Parser;

namespace Engine.Tests;

public class ParserTests
{
    private static (List<StoryNode>, List<Engine.Parser.ParseException>) Parse(string source)
    {
        var tokens = new Engine.Lexer.Lexer(source).Tokenize();
        var parser = new ParserClass(tokens);
        return parser.ParseFile();
    }

    [Fact]
    public void SimpleScene_PISN()
    {
        var (nodes, errors) = Parse(@"
            scene tavern {
                beat "" You walk in.""}
        ");

        Assert.Empty(errors);
        Assert.Single(nodes);
        Assert.Equal("tavern", nodes[0].Id);
        Assert.Equal(NodeKind.Scene, nodes[0].Kind);
        Assert.Single(nodes[0].Children);
        Assert.Equal("You walk in.", nodes[0].Children[0].Text.Trim());
    }

    [Fact]
    public void Beat_WAId()
    {
        var (nodes, errors) = Parse(@"
            scene tavern {
                beat intro ""You walk in.""}
        ");

        Assert.Empty(errors);
        Assert.Equal("intro", nodes[0].Children[0].Id);
    }

    [Fact]
    public void Choice_PABranches()
    {
        var (nodes, errors) = Parse(@"
            scene tavern {
                choice {
                    ""Order a drink."" -> drink
                    ""Leave."" -> town
                }
            }
        ");

        Assert.Empty(errors);
        var choice = nodes[0].Children[0];
        Assert.Equal(NodeKind.Choice, choice.Kind);
        Assert.Equal(2, choice.Branches.Count);
        Assert.Equal("drink", choice.Branches[0].TargetId);
        Assert.Equal("town", choice.Branches[1].TargetId);
    }

    [Fact]
    public void Choice_WithRCPC()
    {
        var (nodes, errors) = Parse(@"
            scene tavern {
                choice {
                    ""Persuade."" [require: skill.persuasion] -> success
                    }
                }
            ");

            Assert.Empty(errors);
            var branch = nodes[0].Children[0].Branches[0];
            Assert.NotNull(branch.Condition);
    }

    [Fact]
    public void Graph_PNC()
    {
        var (nodes, errors) = Parse(@"
            graph overworld {
                node forest -> cave, village 
                node cave -> forest
            }
        ");

        Assert.Empty(errors);
        var graph = nodes[0];
        Assert.Equal(2, graph.Children.Count);
        Assert.Equal(2, graph.Children[0].Connections.Count);
        Assert.Equal("cave", graph.Children[0].Connections[0]);
    }

    [Fact]
    public void Chapter_PNS()
    {
        var (nodes, errors) = Parse(@"
            chapter ""Act I"" {
                scene arrival {
                    beat ""You arrive.""
                }
            }
        ");

        Assert.Empty(errors);
        var chapter = nodes[0];
        Assert.Equal(NodeKind.Chapter, chapter.Kind);
        Assert.Equal("Act I", chapter.Text);
        Assert.Single(chapter.Children);
        Assert.Equal("arrival", chapter.Children[0].Id);
    }

    [Fact]
    public void MissingBrace_PEL()
    {
        var (nodes, errors) = Parse(@"
            scene tavern
                beat ""You walk in.""
            }
        ");

        Assert.NotEmpty(errors);
        Assert.True(errors[0].Line > 0);
    }

    [Fact]
    public void InlineTags_PSE()
    {
        var (nodes, errors) = Parse(@"
            scene tavern {
                beat ""Hello there."" [voice: sanjay, emotion: tense]
            }
        ");

        Assert.Empty(errors);
        var beat = nodes[0].Children[0];
        Assert.Contains("speaker:sanjay", beat.Tags);
        Assert.Contains("emotion:tense", beat.Tags);
    }

    [Fact]
    public void InConitionPrefix_PE()
    {
        var (nodes, errors) = Parse(@"
            scene tavern {
                choice {
                    ""Try."" [require: gold >= 10] -> result
                }
            }
        ");

        Assert.NotEmpty(errors);
    }
}