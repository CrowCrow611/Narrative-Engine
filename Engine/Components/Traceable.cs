namespace Engine.Components;

public class Traceable {
    public List<string> VisitedNodes { get; } = new();
    public List<string> StatChanges { get; } = new();

    public void RecordVisit(string nodeId) => VisitedNodes.Add(nodeId);
    public void RecordStatChange(string entry) => StatChanges.Add(entry);
    public bool HasVisited(string nodeId) => VisitedNodes.Contains(nodeId); 
}