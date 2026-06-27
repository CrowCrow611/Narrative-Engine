using Engine.AST;
using Engine.Components;
using Engine.Systems;

namespace Engine.Tests;

public class RuntimeSystemTests {
    private static RuntimeSystem BuildRuntime(params StoryNode[] nodes) {
        var world = new WorldState();
        var trace = new Traceable();
        var runtime = new RuntimeSystem(world, trace);
        runtime.LoadNodes(nodes);
        return runtime;
    }

    private static StoryNode Beat(string id, string text) => new(
        id, NodeKind.Beat, text, [], [], [], null, [], [], 1, 1);

    private static StoryNode Choice(string id, params ChoiceBranch[] branches) => new(
        id, NodeKind.Choice, null, [..branches], [], [], null, [], [], 1, 1);

    [Fact]
    public void StartNode_SetsEdingState() {
        var runtime = BuildRuntime(Beat("intro", "Hello."));
        runtime.SetStartNode("intro");

        Assert.Equal(EngineState.Reading, runtime.State);
        Assert.Equal("intro", runtime.CurrentNode!.Id);
    }

    [Fact]
    public void ChoiceNode_SetsChoiceState() {
        var runtime = BuildRuntime(
            Choice("fork",
                new("Go left.", "left", null),
                new("Go right.", "right", null)),
            Beat("left", "You go left."),
            Beat("right", "You go right.")
        );
        runtime.SetStartNode("fork");
        Assert.Equal(EngineState.Choice, runtime.State);
    }

    [Fact]
    public void ChooseBranch_TransitionsToTarget() {
        var runtime = BuildRuntime(
            Choice("fork",
                new("Go left.", "left", null),
                new("Go right.", "right", null)),
            Beat("left", "You go left."),
            Beat("right", "You go right.")
        );
        runtime.SetStartNode("fork");
        runtime.ChooseBranch(1);

        Assert.Equal(EngineState.Reading, runtime.State);
        Assert.Equal("right", runtime.CurrentNode!.Id);
    }

    [Fact]
    public void ChooseBranch_InReadingState_Throws() {
        var runtime = BuildRuntime(Beat("intro", "Hello."));
        runtime.SetStartNode("intro");

        Assert.Throws<InvalidOperationException>(() => { runtime.ChooseBranch(0); });
    }

    [Fact]
    public void Traceable_RecordsVisitedNodes() {
        var world = new WorldState();
        var trace = new Traceable();
        var runtime  = new RuntimeSystem(world, trace);
        runtime.LoadNodes([
            Beat("intro", "Hello."),
            Beat("next", "World.")
        ]);
        runtime.SetStartNode("intro");
        runtime.Advance("next");

        Assert.True(trace.HasVisited("intro"));
        Assert.True(trace.HasVisited("next"));
    }
}