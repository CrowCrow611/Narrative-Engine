namespace Engine.AST;

public record StoryNode(
    string Id,
    NodeKind Kind,
    string? Text,
    List<ChoiceBranch> Branches,
    List<string> Connections,
    List<string> Tags,
    string? Condition,
    List<string> Effects,
    List<StoryNode> Children,
    int SourceLine,
    int SourceColumn 
);