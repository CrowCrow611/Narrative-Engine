using Engine.Components;
using Engine.Systems;

namespace Engine.Tests;

public class SST
{
    private static (StatSystem, StatBlock) Build()
    {
        var system = new StatSystem();
        var block = new StatBlock();
        return (system, block);
    }

    [Fact]
    public void BaseStatRC()
    {
        var (sys, block) = Build();
        block.SetBase("strength", 10f);
        sys.Recalculate(block);
        Assert.Equal(10f, sys.Get(block, "strength"));
    }

    [Fact]
    public void AdditiveModifier_SoB()
    {
        var (sys, block) = Build();
        block.SetBase("strength", 10f);
        block.AddModifier(new Modifier("strength", ModifierSource.Equipment, 5f, false));
        sys.Recalculate(block);
        Assert.Equal(15f, sys.Get(block, "strength"));
    }

    [Fact]
    public void SetModifier_OverB()
    {
        var (sys, block) = Build();
        block.SetBase("strength", 10f);
        block.AddModifier(new Modifier("strength", ModifierSource.Permanent, 20f, true));
        sys.Recalculate(block);
        Assert.Equal(20f, sys.Get(block, "strength"));
    }

    [Fact]
    public void SetTA_RC()
    {
        var (sys, block) = Build();
        block.SetBase("strength", 10f);
        block.AddModifier(new Modifier("strength", ModifierSource.Permanent, 10f, true));
        block.AddModifier(new Modifier("strength", ModifierSource.Equipment, 5f, false));
        sys.Recalculate(block);
        Assert.Equal(15f, sys.Get(block, "strength"));
    }

    [Fact]
    public void Clamp_CapsStat()
    {
        var (sys, block) = Build();
        block.SetBase("health", 150f);
        sys.SetClamp("health", 0f, 100f);
        sys.Recalculate(block);
        Assert.Equal(100f, sys.Get(block, "health"));
    }

    [Fact]
    public void RemoveModifier_ByID()
    {
    var (sys, block) = Build();
    block.SetBase("strength", 10f);
    block.AddModifier(new Modifier("strength", ModifierSource.Equipment, 5f, false, "sword"));
    block.RemoveModifier("sword");
    sys.Recalculate(block);
    Assert.Equal(10f, sys.Get(block, "strength"));
    }

    [Fact]
    public void MultiplierModifier_AAF()
    {
        var (sys, block) = Build();
        block.SetBase("strength", 10f);
        block.AddModifier(new Modifier("strength", ModifierSource.Equipment, 5f, false));
        block.AddModifier(new Modifier("strength", ModifierSource.Multiplier, 0.5f, false));
        sys.Recalculate(block);
        Assert.Equal(22.5f, sys.Get(block, "strength"));
            }

}