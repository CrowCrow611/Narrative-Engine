using Engine.Components.Inventory;
using Engine.Events;

namespace Engine.Systems.Inventory;

public record IDurabilityChangedEv(string ContainerId, string ItemId, float OldValue, float NewValue);
public record IBrokenEvent(string ContainerId, string ItemId); // IBrokenEvent is ItemBrokenEvent

public class DurabilitySystem : IEffectHandler
{
    private readonly InventoryState? _inventory;
    private readonly EventBus? _bus;
    private readonly EffectDispatchSystem? _effects;

    public DurabilitySystem(InventoryState? inventory = null,
        EventBus? bus = null, EffectDispatchSystem? effects = null)

    {
        _inventory = inventory;
        _bus = bus;
        _effects = effects;
    }

    public bool CanHandle(string prefix) => prefix == "item";

    public void Apply(string effect)
    {
        var parts = effect.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
            throw new InvalidOperationException($"Malformed item effect: '{effect}'");

        var path = parts[0].Split('.');
        if (path.Length != 4 || path[0] != "item" || path[3] != "durability")
            throw new InvalidOperationException(
                $"Unsupported item effect: '{effect}'. Expected item.<container>.<itemId>.durability");

        if (_inventory is null)
            throw new InvalidOperationException(
                "DurabilitySystem has no InventoryState registered.");

        var containerId = path[1];
        var itemId = path[2];

        var container = _inventory.GetContainer(containerId);
        var stack = container?.Find(itemId);
        if (container is null || stack is null)
            throw new InvalidOperationException($"No item '{itemId}' in container '{containerId}'.");

        if (!float.TryParse(parts[2], out var value))
            throw new InvalidOperationException($"Invalid durability value: '{parts[2]}'");

        var current = stack.CurrentDurability ?? stack.Item.MaxDurability ?? 0f;

        var updated = parts[1] switch
        {
            "=" => value,
            "+=" => current + value,
            "-=" => current - value,
            _ => throw new InvalidOperationException($"Unknown durability operation: '{parts[1]}'")
        };

        if (stack.Item.MaxDurability is float max)
            updated = Math.Clamp(updated, 0f, max);

        stack.CurrentDurability = updated;
        _bus?.Publish(new IDurabilityChangedEv(containerId, itemId, current, updated));

        if (updated <= 0f)
            HandleBreak(container, stack, containerId, itemId);
    }

    private void HandleBreak(Container container, ItemStack stack, string cotnainerId, string itemId)
    {
        var itemDef = stack.Item;

        if (_effects is not null)
            foreach (var e in itemDef.BreakEffects)
                _effects.Apply(e);

        _bus?.Publish(new IBrokenEvent(cotnainerId, itemId));

        if (itemDef.AutoBreakOnZero)
            container.RemoveStack(stack);
    }
}