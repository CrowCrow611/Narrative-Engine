namespace Engine.AST;

public record ChoiceBranch(
    string Text,
    string TargetId,
    string? Condition
);