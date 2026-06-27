using Engine.AST;

namespace Engine.Tests;

public class AstTests {

    [Fact]
    public void StoryNode_HolsIdandKind() {

        var node = new StoryNode(
            Id: "tavern_entrance",
            Kind: NodeKind.Beat,
            Text: "You push open the heavy door.",
            Branches: [],
            Connections: [],
            Tags: [],
            Condition: null,
            Effects: [],
            Children: [],
            SourceLine: 1,
            SourceColumn: 1
        );

        Assert.Equal("tavern_entrance", node.Id);
        Assert.Equal(NodeKind.Beat, node.Kind);
        Assert.Equal("You push open the heavy door.", node.Text);
    }

    [Fact]
    public void ChoiceBranch_HoldsTextAndTarget() {
        var branch = new ChoiceBranch(
            Text: "Order a drink.",
            TargetId: "tavern_drink",
            Condition: null
        );

        Assert.Equal("Order a drink.", branch.Text);
        Assert.Equal("tavern_drink", branch.TargetId);
        Assert.Null(branch.Condition);
    }

    [Fact]
    public void SNode_VBranches_HolsCorrCount() {
        var node = new StoryNode(
            Id: "first_choice",
            Kind: NodeKind.Choice,
            Text: null,
            Branches: [
                new("Order a drink.", "tavern_drink", null),
                new("Leave.", "town_square", null),
            ],
            Connections: [],
            Tags: [],
            Condition: null,
            Effects: [],
            Children: [],
            SourceLine: 5,
            SourceColumn: 1
        );

        Assert.Equal(2, node.Branches.Count);
        Assert.Equal("tavern_drink", node.Branches[0].TargetId);
    }
}