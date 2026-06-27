using Engine.Components;
using Engine.Systems;

namespace Engine.Tests;

public class ConditionEvaluatorTests{
    private static ConditionEvetorSys Build(Action<WorldState>? setup = null) {
        var world = new WorldState();
        setup?.Invoke(world);
        return new ConditionEvetorSys(world);
    }

    [Fact]
    public void NullCondition_ReturnsTrue() {
        var eval = Build();
        Assert.True(eval.Evaluate(null));
    }

    [Fact]
    public void EmptyCondition_ReturnsTrue() {
        var eval = Build();
        Assert.True(eval.Evaluate(""));
    }

    [Fact]
    public void FlagCondition_TrueWhenSet() {
        var eval = Build(w => w.SetFlag("met_sanjay", true));
        Assert.True(eval.Evaluate("flag.met_sanjay"));
    }

    [Fact]
    public void FlagCondition_FalseWhenNotSet() {
        var eval = Build();
        Assert.False(eval.Evaluate("flag.met_sanjay"));
    }

    [Fact]
    public void UnsupportedCondition_Throws() {
        var eval = Build();
        Assert.Throws<InvalidOperationException>(() => { eval.Evaluate("counter.gold >= 100"); });
    }
}