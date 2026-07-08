using Engine.Components;
namespace Engine.Components;

public enum CombatantStatus { Active, Stunned, Dead, Fled }

public class Combatant
{
    public string Id { get;} 
    public string DisplayName { get; }
    public StatBlock Stats { get; }
    public bool IsPlayer { get; }
    public CombatantStatus Status { get; set; } = CombatantStatus.Active;
    public float Initiative { get; set; } = 0f;
    public int ExtraTurns { get; set; } = 0; 

    public float CurrentHP { get; set; }
    public float MaxHP { get; set; }

    public List<StatusEffect> StatusEffects { get; } = new();

    public Combatant(string id, string displayName, StatBlock stats, bool isPlayer)
    {
        Id = id;
        DisplayName = displayName;
        Stats = stats;
        IsPlayer = isPlayer;
    }
}

public class StatusEffect
{
    public string Id { get; }
    public string Name { get; }
    public int Duration { get; set; }
    public List<string> OnApply { get; } = new();
    public List<string> OnTick { get; } = new();
    public List <string> OnExpire { get;} = new();
    public bool StunsTurn { get; set; } = false;
    public float DamagePerTurn { get; set; } = 0f;

    public StatusEffect(string id, string name, int duration)
    {
        Id = id;
        Name = name;
        Duration = duration;
    }
}

public class ComboEntry
{
    public string ActionId { get; }
    public string ActorId { get; }
    public int Round { get; }

    public ComboEntry(string actionId, string actorId, int round)
    {
        ActionId = actionId;
        ActorId = actorId;
        Round = round;
    }
}

public class CombatState
{
    public List<Combatant> Combatants { get; } = new();
    public List<ComboEntry> ComboHistory { get; } = new();
    public int Round { get; set; } = 0;
    public bool IsActivve { get; set; } = false;
    public string? VictoryCondition { get; set; }
    public string? DefeatCondition { get; set; }

    public string? InitiativeFormula { get; set; }
    public string? WoundFormula { get; set; }
    public bool IsActive { get; set; } = false;
    public void AddCombatant(Combatant c) => Combatants.Add(c);

    public Combatant? Find(string id) => Combatants.FirstOrDefault(c => c.Id == id);
    public List<Combatant> Alive => Combatants.Where(c => c.Status != CombatantStatus.Dead &&
                                                        c.Status != CombatantStatus.Fled).ToList();
    public List<Combatant> ActiveThisTurn => Alive.Where(c => c.Status == CombatantStatus.Active).ToList(); 

}