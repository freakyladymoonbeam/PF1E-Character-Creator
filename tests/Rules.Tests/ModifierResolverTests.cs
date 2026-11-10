using Rules.Core;

namespace Rules.Tests;

public class ModifierResolverTests
{
    // Tests for the ModifierResolver class.
    [Fact]
    public void SameTypeBonuses_OnlyHighestCounts()
    {
        var mods = new[]
        {
            new Modifier("AC", 2, BonusType.Enhancement, "Ring of Protection +2"),
            new Modifier("AC", 1, BonusType.Enhancement, "Amulet of Natural Armor"),
        };
        Assert.Equal(2, ModifierResolver.Total(mods));
    }

    // Tests that different types of bonuses stack correctly.
    [Fact]
    public void DifferentTypeBonuses_Stack()
    {
        var mods = new[]
        {
            new Modifier("AC", 4, BonusType.Armor, "Chainmail"),
            new Modifier("AC", 2, BonusType.Shield, "Heavy shield"),
        };
        Assert.Equal(6, ModifierResolver.Total(mods));
    }

    // Tests that dodge bonuses stack correctly.
    [Fact]
    public void DodgeBonuses_Stack()
    {
        var mods = new[]
        {
            new Modifier("AC", 1, BonusType.Dodge, "Dodge feat"),
            new Modifier("AC", 1, BonusType.Dodge, "Haste"),
        };
        Assert.Equal(2, ModifierResolver.Total(mods));
    }

    // Tests that penalties always stack.
    [Fact]
    public void Penalties_AlwaysStack()
    {
        var mods = new[]
        {
            new Modifier("AC", -1, BonusType.Untyped, "Fatigued"),
            new Modifier("AC", -2, BonusType.Untyped, "Prone"),
        };
        Assert.Equal(-3, ModifierResolver.Total(mods));
    }
}