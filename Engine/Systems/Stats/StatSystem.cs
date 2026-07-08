using Engine.Components;

namespace Engine.Systems;

public class StatSystem
{
    private static readonly ModifierSource[] ResolutionOrder = [
        ModifierSource.Base,
        ModifierSource.Permanent,
        ModifierSource.Equipment,
        ModifierSource.Status,
        ModifierSource.Spell,
        ModifierSource.Emotional,
        ModifierSource.Weather,
        ModifierSource.Multiplier,
    ];

    private readonly Dictionary<string, (float Min, float Max)> _clamps = new();
    public void SetClamp(string stat, float min, float max) => _clamps[stat] = (min, max);

    public void Recalculate(StatBlock block)
    {
        var statNames = new HashSet<string>(block.Base.Keys);
        foreach (var mod in block.Modifiers)
            statNames.Add(mod.StatName);

        block.Derived.Clear();

        foreach (var stat in statNames)
        {
            float value = block.Base.TryGetValue(stat, out var b) ? b : 0f;
            
            foreach (var source in ResolutionOrder)
            {
                if (source == ModifierSource.Base) continue;

                var mods = block.Modifiers 
                    .Where(m => m.StatName == stat && m.Source == source)
                    .ToList();

                foreach (var mod in mods.Where(m => m.IsSet))
                    value = mod.Value;

                foreach (var mod in mods.Where(m => !m.IsSet)) 
                    value += mod.Value;

                if (source == ModifierSource.Multiplier)
                {
                    float multiplier = 1f;
                    foreach (var mod in mods.Where(m => !m.IsSet))
                        multiplier += mod.Value;

                    foreach (var mod in mods.Where(m => !m.IsSet))
                        value -= mod.Value;
                    value *= multiplier;
                }
            } 

            if (_clamps.TryGetValue(stat, out var clamp))
                value = Math.Clamp(value, clamp.Min, clamp.Max);

            block.Derived[stat] = value;
        }
    }

    public float Get(StatBlock block, string stat) =>
        block.Derived.TryGetValue(stat, out var v) ? v :
        block.Base.TryGetValue(stat, out var b) ? b : 0f;
}