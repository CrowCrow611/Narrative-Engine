using Engine.AST;
using Engine.Components;
using Engine.Events;
using Engine.Rendering;
using Engine.Systems;

namespace Engine.Tests;

public class MilestoneTests
{
    private static (RuntimeSystem, Traceable) BuildWorld(params StoryNode[] nodes)
    {
        var world = new WorldState();
        var trace = new Traceable();
        var runtime = new RuntimeSystem(world, trace);
        runtime.LoadNodes(nodes);
        return (runtime, trace);
    }

    private static StoryNode Beat(string id, string text) => new(
        id, NodeKind.Beat, text, [], [], [], null, [], [], 1, 1);
    private static StoryNode Choice(string id, params ChoiceBranch[] branches) => new(
        id, NodeKind.Choice, null, [..branches], [], [], null, [], [], 1, 1);
    
    [Fact]
    public void ThreeNodeTraversal_TracksAllVisits()
    {
        var (runtime, trace) = BuildWorld(
            Beat("intro", "You stand at a crossroads."),
            Choice("fork",
                new("Go north.", "north", null),
                new("Go south.", "south", null)),
            Beat("north", "You head north, into the deadly forest."),
            Beat("south", "Yiou head south to the Abhi boi house")
        );

        runtime.SetStartNode("intro");
        runtime.Advance("fork");
        runtime.ChooseBranch(0);

        Assert.True(trace.HasVisited("intro"));
        Assert.True(trace.HasVisited("fork"));
        Assert.True(trace.HasVisited("north"));
        Assert.Equal("north", runtime.CurrentNode!.Id);
    }

    [Fact]
    public void BrowserRenderer_RBAC()
    {
        var renderer = new BrowserRenderer();

        renderer.RenderNode(Beat("intro", "You stand at a crossroads."));
        renderer.RenderNode(Choice("fork",
            new("Go north.", "north", null),
            new("Go south.", "south", null)));

        var html = renderer.FlushHtml();

        Assert.Contains("beat", html);
        Assert.Contains("You stand at a crossroads.", html);
        Assert.Contains("choices", html);
        Assert.Contains("Go north.", html);
    }

    [Fact]
    public void TerminalRenderer_DoesNotThrow()
    {
        var renderer = new TerminalRenderer();
        var ex = Record.Exception(() =>
        {
            renderer.RenderNode(Beat("intro", "You stand at a crossroads."));
            renderer.RenderNode(Choice("fork",
                new("Go north.", "north", null),
                new("Go south.", "south", null)));
        });
        Assert.Null(ex);
    }

    [Fact]
    public void EventBus_FNE()
    {
        var bus = new EventBus();
        var world = new WorldState();
        var trace = new Traceable();
        var runtime = new RuntimeSystem(world, trace, bus);

        string? lastNode = null;
        bus.Subscribe<NodeEnteredEvent>(e => lastNode = e.NodeId);

        runtime.LoadNodes([Beat("intro", "hello.")]);
        runtime.SetStartNode("intro");

        Assert.Equal("intro", lastNode);
    }
}