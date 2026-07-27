namespace Engine.Components.Quests;

public enum QuestStatus { Inactive, Active, Completed, Failed }

public class Objective
{
    public string Id { get; }
    public string Description { get; }
    public string Condition { get; }
    public bool IsComplete { get; set; } = false;

    public Objective(string id, string description, string condition)
    {
        Id = id;
        Description = description;
        Condition = condition;

    }
}

public class ObjectiveGroup
{
    public string Id { get; }
    public List<Objective> Objectives { get; } = new();
    public bool RequireAll { get; set; } = true;

    public ObjectiveGroup(string id, bool requireAll = true)
    {
        Id = id;
        RequireAll = requireAll;
    }

    public bool IsComplete =>
        Objectives.Count > 0 &&
        (RequireAll ? Objectives.All(o => o.IsComplete) : Objectives.Any(o => o.IsComplete));
}

public class Transition
{
    public string TargetStageId { get; }
    public string? WhenGroup { get; set; }
    public string? Require { get; set; }

    public Transition(string targetStageId)
    {
        TargetStageId = targetStageId;
    }
}

public class QuestStage
{
    public string Id { get; }
    public List<ObjectiveGroup> Groups { get; } = new();

    public string? NextStageId { get; set; }
    public List<Transition> Transitions { get; } = new();
    public List<string> Effects { get; } = new();
    public QuestStage(string id) => Id = id;
    public float? TimerSeconds { get; set; }
    public string? TimeoutStageId { get; set; }
    public List<string> TimeoutEffects { get; } = new();
}

public class Quest
{
    public string Id { get; }
    public string Name { get; set; }
    public Dictionary<string, QuestStage> Stages { get; } = new();
    public string StartStageId { get; set; }
    public QuestStatus Status { get; set; } = QuestStatus.Inactive;
    public string? CurrentStageId { get; set; }
    public bool IsHidden { get; set; } = false;
    public string? ActivationCondition { get; set; }
    public float? TimerSeconds { get; set; }
    public List<string> RewardEffects { get; } = new();
    public string? TimeoutStageId { get; set; }
    public List<string> TimeoutEffects { get; } = new();
    public List<string> UnlocksQuestIds { get; } = new(); 

    public Quest(string id, string name, string startStageId)
    {
        Id = id;
        Name = name;
        StartStageId = startStageId;
    }

    public void AddStage(QuestStage stage) => Stages[stage.Id] = stage;
    public QuestStage? CurrentStage =>
        CurrentStageId is not null ? Stages.GetValueOrDefault(CurrentStageId) : null;
}

public class QuestState
{
    public Dictionary<string, Quest> Quests { get; } = new();
    public void Add(Quest quest) => Quests[quest.Id] = quest;
    public Quest? Get(string questId) => Quests.TryGetValue(questId, out var q) ? q : null;
}