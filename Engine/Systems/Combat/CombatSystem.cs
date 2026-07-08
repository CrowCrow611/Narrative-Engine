using Engine.Components;
using Engine.Events;


namespace Engine.Systems;

public record CombatStartedEvent(List<string> CombatantIds);
public record TurnStartedEvent(string CombatantId, int Round);
public record ActionResolvedEvent(string ActorId, string TargetId,
    string ActionId, float Damage, bool Hit);
public record CombatantDiedEvent(string CombatantId);
public record CombatEndedEvent(bool PlayerWon);
public record StatusEffectAppliedEvent(string CombatantId, string EffectName);
public record ComboTriggeredEvent(string ActorId, string ComboName, int ChainLength);

public class CombatSystem
{
    private readonly EventBus? _bus;
    private readonly EffectDispatchSystem? _effects;
    private readonly StatSystem _stats;
    private readonly Random _rng;

    private readonly Dictionary<string, List<string>> _comboDefs = new();
    private readonly Dictionary<string,
        Func<Combatant, List<Combatant>, string>> _aiBehaviours = new();

    public CombatSystem(StatSystem stats, EventBus? bus = null,
        EffectDispatchSystem? effects = null, int seed = 0)
    {
        _stats = stats;
        _bus = bus;
        _effects = effects;
        _rng = seed == 0 ? new Random() : new Random(seed);
    }

    public void RegisterCombo(string name, List<string> sequence) =>
        _comboDefs[name] = sequence;

    public void RegisterAI(string behaviourId,
        Func<Combatant, List<Combatant>, string> selector) => 
        _aiBehaviours[behaviourId] = selector; 

public Queue<Combatant> StartCombat(CombatState state)
    {
        state.IsActive = true;
        state.Round = 0;

        RollInitiative(state);

        var queue = BuildTurnQueue(state);

        _bus?.Publish(new CombatStartedEvent(
            state.Combatants.Select(c => c.Id).ToList()));

        return queue;
    }

    private void RollInitiative(CombatState state)
    {
        foreach (var c in state.Combatants)
        {
            if (state.InitiativeFormula is null)
            {
                var speed = _stats.Get(c.Stats, "speed");
                c.Initiative = speed + _rng.Next(1, 21);
            }
        }
    }

    private Queue<Combatant> BuildTurnQueue(CombatState state)
    {
        var ordered = state.Alive 
            .OrderByDescending(c => c.Initiative)
            .ToList();

        return new Queue<Combatant>(ordered);
    }

    public Combatant? NextTurn(CombatState state, Queue<Combatant> queue)
    {
        while (queue.Count > 0)
        {
            var next = queue.Dequeue();
            if (next.Status == CombatantStatus.Dead ||
                next.Status == CombatantStatus.Fled)
                continue;

            if (next.Status == CombatantStatus.Stunned)
            {
                queue.Enqueue(next);
                TickStatusEffects(state, next);
                continue;
            }

            if (queue.Count == 0)
            {
                state.Round++;
                foreach (var c in state.Alive)
                    if (!queue.Contains(c))
                        queue.Enqueue(c);
            }

            _bus?.Publish(new TurnStartedEvent(next.Id, state.Round));
            TickStatusEffects(state, next);
            return next;
        }

        return null;
    }

    public void ResolveAction(CombatState state, Combatant actor,
        Combatant target, string actionId, float baseDamage)
    {
        _stats.Recalculate(actor.Stats);
        _stats.Recalculate(target.Stats);

        var hitChance = _stats.Get(target.Stats, "accuracy");
        var hit = _rng.NextDouble() * 100 <= hitChance;
        var damage = 0f;

        if (hit)
        {
            var defense = _stats.Get(target.Stats, "defense");
            damage = state.WoundFormula is null 
                ? Math.Max(0, baseDamage - defense * 0.05f)
                : baseDamage;

            ApplyDamage(state, target, damage);
        }

        state.ComboHistory.Add(new ComboEntry(actionId, actor.Id, state.Round));
        CheckCombos(state, actor);

        _bus?.Publish(new ActionResolvedEvent(
            actor.Id, target.Id, actionId, damage, hit));
    }
    private void ApplyDamage(CombatState state, Combatant target, float damage)
    {
        target.CurrentHP = Math.Max(0, target.CurrentHP - damage);
        if (target.CurrentHP <= 0)
            KillCombatant(state, target);
    }

    private void KillCombatant(CombatState state, Combatant target)
    {
        target.Status = CombatantStatus.Dead;
        _bus?.Publish(new CombatantDiedEvent(target.Id));
        CheckVictory(state);
    }

    private void CheckVictory(CombatState state)
    {
        var playersAlive = state.Alive.Any(c => c.IsPlayer);
        var enemiesAlive = state.Alive.Any(c => !c.IsPlayer);

        if (!enemiesAlive)
        {
            state.IsActive = false;
            _bus?.Publish(new CombatEndedEvent(true));
        }
        else if (!playersAlive)
        {
            state.IsActive = false;
            _bus?.Publish(new CombatEndedEvent(false));
        }
    }

    public void ApplyStatusEffect(CombatState state, Combatant target, StatusEffect effect)
    {
        target.StatusEffects.Add(effect);
        if (effect.StunsTurn) target.Status = CombatantStatus.Stunned;

        if (_effects is not null)
            foreach (var e in effect.OnApply)
                _effects.Apply(e);

        _bus?.Publish(new StatusEffectAppliedEvent(target.Id, effect.Name));
    }

    private void TickStatusEffects(CombatState state, Combatant combatant)
    {
        foreach (var effect in combatant.StatusEffects.ToList())
        {
            if (effect.DamagePerTurn > 0) 
                ApplyDamage(state, combatant, effect.DamagePerTurn);

            if (_effects is not null)
                foreach (var e in effect.OnTick)
                    _effects.Apply(e);

            if (effect.Duration > 0) effect.Duration--;

            if (effect.Duration == 0)
            {
                combatant.StatusEffects.Remove(effect);
                if (effect.StunsTurn && 
                    !combatant.StatusEffects.Any(e => e.StunsTurn))
                    combatant.Status = CombatantStatus.Active;

                if (_effects is not null)
                    foreach (var e in effect.OnExpire)
                        _effects.Apply(e);
            }
        }
    }

    private void CheckCombos(CombatState state, Combatant actor)
    {
        var recentActions = state.ComboHistory
            .Where(e => e.ActorId == actor.Id)
            .TakeLast(5)
            .Select(e => e.ActionId)
            .ToList();

        foreach (var (comboName, sequence) in _comboDefs)
        {
            if (recentActions.Count < sequence.Count) continue;

            var tail = recentActions
                .TakeLast(sequence.Count)
                .ToList();

            if (tail.SequenceEqual(sequence))
                _bus?.Publish(new ComboTriggeredEvent(
                    actor.Id, comboName, sequence.Count));
        }
    }

    public string ResolveAI(Combatant actor, List<Combatant> targets,
        string behaviourId)
    {
        if (_aiBehaviours.TryGetValue(behaviourId,out var selector))
            return selector(actor, targets);

        return "basic_attack";
    }
}