namespace Engine.Components.Quests;

public class SlotCandidate
{
    public string Value { get; }
    public string? Condition { get; set; }
    public float Weight { get; set; } = 1f;

    public SlotCandidate(string value, string? condition = null, float weight = 1f)
    {
        Value = value;
        Condition = condition;
        Weight = weight;
    }
}

public class Slot
{
    public string Name { get; }
    public List<SlotCandidate> Candidates { get; } = new();

    public Slot(string name) => Name = name;
}

public class ObjectiveTemplate
{
    public string Id { get; }
    public string Description { get; set; } = "";
    public string Condition { get; set; } = "";

    public ObjectiveTemplate(string id) => Id = id;
}

public class ObjectiveGroupTemplate
{
    public string Id { get; }
    public bool RequireAll { get; set; } = true;
    public List<ObjectiveTemplate> Objectives { get; } = new();

    public ObjectiveGroupTemplate(string id) => Id = id;
}

public class TransitionTemplate
{
    public string TargetStageId { get; }
    public string? WhenGroup { get; set; }
    public string? Require { get; set; }

    public TransitionTemplate(string targetStageId) => TargetStageId = targetStageId;
}

public class QuestStageTemplate
{
    public string Id { get; }
    public List<ObjectiveGroupTemplate> Groups { get; } = new();
    public string? NextStageId { get; set; }
    public List<TransitionTemplate> Transitions { get; } = new();
    public List<string> Effects { get; } = new();
    public float? TimerSeconds { get; set; }
    public string? TimeoutStageId { get; set; }
    public List<string> TimeoutEffects { get; } = new();

    public QuestStageTemplate(string id) => Id = id;
}

public class QuestTemplate
{
    public string Id { get; }
    public string NamePattern { get; set; } = "";
    public List<Slot> Slots { get; } = new();
    public Dictionary<string, QuestStageTemplate> Stages { get; } = new();
    public string StartStageId { get; set; } = "";
    public List<string> RewardEffects { get; } = new();
    public float? TimerSeconds { get; set; }
    public string? TimeoutStageId { get; set; }
    public List<string> TimeoutEffects { get; } = new();
    public bool IsHidden { get; set; } = false;
    public string? ActivationCondition { get; set; }

    public int NeverRepeatCount { get; set; } = 0;
    private readonly Queue<string> _history = new();
    public QuestTemplate(string id) => Id = id;
    public void AddStage(QuestStageTemplate stage) => Stages[stage.Id] = stage;

    internal bool IsRecentCombo(string signature) => _history.Contains(signature);

    internal void RecordCombo(string signature)
    {
        if (NeverRepeatCount <= 0) return;
        _history.Enqueue(signature);
        while (_history.Count > NeverRepeatCount)
            _history.Dequeue();
    }
}