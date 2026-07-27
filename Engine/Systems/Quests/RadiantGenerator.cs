using Engine.Components.Quests;
using Engine.Events;

namespace Engine.Systems.Quests;

public record QuestGeneratedEvent(string TemplateId, string QuestId);

public class RadiantGenerator
{
    private readonly ConditionEvetorSys? _conditions;
    private readonly EventBus? _bus;
    private readonly Random _rng;
    private const int RetryLimit = 20;

    public RadiantGenerator(ConditionEvetorSys? conditions = null,
        EventBus? bus = null, int seed = 0)
    {
        _conditions = conditions;
        _bus = bus;
        _rng = seed == 0 ? new Random() : new Random(seed);
    }

    public Quest? Generate(QuestTemplate template, string actorId = "player")
    {
        var chosen = new Dictionary<string, string>();

        for (var attempt = 0; attempt <RetryLimit; attempt++)
        {
            chosen.Clear();

            foreach (var slot in template.Slots)
            {
                var valid = slot.Candidates
                    .Where(c => c.Weight > 0f)
                    .Where(c => c.Condition is null ||
                        (_conditions?.Evaluate(c.Condition, actorId) ?? true))
                    .ToList();

                if (valid.Count == 0) return null;

                chosen[slot.Name] = PickWeighted(valid);
            }

            var signature = string.Join("|", template.Slots.Select(s => chosen[s.Name]));
            var isRepeat = template.NeverRepeatCount > 0 && template.IsRecentCombo(signature);
            var outOfRetries = attempt == RetryLimit - 1;

            if (!isRepeat || outOfRetries)
            {
                template.RecordCombo(signature);
                return BuildQuest(template, chosen);
            }
        }

        return null;
    }

    private string PickWeighted(List<SlotCandidate> candidates)
    {
        var totalWeight = candidates.Sum(c => c.Weight);
        var roll = _rng.NextDouble() * totalWeight;
        var cumulative = 0.0;

        foreach (var c in candidates)
        {
            cumulative += c.Weight;
            if (roll <= cumulative) return c.Value;
        }

        return candidates[^1].Value;
    }

    private Quest BuildQuest(QuestTemplate template, Dictionary<string, string> values)
    {
        var questId = $"{template.Id}_{Guid.NewGuid():N}";
        var name = Substitute(template.NamePattern, values);

        var quest = new Quest(questId, name, template.StartStageId)
        {
            TimerSeconds = template.TimerSeconds,
            IsHidden = template.IsHidden,
            ActivationCondition = template.ActivationCondition is null
                ? null : Substitute(template.ActivationCondition, values),
            TimeoutStageId = template.TimeoutStageId
        };

        foreach (var effect in template.TimeoutEffects)
            quest.TimeoutEffects.Add(Substitute(effect, values));

        foreach (var stageTemplate in template.Stages.Values)
        {
            var stage = new QuestStage(stageTemplate.Id)
            {
                NextStageId = stageTemplate.NextStageId,
                TimerSeconds = stageTemplate.TimerSeconds,
                TimeoutStageId = stageTemplate.TimeoutStageId
            };

            foreach (var groupTemplate in stageTemplate.Groups)
            {
                var group = new ObjectiveGroup(groupTemplate.Id, groupTemplate.RequireAll);
                foreach (var objTemplate in groupTemplate.Objectives)
                {
                    group.Objectives.Add(new Objective(
                        objTemplate.Id,
                        Substitute(objTemplate.Description, values),
                        Substitute(objTemplate.Condition, values)));
                }
                stage.Groups.Add(group);
            }

            foreach (var TransitionTemplate in stageTemplate.Transitions)
            {
                stage.Transitions.Add(new Transition(TransitionTemplate.TargetStageId)
                {
                    WhenGroup = TransitionTemplate.WhenGroup,
                    Require = TransitionTemplate.Require is null
                        ? null : Substitute(TransitionTemplate.Require, values)
                });
            }

            foreach (var effect in stageTemplate.Effects)
                stage.Effects.Add(Substitute(effect, values));

            foreach (var effect in stageTemplate.TimeoutEffects)
                stage.TimeoutEffects.Add(Substitute(effect, values));

            quest.AddStage(stage);
        }

        foreach (var effect in template.RewardEffects)
            quest.RewardEffects.Add(Substitute(effect, values));

        _bus?.Publish(new QuestGeneratedEvent(template.Id, quest.Id));

        return quest;
    }

    private static string Substitute(string text, Dictionary<string, string> values)
    {
        foreach (var (key, val) in values)
            text = text.Replace("{" + key + "}", val);
        return text;
    }
}