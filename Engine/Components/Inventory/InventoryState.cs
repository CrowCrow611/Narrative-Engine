namespace Engine.Components.Inventory;

public class ItemDefinition
{
    public string Id { get; }
    public string DisplayName { get; }
    public float Weight { get; set; } = 0f;
    public int MaxStack { get; set; } = 1;
    public List<string> Tags { get; } = new();
    public Dictionary<string, string> Properties { get; } = new();

    public ItemDefinition(string id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }

    public bool HasTag(string tag) => Tags.Contains(tag);
}

public class ItemStack
{
    public ItemDefinition Item { get; }
    public int Quantity { get; set; }

    public ItemStack(ItemDefinition item, int quantity = 1)
    {
        Item = item;
        Quantity = quantity;
    }

    public float TotalWeight => Item.Weight * Quantity;
}

public class Container
{
    public string Id { get; }
    public string DisplayName { get; }
    public float MaxWeight { get; set; } = -1f;
    public bool IsLocked { get; set; } = false;

    private readonly List<ItemStack> _stacks = new();
    public IReadOnlyList<ItemStack> Stacks => _stacks;

    public float CurrentWeight =>
        _stacks.Sum(s => s.TotalWeight);

    public Container(string id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }

    public void AddStack(ItemStack stack) => _stacks.Add(stack);
    public void RemoveStack(ItemStack stack) => _stacks.Remove(stack);

    public ItemStack? Find(string itemId) =>
        _stacks.FirstOrDefault(s => s.Item.Id == itemId);

    public List<ItemStack> FindByTag(string tag) =>
        _stacks.Where(s => s.Item.HasTag(tag)).ToList();

    public bool CanFitWeight(float weight) =>
        MaxWeight < 0 || CurrentWeight + weight <= MaxWeight;
}

public class InventoryState
{
    private readonly Dictionary<string, Container> _containers = new();
    private readonly Dictionary<string, ItemDefinition> _registry = new();

    public IReadOnlyDictionary<string, Container> Containers => _containers;
    public IReadOnlyDictionary<string, ItemDefinition> Registry => _registry;

    public void RegisterItem(ItemDefinition item) => _registry[item.Id] = item;
    public void AddContainer(Container container) => _containers[container.Id] = container;
    public void RemoveContainer(string id) => _containers.Remove(id);

    public Container? GetContainer(string id) =>
        _containers.TryGetValue(id, out var c) ? c : null;

    public ItemDefinition? GetItem(string id) =>
        _registry.TryGetValue(id, out var i) ? i : null;
}