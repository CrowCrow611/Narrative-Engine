using Engine.Components;
using Engine.Events;

namespace Engine.Systems;

public record SkillCheckResult(
    bool Success,
    bool CriticalSuccess,
    bool CriticalFailure,
    int Roll,
    float SkillValue,
    float Total,
    float Difficulty
);

public record SkillCheckRE(string ActorId, string SkillName, SkillCheckResult Result);

public class SkillCheckSystem
{
    private readonly StatSystem _stats;
    private readonly EventBus? _bus;
    private readonly Random _rng;
    private readonly int _sides;

    public SkillCheckSystem(StatSystem state, EventBus? bus = null, int sides = 20, int seed = 0)
    {
        _stats = state;
        _bus = bus;
        _sides = sides;
        _rng = seed == 0 ? new Random() : new Random(seed);
    }

    public SkillCheckResult Resolve(string actorid, StatBlock block, string skillName, float difficulty)
    {
        var roll = _rng.Next(1, _sides + 1);
        var skillValue = _stats.Get(block, skillName);
        var total = roll + skillValue;

        var criticalSuccess = roll == _sides;
        var criticalFailure = roll == 1;
        var success = criticalFailure ? false : (criticalSuccess ? true : total >= difficulty);

        var result = new SkillCheckResult(success, criticalSuccess, criticalFailure, roll, skillValue, total, difficulty);
        _bus?.Publish(new SkillCheckRE(actorid, skillName, result));
        return result;
    }
}