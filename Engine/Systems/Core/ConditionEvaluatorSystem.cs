using Engine.Components;
using Engine.Components.Relations;
using Engine.Components.Inventory;

namespace Engine.Systems;

public class ConditionEvetorSys {
    private readonly WorldState _world;
    private readonly StatSystem? _stats;
    private readonly Dictionary<string, StatBlock> _statBlocks = new();
    private readonly Dictionary<string, RelationState> _relationStates = new();
    private readonly InventoryState? _inventory;

    public ConditionEvetorSys(WorldState world, StatSystem? stats = null,
        InventoryState? inventory = null) {
        _world = world;
        _stats = stats;
        _inventory = inventory;
    }

    public void RegisterStats(string actorId, StatBlock block) => _statBlocks[actorId] = block;
    public void RegisterRelations(string actorId, RelationState state) => _relationStates[actorId] = state;

    public bool Evaluate(string? condition, string actorId = "player") {
        if (string.IsNullOrWhiteSpace(condition)) return true;

        if (condition.Contains("&&"))
        {
            var parts = condition.Split("&&", StringSplitOptions.TrimEntries);
            return parts.All(p => EvaluateAtom(p, actorId));
        }

        if (condition.Contains("||"))
        {
            var parts = condition.Split("||", StringSplitOptions.TrimEntries);
            return parts.Any(p => EvaluateAtom(p, actorId));
        }

        return EvaluateAtom(condition, actorId);
    }

    private bool EvaluateAtom(string condition, string actorId)
    {
        var parts = condition.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            throw new InvalidOperationException($"Unsupported condition: '{condition}'");

        var path = parts[0].Split('.');
        if (path.Length < 2)
            throw new InvalidOperationException($"Unsupported condition: '{condition}'");

        var prefix = path[0];

        if (parts.Length == 1)
        {
            if (prefix == "flag" && path.Length == 2)
                return _world.GetFlag(path[1]);

            if (prefix == "item" && path.Length == 4 && path [3] == "exists")
                return ItemExists(path[1], path[2]);

            throw new InvalidOperationException($"Unsupported conditon: '{condition}'");
        }

        if (parts.Length == 3)
        {
            var op = parts[1];
            float left;

            if (prefix == "item")
            {
            if (path.Length != 4)
                throw new InvalidOperationException($"Unsupported item condition: '{condition}'");

            left = path[3] switch
            {
                "durability" => GetItemDurability(path[1], path[2]),
                "count" => GetItemCount(path[1], path[2]),
                _ => throw new InvalidOperationException(
                    $"Unknown item property '{path[3]}': '{condition}'")
            };
        }
        else
            {
                if (path.Length != 2)
                    throw new InvalidOperationException($"Unsupported condition: '{condition}'");

                var key = path[1];
                left = prefix switch 
                {
                "counter" => _world.GetCounter(key),
                "timer" => _world.GetTimer(key),
                "skill" => GetSkill(actorId, key),
                "rep" => GetRep(actorId, key),
                "flag" => throw new InvalidOperationException(
                    $"'flag.' conditions dont take operators: '{condition}'"),
                _ => throw new InvalidOperationException(
                    $"Unsupported condtion prefix '{prefix}': '{condition}'")
            };
        }

            if (!float.TryParse(parts[2], out var right))
                throw new InvalidOperationException($"Invalid comparison value: '{parts[2]}'");

            return op switch
            {
                ">=" => left >= right,
                "<=" => left <= right,
                ">" => left > right,
                "<" => left < right,
                "==" => Math.Abs(left - right) < 0.0001f,
                "!=" => Math.Abs(left - right) >= 0.0001f,
                _ => throw new InvalidOperationException($"Unknown operator '{op}': '{condition}'") 
            };
        }

        throw new InvalidOperationException($"Unsupported condition: '{condition}'");
    }

    private float GetSkill(string actorId, string statName)
    {
        if (!_statBlocks.TryGetValue(actorId, out var block))
            throw new InvalidOperationException($"No stats registered for actor '{actorId}'");
        if (_stats is null)
            throw new InvalidOperationException("StatSystem not provided to ConditionEvaluator");
        return _stats.Get(block, statName);
    }

    private float GetRep(string actorId, string targetId)
    {
        if (!_relationStates.TryGetValue(actorId, out var relState))
            throw new InvalidOperationException($"No relations registeted for actor '{actorId}'");
        var relation = relState.Get(targetId);
        if (relation is null)
            throw new InvalidOperationException($"No relation '{targetId}' for actor '{actorId}'");
        return relation.Score;
    }

    private bool ItemExists(string containerId, string itemId)
    {
        if (_inventory is null)
            throw new InvalidOperationException("InventoryState not provided to ConditionEvetorSys");
        var stack = _inventory.GetContainer(containerId)?.Find(itemId);
        return stack is not null && stack.Quantity > 0;
    }

    private float GetItemDurability(string containerId, string itemId)
    {
        if (_inventory is null)
            throw new InvalidOperationException("InventoryState not provided to ConditionEvetorSys");
        var stack = _inventory.GetContainer(containerId)?.Find(itemId);
        if (stack is null) return 0f;
        return stack.CurrentDurability ?? stack.Item.MaxDurability ?? 0f;
    }

    private float GetItemCount(string containerId, string itemId)
    {
        if (_inventory is null)
            throw new InvalidOperationException("InventoryState not provided to ConditionEvetorSys");
        var stack = _inventory.GetContainer(containerId)?.Find(itemId);
        return stack?.Quantity ?? 0;
    }
}