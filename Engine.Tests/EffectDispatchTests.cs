// Engine.Tests/EffectDispatchTests.cs
using Engine.Components;
using Engine.Systems;

namespace Engine.Tests;

public class EffectDispatchTests
{
    private static (EffectDispatchSystem, WorldState) Build()
    {
        var world = new WorldState();
        return (new EffectDispatchSystem(world), world);
    }

    [Fact]
    public void SetFlag_True()
    {
        var (dispatch, world) = Build();
        dispatch.Apply("flag.met_mira = true");
        Assert.True(world.GetFlag("met_mira"));
    }

    [Fact]
    public void SetFlag_False()
    {
        var (dispatch, world) = Build();
        world.SetFlag("met_mira", true);
        dispatch.Apply("flag.met_mira = false");
        Assert.False(world.GetFlag("met_mira"));
    }

    [Fact]
    public void CounterAssign()
    {
        var (dispatch, world) = Build();
        dispatch.Apply("counter.gold = 100");
        Assert.Equal(100, world.GetCounter("gold"));
    }

    [Fact]
    public void CounterAdd()
    {
        var (dispatch, world) = Build();
        dispatch.Apply("counter.gold = 100");
        dispatch.Apply("counter.gold += 50");
        Assert.Equal(150, world.GetCounter("gold"));
    }

    [Fact]
    public void CounterSubtract()
    {
        var (dispatch, world) = Build();
        dispatch.Apply("counter.gold = 100");
        dispatch.Apply("counter.gold -= 30");
        Assert.Equal(70, world.GetCounter("gold"));
    }

    [Fact]
    public void ForceAccumulation_CommitsAll()
    {
        var world    = new WorldState();
        var dispatch = new EffectDispatchSystem(world);
        var accum    = new ForceAccumulationSystem(dispatch);

        accum.Enqueue("flag.met_mira = true");
        accum.Enqueue("counter.gold = 50");
        accum.Commit();

        Assert.True(world.GetFlag("met_mira"));
        Assert.Equal(50, world.GetCounter("gold"));
    }

    [Fact]
    public void ForceAccumulation_Discard_AppliesNothing()
    {
        var world    = new WorldState();
        var dispatch = new EffectDispatchSystem(world);
        var accum    = new ForceAccumulationSystem(dispatch);

        accum.Enqueue("flag.met_mira = true");
        accum.Discard();

        Assert.False(world.GetFlag("met_mira"));
    }
}