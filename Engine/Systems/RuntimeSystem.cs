using Engine.AST;
using Engine.Components;

namespace Engine.Systems;

public enum EngineState { Reading, Choice }

public class RuntimeSystem {
    private readonly Dictionary<string, StoryNode> _nodes = new();
    private readonly WorldState _world;
    private readonly Traceable _trace;

    public EngineState State { get; private set; } = EngineState.Reading;
    public StoryNode? CurrentNode { get; private set; }

    public RuntimeSystem(WorldState world, Traceable trace) {
        _world = world;
        _trace = trace;
    }

    public void LoadNodes(IEnumerable<StoryNode> nodes) {
        foreach (var node in nodes)
        _nodes[node.Id] = node;
    }

    public void SetStartNode(string nodeId) => EnterNode(nodeId);

    private void EnterNode(string nodeId) {
        if (!_nodes.TryGetValue(nodeId, out var node))
            throw new InvalidOperationException($"Node '{nodeId}' not found.");

        CurrentNode = node;
        _trace.RecordVisit(nodeId);

        State = node.Kind switch {
            NodeKind.Choice => EngineState.Choice,
            _    => EngineState.Reading,
        };
    }

    public void ChooseBranch(int index) {
        if (State != EngineState.Choice)
            throw new InvalidOperationException(
                $"Cannot choose a branch in state {State}.");

        if (CurrentNode is null || index < 0 || index >= CurrentNode.Branches.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        var branch = CurrentNode.Branches[index];
        EnterNode(branch.TargetId);
    }

    public void Advance(string targetId) => EnterNode(targetId);
}