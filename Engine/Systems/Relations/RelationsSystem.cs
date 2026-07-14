using Engine.Components.Relations;
using Engine.Events;

namespace Engine.Systems.Relations;

public record RepChangedEvent(string OwnerId, string TargetId, float OldScore, float NewScore);
public record TierChangedEvent(string OwnerId, string TargetId, string? OldTier, string NewTier);
public record MemoryAddedEvent(string OwnerId, string TargetId, string EventKey, float Impact);
public record QualityChangedEvent(string OwnerId, string TargetId, float OldQuality, float NewQuality);

public class RelationsSystem
{
    private readonly EventBus? _bus;
    private readonly EffectDispatchSystem? _effects;

    public RelationsSystem(EventBus? bus = null, EffectDispatchSystem? effects = null)
    {
        _bus     = bus;
        _effects = effects;
    }

    public void ModifyScore(string ownerId, Relation relation, float delta)
    {
        var old = relation.Score;
        relation.Score = Math.Clamp(relation.Score + delta, relation.Min, relation.Max);
        if (Math.Abs(old - relation.Score) < 0.001f) return;
        _bus?.Publish(new RepChangedEvent(ownerId, relation.TargetId, old, relation.Score));
        CheckTierChange(ownerId, relation, old);
    }

    public void SetScore(string ownerId, Relation relation, float value)
    {
        var old = relation.Score;
        relation.Score = Math.Clamp(value, relation.Min, relation.Max);
        _bus?.Publish(new RepChangedEvent(ownerId, relation.TargetId, old, relation.Score));
        CheckTierChange(ownerId, relation, old);
    }

    public void RecordMemory(string ownerId, Relation relation,
        string eventKey, float impact, int turn)
    {
        var entry = new MemoryEntry($"{ownerId}_{eventKey}_{turn}", eventKey, impact, turn);
        relation.AddMemory(entry);
        _bus?.Publish(new MemoryAddedEvent(ownerId, relation.TargetId, eventKey, impact));
    }

    public void RecalculateQuality(string ownerId, Relation relation,
        Dictionary<string, float> externalInputs)
    {
        var oldQuality  = relation.Quality;
        float total     = 0f;
        float weights   = 0f;

        var scoreWeight     = relation.InputWeights.TryGetValue("score", out var sw) ? sw : 0.4f;
        var normalizedScore = (relation.Score - relation.Min) / (relation.Max - relation.Min);
        total   += normalizedScore * scoreWeight;
        weights += scoreWeight;

        var historyWeight = relation.InputWeights.TryGetValue("history", out var hw) ? hw : 0.3f;
        var historyScore  = relation.Memory.Count > 0
            ? Math.Clamp(relation.Memory.Sum(m => m.Impact) / relation.Memory.Count, -1f, 1f)
            : 0f;
        var normalizedHistory = (historyScore + 1f) / 2f;
        total   += normalizedHistory * historyWeight;
        weights += historyWeight;

        foreach (var (key, value) in externalInputs)
        {
            var w = relation.InputWeights.TryGetValue(key, out var ew) ? ew : 0f;
            total   += Math.Clamp(value, 0f, 1f) * w;
            weights += w;
        }

        relation.Quality = weights > 0f
            ? Math.Clamp(total / weights * 100f, 0f, 100f)
            : normalizedScore * 100f;

        if (Math.Abs(oldQuality - relation.Quality) > 0.01f)
            _bus?.Publish(new QualityChangedEvent(
                ownerId, relation.TargetId, oldQuality, relation.Quality));
    }

    public void Decay(string ownerId, Relation relation, float deltaTime)
    {
        if (relation.DecayRate == 0f) return;
        ModifyScore(ownerId, relation, -relation.DecayRate * deltaTime);
    }

    public void ApplySpillover(string ownerId,
        Relation source, Relation target, float strength)
    {
        var delta = source.Score * strength;
        ModifyScore(ownerId, target, delta);
    }

    private void CheckTierChange(string ownerId, Relation relation, float oldScore)
    {
        var oldTier = relation.GetTier(oldScore);
        var newTier = relation.GetTier(relation.Score);

        if (newTier is null || oldTier?.Name == newTier.Name) return;

        if (oldTier is not null && _effects is not null)
            foreach (var e in oldTier.OnExit)
                _effects.Apply(e);

        if (_effects is not null)
            foreach (var e in newTier.OnEnter)
                _effects.Apply(e);

        relation.CurrentTier = newTier.Name;
        _bus?.Publish(new TierChangedEvent(
            ownerId, relation.TargetId, oldTier?.Name, newTier.Name));
    }
}
