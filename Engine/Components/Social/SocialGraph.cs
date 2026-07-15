namespace Engine.Components.Social;

public enum BondStage
{
    Stranger, Acquaintance, Friend, Close, Intimate, Bonded
}

public class Bond
{
    public string TargetId { get; }
    public string Type { get; set; }
    public float Strength { get; set; } = 0f;
    public float Min { get; set; } = 0f;
    public float Max { get; set;} = 10f;
    public BondStage Stage { get; set; } = BondStage.Stranger;
    public List<string> History {get; } = new();
    public Dictionary<string, string> Metadata { get; } = new();
    public Dictionary<BondStage, float> StageThresholds { get; } = new()
    {
        [BondStage.Stranger] = 0f,
        [BondStage.Acquaintance] = 1f,
        [BondStage.Friend] = 3f,
        [BondStage.Close] = 5f,
        [BondStage.Intimate] = 7f,
        [BondStage.Bonded] = 9f,
    };

    public Bond(string targetId, string type)
    {
        TargetId = targetId;
        Type = type;
    }

    public BondStage CalculateStage()
    {
        var stage = BondStage.Stranger;
        foreach (var (s, threshold) in StageThresholds.OrderBy(kv => kv.Value))
            if (Strength >= threshold) stage = s;
        return stage;
    }
}

public class SocialGraph
{
    private readonly Dictionary<string, List<Bond>> _bonds = new();

    public IReadOnlyDictionary<string, List<Bond>> Bonds => _bonds;

    public void AddBond(string ownerId, Bond bond)
    {
        if (!_bonds.ContainsKey(ownerId))
            _bonds[ownerId] = new();
        _bonds[ownerId].Add(bond);
    }

    public void RemoveBond(string ownerId, string targetId)
    {
        if (_bonds.TryGetValue(ownerId, out var bonds))
            bonds.RemoveAll(b => b.TargetId == targetId);
    }

    public Bond? GetBond(string ownerId, string targetId) =>
        _bonds.TryGetValue(ownerId, out var bonds)
            ? bonds.FirstOrDefault(b => b.TargetId == targetId)
            : null;

    public List<Bond> GetBond(string ownerId) =>
        _bonds.TryGetValue(ownerId, out var bonds) ? bonds : new();

    public List<Bond> GetBondsByType(string ownerId, string type) =>
        GetBond(ownerId).Where(b => b.Type == type).ToList();

    public bool HasBond(string ownerId, string targetId) =>
        GetBond(ownerId, targetId) is not null;
}