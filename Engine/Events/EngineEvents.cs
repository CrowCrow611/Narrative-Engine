namespace Engine.Events;

public record NodeEnteredEvent(string NodeId);
public record ChoiceMadeEvent(string NodeId, int BranchIndex, string TargetId);
public record FlagChangedEvent(string Key, bool Value);
public record CounterChangedEvent(string Key, int Value);