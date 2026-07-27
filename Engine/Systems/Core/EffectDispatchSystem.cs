using Engine.Components;

namespace Engine.Systems;
public interface IEffectHandler
{
    bool CanHandle(string prefix);
    void Apply(string effect);
}

public class EffectDispatchSystem
{
    private readonly WorldState _world;
    private readonly List<IEffectHandler> _handlers = new();

    public EffectDispatchSystem(WorldState world)
    {
        _world = world;
    }

    public void RegisterHandler(IEffectHandler handler) => _handlers.Add(handler);

    public void Apply(string effect)
    {
        var parts = effect.Trim().Split(' ');

        if (parts.Length == 3 && parts[0].StartsWith("flag."))
        {
            var key = parts[0].Substring(5);
            var val = parts[2] == "true";
            _world.SetFlag(key, val);
            return;
        }

        if (parts.Length == 3 && parts[0].StartsWith("counter."))
        {
            var key = parts[0].Substring(8);
            var n = int.Parse(parts[2]);
            var cur = _world.GetCounter(key);

            _world.SetCounter(key, parts[1] switch
            {
                "+=" => cur + n,
                "-=" => cur - n,
                "=" => n,
                _ => throw new InvalidOperationException(
                    $"Unknown counter operation: '{parts[1]}'")
            });
            return;
        }

        var prefix = parts[0].Split('.')[0];
        var handler = _handlers.FirstOrDefault(h => h.CanHandle(prefix));
        if (handler is not null)
        {
            handler.Apply(effect);
            return;
        }

        throw new InvalidOperationException($"Unsupported effect: '{effect}'");
    }
}