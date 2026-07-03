namespace Engine.Components;

public enum ModifierSource
{
    Base, Permanent, Equipment, Status, Spell, Emotional, Weather, Multiplier
}

public record Modifier(
    string StatName,
    ModifierSource Source,
    float Value,
    bool IsSet,
    string? Id = null
);

public class StatBlock
{
    public Dictionary<string, float> Base { get; } = new();

    public List<Modifier> Modifiers { get; } = new();

    public Dictionary<string, float> Derived { get; } = new();

    public int Level { get; set; } = 1;
    public int XP { get; set; } = 0;
    public string? Class { get; set; } 

    public void SetBase(string stat, float value) => Base[stat] = value;
    public void AddModifier(Modifier mod) => Modifiers.Add(mod);
    public void RemoveModifier(string id) =>  Modifiers.RemoveAll(m => m.Id == id);
}