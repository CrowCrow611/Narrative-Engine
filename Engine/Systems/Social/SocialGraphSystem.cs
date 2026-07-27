using Engine.Components.Social;
using Engine.Events;

namespace Engine.Systems.Social;

public record BondFormedEvent(string OwnerId, string TargetId, string Type);
public record BondStrengthChangedEvent(string OwnerId, string TargetId,
    float OldStrength, float NewStrength);
public record BondStageChangedEvent(string OwnerId, string TargetId, 
    BondStage OldStage, BondStage NewStage);
public record BondBrokenEvent(string OwnerId, string TargetId, string Type);

public class SocialGraphSystem : IEffectHandler
{
    private readonly EventBus? _bus;
    private readonly EffectDispatchSystem? _effects;
    private readonly SocialGraph? _graph;

    public SocialGraphSystem(EventBus? bus = null,
        EffectDispatchSystem? effects = null, SocialGraph? graph = null)
    {
        _bus = bus;
        _effects = effects;
        _graph = graph;
    }

    public Bond FormBond(SocialGraph graph, string ownerId,
        string targetId, string type, float initialStrength = 0f)
    {
        var existing = graph.GetBond(ownerId, targetId);
        if (existing is not null)
        {
            existing.Type =type;
            return existing;
        }

        var bond = new Bond(targetId, type) { Strength = initialStrength };
        graph.AddBond(ownerId, bond);
        _bus?.Publish(new BondFormedEvent(ownerId, targetId, type));
        return bond;
    }

    public void ModifyStrength(SocialGraph graph, string ownerId,
        string targetId, float delta)
    {
        var bond = graph.GetBond(ownerId, targetId);
        if (bond is null) return;

        var oldStrength = bond.Strength;
        var oldStage = bond.Stage;

        bond.Strength = Math.Clamp(
            bond.Strength + delta, bond.Min, bond.Max);

        if (Math.Abs(oldStrength - bond.Strength) < 0.001f) return;

        _bus?.Publish(new BondStrengthChangedEvent(
            ownerId, targetId, oldStrength, bond.Strength));

        var newStage = bond.CalculateStage();
        if (newStage != oldStage)
        {
            bond.Stage = newStage;
            _bus?.Publish(new BondStageChangedEvent(
                ownerId, targetId, oldStage, newStage));
        }
    }

    public void SetStrength(SocialGraph graph, string ownerId,
        string targetId, float value)
    {
        var bond = graph.GetBond(ownerId, targetId);
        if (bond is null) return;
        var delta = value - bond.Strength;
        ModifyStrength(graph, ownerId, targetId, delta);
    }

    public void RecordEvent(SocialGraph graph, string ownerId,
        string targetId, string eventKey)
    {
        var bond = graph.GetBond(ownerId, targetId);
        bond?.History.Add(eventKey);
    }

    public void BreakBond(SocialGraph graph, string ownerId, string targetId)
    {
        var bond = graph.GetBond(ownerId, targetId);
        if (bond is null) return;

        graph.RemoveBond(ownerId, targetId);
        _bus?.Publish(new BondBrokenEvent(ownerId, targetId, bond.Type));
    }

    public float GetStrength(SocialGraph graph, string ownerId, string targetId) =>
        graph.GetBond(ownerId, targetId)?.Strength ?? 0f;

    public BondStage GetStage(SocialGraph graph, string ownerId, string targetId) =>
        graph.GetBond(ownerId, targetId)?.Stage ?? BondStage.Stranger;

    public bool AreMutuallyBonded(SocialGraph graph, 
        string idA, string idB) =>
        graph.HasBond(idA, idB) && graph.HasBond(idB, idA);

    public float GetMutualStrength(SocialGraph graph,
        string idA, string idB) =>
        (GetStrength(graph, idA, idB) + GetStrength(graph,idB, idA)) / 2f;

    public bool CanHandle(string prefix) => prefix == "bond";

    public void Apply(string effect)
    {
        var parts = effect.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
            throw new InvalidOperationException($"Malformed bond effect: ''{effect}");

        var path = parts[0].Split('.');
        if (path.Length != 4 || path[0] != "bond" || path[3] != "type")
            throw new InvalidOperationException($"Unsupported bond effect: '{effect}'");

        if (_graph is null)
            throw new InvalidOperationException(
                "SocialGraphSystem has no SocialGraph registered.");

        var ownerId = path[1];
        var targetId = path[2];

        var bond = _graph.GetBond(ownerId, targetId);
        if (bond is null)
            throw new InvalidOperationException($"No bond from '{ownerId}' to '{targetId}'.");

        bond.Type = parts[2];
    }
}