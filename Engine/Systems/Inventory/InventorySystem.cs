using Engine.Components.Inventory;
using Engine.Events;

namespace Engine.Systems.Inventory;

public record ItemAddedEvent(string ContainerId, string ItemId, int Quantity);
public record ItemRemovedEvent(string ContainerId, string ItemId, int Quantity);
public record ItemMovedEvent(string FromId, string ToId, string ItemId, int Quantity);
public record ContainerFullEvent(string Container, string ItemId);

public class InventorySystem
{
    private readonly EventBus? _bus;
    public InventorySystem(EventBus? bus = null) => _bus = bus;

    public bool TryAdd(Container container, ItemDefinition item, int quantity = 1)
    {
        if (container.IsLocked) return false;

        var weight = item.Weight * quantity;
        if (!container.CanFitWeight(weight))
        {
            _bus?.Publish(new ContainerFullEvent(container.Id, item.Id));
            return false;
        }

        var existing = container.Find(item.Id);

        if (existing is not null && item.MaxStack > 1)
        {
            var space = item.MaxStack - existing.Quantity;
            var add = Math.Min(quantity, space);
            existing.Quantity += add;

            if (add < quantity)
            {
                _bus?.Publish(new ContainerFullEvent(container.Id, item.Id));
                return false;
            }
        }
        else
        {
            container.AddStack(new ItemStack(item, quantity));
        }

        _bus?.Publish(new ItemAddedEvent(container.Id, item.Id, quantity));
        return true;
    }

    public bool TryRemove(Container container, string itemId, int quantity = 1)
    {
        if (container.IsLocked) return false;
        var stack = container.Find(itemId);
        if (stack is null || stack.Quantity < quantity) return false;

        stack.Quantity -= quantity;
        if (stack.Quantity == 0)
            container.RemoveStack(stack);

        _bus?.Publish(new ItemRemovedEvent(container.Id, itemId, quantity));
        return true;
    }

    public bool TryMove(Container from, Container to, string itemId, int quantity = 1)
    {
        var stack = from.Find(itemId);
        if (stack is null || stack.Quantity < quantity) return false;

        if (!TryAdd(to, stack.Item, quantity)) return false;
        TryRemove(from, itemId, quantity);

        _bus?.Publish(new ItemMovedEvent(from.Id, to.Id, itemId, quantity));
        return true;
    }

    public int Count(Container container, string itemId) =>
        container.Find(itemId)?.Quantity ?? 0;

    public bool Has(Container container, string itemId, int quantity = 1) =>
        Count(container, itemId) >= quantity;

    public List<ItemStack> FindByTag(Container container, string tag) =>
        container.FindByTag(tag);

    public float TotalWeight(Container container) =>
        container.CurrentWeight;
}