// Fro now i plan on 
namespace Engine.Components.Abilities;

public class AbilityNode
{
    public string Id { get;}
    public string DisplayName { get;}
    public float Cost { get; set;}
    public List<string> Prerequisites { get;} = new();
    public List<string> Effects { get;} = new();
    public bool IsUnlocked { get; set;} = false;
    public Dictionary<string, string> Metadata { get;} = new();

    public AbilityNode(string id, string displayName, float cost)
    {
        Id = id;
        DisplayName = displayName;
        Cost = cost;
    }
}

public class AbilityTree
{
    public string Id { get;}
    public string ResourceName { get; set;} = "mana";
    public float CurrentResource { get; set;} = 0f;
    public float MaxResource { get; set;} = 100f;

    public AbilityTree(string id) => Id = id;

    private readonly Dictionary<string, AbilityNode> _nodes = new();
    public IReadOnlyDictionary<string, AbilityNode> Nodes => _nodes;
    public void AddNode(AbilityNode node) => _nodes[node.Id] = node;
    public AbilityNode? Get(string id) =>
        _nodes.TryGetValue(id, out var n) ? n : null;

    public bool IsUnlocked(string id) =>
        _nodes.TryGetValue(id, out var n) && n.IsUnlocked;
        
    public bool CanUnlock (string id)
    {
        if (!_nodes.TryGetValue(id, out var node)) return false;
        if (node.IsUnlocked) return false;
        if (CurrentResource < node.Cost) return false;
        return node.Prerequisites.All(p => IsUnlocked(p));
    }
}

public class AbilityBook
{
    private readonly Dictionary<string, AbilityTree> _trees = new();
    public IReadOnlyDictionary<string, AbilityTree> Trees => _trees;

    public void AddTree(AbilityTree tree) => _trees[tree.Id] = tree;
    public void RemoveTree(string id) => _trees.Remove(id);

    public AbilityTree? Get(string id) =>
        _trees.TryGetValue(id, out var t) ? t : null;
}