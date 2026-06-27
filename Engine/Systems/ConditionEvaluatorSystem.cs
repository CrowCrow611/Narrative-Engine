using Engine.Components;

namespace Engine.Systems;

public class ConditionEvetorSys {
    private readonly WorldState _world;

    public ConditionEvetorSys(WorldState world) {
        _world = world;
    }

    public bool Evaluate(string? condition) {
        if (string.IsNullOrWhiteSpace(condition)) return true;

        var parts = condition.Trim().Split('.');
        if (parts.Length == 2 && parts[0] == "flag")
            return _world.GetFlag(parts[1]);
        
        throw new InvalidOperationException( $"Unsupported condition: '{condition}'");
    }
}