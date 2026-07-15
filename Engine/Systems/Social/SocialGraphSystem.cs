using Engine.Components.Social;
using Engine.Events;

namespace Engine.Systems.Social;

public record BondFormedEvent(string OwnerId, string TargetId, string Type);
public record BondStrengthChangedEvent(string OwnerId, string TargetiD,
    float OldStrength, float NewStrength);
public record BondStageChangedEvent(string OwnerId, string TargetId, 
    BondStage OldStage, BondStage NewStage);
public record BondBrokenEvent(string OwnerId, string TargetId, string Type);

public class SocialGraphSystem
{
    private readonly EventBus? _bus;
    private readonly EffectDispatchSystem? _effects;

    public SocialGraphSystem(EventBus? bus = null,
        EffectDispatchSystem? effects = null)
    {
        _bus = bus;
        _effects = effects;
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

    public void ModifyStrength(SocialGraph graph, string owenerId,
        string targetId, float delta)
    {
        var bond = graph.GetBond(owenerId, targetId);
        if (bond is null) return;

        var oldStrength = bond.Strength;
        var oldStage = bond.Stage;

        bond.Strength = Math.Clamp(
            bond.Strength + delta, bond.Min, bond.Max);

        if (Math.Abs(oldStrength - bond.Strength) < 0.001f) return;

        _bus?.Publish(new BondStrengthChangedEvent(
            owenerId, targetId, oldStrength, bond.Strength));

        var newStage = bond.CalculateStage();
        if (newStage != oldStage)
        {
            bond.Stage = newStage;
            _bus?.Publish(new BondStageChangedEvent(
                owenerId, targetId, oldStage, newStage));
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

    public bool AreMutallyBonded(SocialGraph graph, 
        string idA, string idB) =>
        graph.HasBond(idA, idB) && graph.HasBond(idB, idA);

    public float GetMutualStrength(SocialGraph graph,
        string idA, string idB) =>
        (GetStrength(graph, idA, idB) + GetStrength(graph,idB, idA)) / 2f;
}