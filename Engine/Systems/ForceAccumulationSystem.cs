using Engine.Components;

namespace Engine.Systems;

public class ForceAccumulationSystem {
    private readonly EffectDispatchSystem _dispatch;
    private readonly List<string> _pending = new();

    public ForceAccumulationSystem(EffectDispatchSystem dispatch) {
        _dispatch = dispatch;
    }

    public void Enqueue(string effect) => _pending.Add(effect);
    public void Enqueue(IEnumerable<string> effects) => _pending.AddRange(effects);

    public void Commit()
    {
        foreach (var effect in _pending)
            _dispatch.Apply(effect);
        _pending.Clear();
    }

    public void Discard() => _pending.Clear();
}