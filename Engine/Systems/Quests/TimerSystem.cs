using Engine.Components.Quests;
using Engine.Events;

namespace Engine.Systems.Quests;

public record StageTimedOutEvent(string QuestId, string StageId);
public record QuestTimedOutEvent(string QuestId);

public class TimerSystem
{
    private readonly EventBus? _bus;
    private readonly EffectDispatchSystem? _effects;

    public TimerSystem(EventBus? bus = null, EffectDispatchSystem? effects = null)
    {
        _bus = bus;
        _effects = effects;
    }

    public void Step(Quest quest, float deltaTime)
    {
        if (quest.Status != QuestStatus.Active) return;

        var stage = quest.CurrentStage;
        if (stage is not null && stage.TimerSeconds is float stageRemaining)
        {
            stageRemaining -= deltaTime;
            stage.TimerSeconds = stageRemaining;
            if (stageRemaining <= 0f)
                HandleStageTimeout(quest, stage);
        }

        if (quest.Status == QuestStatus.Active && quest.TimerSeconds is float questRemaining)
        {
            questRemaining -= deltaTime;
            quest.TimerSeconds = questRemaining;
            if (questRemaining <= 0f)
                HandleQuestTimeout(quest);
        }
    }

    private void HandleStageTimeout(Quest quest, QuestStage stage)
    {
        if (_effects is not null)
            foreach (var e in stage.TimeoutEffects)
                _effects.Apply(e);

        _bus?.Publish(new StageTimedOutEvent(quest.Id, stage.Id));

        if (stage.TimeoutStageId is not null)
            quest.CurrentStageId = stage.TimeoutStageId;
    }

    private void HandleQuestTimeout(Quest quest)
    {
        if (_effects is not null)
            foreach (var e in quest.TimeoutEffects)
                _effects.Apply(e);

        _bus?.Publish(new QuestTimedOutEvent(quest.Id));

        if (quest.TimeoutStageId is not null)
            quest.CurrentStageId = quest.TimeoutStageId;
    }
}