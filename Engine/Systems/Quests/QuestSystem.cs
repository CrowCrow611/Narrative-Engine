using Engine.Components.Quests;
using Engine.Events;

namespace Engine.Systems.Quests;

public record QuestActivatedEvent(string QuestId);
public record ObjectiveCompletedEvent(string QuestId, string StageId, string GroupId, string ObjectiveId);
public record StageCompletedEvent(string QuestId, string StageId);
public record QuestCompletedEvent(string QuestId);
public record QuestFailedEvent(string QuestId, string Reason);

public class QuestSystem : IEffectHandler
{
    private readonly EventBus? _bus;
    private readonly EffectDispatchSystem? _effects;
    private readonly ConditionEvetorSys? _conditions;
    private readonly QuestState? _questState;

    public QuestSystem(EventBus? bus = null, EffectDispatchSystem? effects = null,
        ConditionEvetorSys? conditions = null, QuestState? questState = null)
    {
        _bus = bus;
        _effects = effects;
        _conditions = conditions;
        _questState = questState;
    }

    public void ActivateQuest(Quest quest)
    {
        if (quest.Status != QuestStatus.Inactive) return;

        quest.Status = QuestStatus.Active;
        quest.CurrentStageId = quest.StartStageId;

        _bus?.Publish(new QuestActivatedEvent(quest.Id));
    }

    public void CheckHiddenQuests(QuestState questState, string actorId = "player")
    {
        foreach (var quest in questState.Quests.Values)
        {
            if (!quest.IsHidden) continue;
            if (quest.Status != QuestStatus.Inactive) continue;
            if (quest.ActivationCondition is null) continue;
            if (_conditions is null) continue;

            if (_conditions.Evaluate(quest.ActivationCondition, actorId))
                ActivateQuest(quest);
        }
    }

    public void CheckObjectives(Quest quest, string actorId = "player")
    {
        if (quest.Status != QuestStatus.Active) return;
        var stage = quest.CurrentStage;
        if (stage is null) return;

        foreach (var group in stage.Groups)
        {
            foreach (var objective in group.Objectives)
            {
                if (objective.IsComplete) continue;
                if (_conditions is null) continue;

                if (_conditions.Evaluate(objective.Condition, actorId))
                {
                    objective.IsComplete = true;
                    _bus?.Publish(new ObjectiveCompletedEvent(
                        quest.Id, stage.Id, group.Id, objective.Id));
                }
            }
        }

        TryAdvance(quest, stage);
    }

    private void TryAdvance(Quest quest, QuestStage stage)
    {
        bool stageComplete = false;
        string? nextStageId = null;

        if (stage.Transitions.Count > 0)
        {
            foreach (var t in stage.Transitions)
            {
                var groupOk = t.WhenGroup is null ||
                    stage.Groups.FirstOrDefault(g => g.Id == t.WhenGroup)?.IsComplete == true;
                if (!groupOk) continue;

                var requireOk = t.Require is null ||
                    (_conditions?.Evaluate(t.Require) ?? true);
                if (!requireOk) continue;

                stageComplete = true;
                nextStageId = t.TargetStageId;
                break;
            }
        }
        else if (stage.Groups.Count > 0 && stage.Groups.All(g => g.IsComplete))
        {
            stageComplete = true;
            nextStageId = stage.NextStageId;
        }

        if (!stageComplete) return;
        CompleteStage(quest, stage, nextStageId);
    }

    private void CompleteStage(Quest quest, QuestStage stage, string? nextStageId)
    {
        if (_effects is not null)
            foreach (var effect in stage.Effects)
                _effects.Apply(effect);

        _bus?.Publish(new StageCompletedEvent(quest.Id, stage.Id));

        if (nextStageId is null)
        {
            CompleteQuest(quest);
            return;
        }

        quest.CurrentStageId = nextStageId;
    }

    private void CompleteQuest(Quest quest)
    {
        quest.Status = QuestStatus.Completed;

        if (_effects is not null)
            foreach (var effect in quest.RewardEffects)
                _effects.Apply(effect);

        _bus?.Publish(new QuestCompletedEvent(quest.Id));

        if (_questState is not null)
            foreach (var unlockedId in quest.UnlocksQuestIds)
            {
                var unlocked = _questState.Get(unlockedId);
                if (unlocked is not null)
                    ActivateQuest(unlocked);
            }
    }

    public void FailQuest(Quest quest, string reason)
    {
        if (quest.Status != QuestStatus.Active) return;
        quest.Status = QuestStatus.Failed;
        _bus?.Publish(new QuestFailedEvent(quest.Id, reason));
    }

    public bool CanHandle(string prefix) => prefix == "quest";

    public void Apply(string effect)
    {
        var parts = effect.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var path = parts[0].Split('.');

        if (path.Length < 3 || path[0] != "quest")
            throw new InvalidOperationException($"Malformed quest effect: '{effect}'");

        if (_questState is null)
            throw new InvalidOperationException(
                "QuestSystem has no QuestState registered - pass one to the constructor to use quest. effects.");

        var questId = path[1];
        var quest = _questState.Get(questId);
        if (quest is null)
            throw new InvalidOperationException($"Unknown quest '{questId}' in effect: '{effect}'");

        var action = string.Join('.', path.Skip(2));

        switch (action)
        {
            case "timer.cancel":
                quest.TimerSeconds = null;
                if (quest.CurrentStage is not null)
                    quest.CurrentStage.TimerSeconds = null;
                break;

            case "timer.set":
                if (parts.Length < 2 || !float.TryParse(parts[1], out var seconds))
                    throw new InvalidOperationException($"Missing/invalid timer value: '{effect}'");
                quest.TimerSeconds = seconds;
                break;

            case "fail":
                FailQuest(quest, "failed via effect");
                break;

            case "activate":
                ActivateQuest(quest);
                break;

            default:
                throw new InvalidOperationException($"Unknown quest action '{action}': '{effect}'");
        }
    }
}
