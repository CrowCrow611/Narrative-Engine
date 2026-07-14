namespace Engine.Components.Relations;

public class RepTier
{
    public string Name { get; }
    public float Min { get; }
    public float Max { get; }
    public List<string> OnEnter { get; } = new();
    public List<string> OnExit { get; } = new();

    public RepTier(string name, float min, float max)
    {
        Name = name;
        Min = min;
        Max = max;
    }

    public bool Contains(float value) => value >= Min && value <= Max;
}

public class RelationInput
{
    public string Name { get; }
    public float Weight { get; set; }

    public RelationInput(string name, float weight)
    {
        Name = name;
        Weight = weight;
    }
}

public class MemoryEntry
{
    public string Id { get; }
    public string EventKey { get;}
    public float Impact { get; }
    public int Turn { get; } 

    public MemoryEntry(string id, string eventKey, float impact, int turn)
    {
        Id = id;
        EventKey = eventKey;
        Impact = impact;
        Turn = turn;
    }
}

public class Relation
{
    public string TargetId { get; }
    public float Score { get; set; } = 0f;
    public float Min { get; set; } = -100f;
    public float Max { get; set; } = 100f;
    public float DecayRate { get; set; } = 0f;
    public float Quality { get; set; } = 0f;
    public string? CurrentTier { get; set; }

    public List<RepTier> Tiers { get; } = new();
    public List<RelationInput> Inputs { get; } = new();
    public List<MemoryEntry> Memory { get;} = new();

    public Dictionary<string, float> InputWeights { get; } = new();

    public Relation(string targetId) => TargetId = targetId;

    public void AddTier(RepTier tier) => Tiers.Add(tier);
    public void AddMemory(MemoryEntry e) => Memory.Add(e);

    public RepTier? GetTier(float value) =>
        Tiers.FirstOrDefault(t => t.Contains(value));
    public bool HasMemory(string eventKey) =>
        Memory.Any(m => m.EventKey == eventKey);
}

public class RelationState
{
    private readonly Dictionary<string, Relation> _relations = new();
    public IReadOnlyDictionary<string, Relation> Relations => _relations;

    public void Add(Relation relation) =>
        _relations[relation.TargetId] = relation;

    public void Remove(string targetId) =>
        _relations.Remove(targetId);
    public Relation? Get(string targetId) =>
        _relations.TryGetValue(targetId, out var r) ? r : null;
    
    public bool Has(string targetId) =>
        _relations.ContainsKey(targetId);
}