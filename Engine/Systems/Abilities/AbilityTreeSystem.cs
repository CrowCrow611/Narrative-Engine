using Engine.Components.Abilities;
using Engine.Events;
using Engine.Systems;

namespace Engine.Systems.Abilities;

public record AbilityUnlockedEvent(string TreeId, string AbilityId);
public record AbilityUsedEvent(string TreeId, string AbilityId, string ActorId);
public record AbilityFailedEvent(string TreeId, string AbilityId, string Reason);

public class AbilityTreeSystem
{
    private readonly EventBus? _bus;
    private readonly EffectDispatchSystem? _effects;

    public AbilityTreeSystem(EventBus? bus = null,
        EffectDispatchSystem? effects = null)
    {
        _bus = bus;
        _effects = effects;
    }

    public bool TryUnlock(AbilityTree tree, string abilityId)
    {
        if (!tree.CanUnlock(abilityId))
        {
            var reason = GetFailReason(tree, abilityId);
            _bus?.Publish(new AbilityFailedEvent(tree.Id, abilityId, reason));
            return false;
        }

        var node = tree.Get(abilityId)!;
        node.IsUnlocked = true;
        tree.CurrentResource -= node.Cost;

        _bus?.Publish(new AbilityUnlockedEvent(tree.Id, abilityId));
        return true;
    }

    public bool TryUse(AbilityTree tree, string abilityId, string actorId)
    {
        var node = tree.Get(abilityId);
        if (node is null || !node.IsUnlocked)
        {
            _bus?.Publish(new AbilityFailedEvent(
                tree.Id, abilityId, "Ability not unlocked."));
            return false;
        }

        if (tree.CurrentResource < node.Cost)
        {
            _bus?.Publish(new AbilityFailedEvent(
                tree.Id, abilityId,
                $"Not enough {tree.ResourceName}."));
            return false;
        }

        tree.CurrentResource -= node.Cost;

        if (_effects is not null) 
            foreach (var effect in node.Effects)
                _effects.Apply(effect);

            _bus?.Publish(new AbilityUsedEvent(tree.Id, abilityId, actorId));
            return true;
    }

    public void SetResource(AbilityTree tree, float value) =>
        tree.CurrentResource = Math.Clamp(value, 0f, tree.MaxResource);

    public void ModifyResource(AbilityTree tree, float delta) =>
        tree.CurrentResource = Math.Clamp (
            tree.CurrentResource + delta, 0f, tree.MaxResource);

    public void AddNode(AbilityTree tree, AbilityNode node) =>
        tree.AddNode(node);

    public void LockAbility(AbilityTree tree, string abilityId)
    {
        var node = tree.Get(abilityId);
        if (node is not null) node.IsUnlocked = false;
    }

    private static string GetFailReason(AbilityTree tree, string abilityId)
    {
        var node = tree.Get(abilityId);
        if (node is null) return "Ability does not exist";
        if (node.IsUnlocked) return "Already unlocked";
        if (tree.CurrentResource < node.Cost)
            return $"Not enough {tree.ResourceName} " +
                $"(need {node.Cost}, have {tree.CurrentResource}.";

        var missing = node.Prerequisites 
            .Where(p => !tree.IsUnlocked(p))
            .ToList();
        if (missing.Count > 0)
            return $"Missing prerequisites: {string.Join(", ", missing)}.";

        return "Unknown reason.";
    }
}