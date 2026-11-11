using Rules.Core;
using Rules.Core.Data;

namespace Rules.Tests;

// Unit tests for hit point calculations in characters.
public class HitPointTests
{
    private static Character Make(
        ClassDefinition cls, int level, int con = 10,
        List<int>? rolls = null, RaceDefinition? race = null)
    {
        var character = new Character { Race = race };
        character.Classes.Add(new ClassLevel(cls, level, rolls));
        character.BaseAbilityScores[Ability.Constitution] = con;
        return character;
    }

    // Tests that a level one Fighter has maximum hit die plus Constitution modifier.
    [Theory]
    [InlineData(10, 10)]
    [InlineData(14, 12)]   // Con +2
    [InlineData(8, 9)]     // Con -1
    public void Fighter_LevelOne_IsMaxHitDiePlusCon(int con, int expected)
    {
        Assert.Equal(expected, Make(TestData.Fighter(), 1, con).MaxHitPoints);
    }

    // Tests that a level one Wizard has six hit points.
    [Fact]
    public void Wizard_LevelOne_IsSix()
    {
        Assert.Equal(6, Make(TestData.Wizard(), 1).MaxHitPoints);
    }

    
    // Tests that a level three Fighter uses average hit points when no rolls are recorded.
    [Fact]
    public void Fighter_LevelThree_UsesAverageWhenNoRollsRecorded()
    {
        // 10 (max) + 6 (average d10) + 6 = 22
        Assert.Equal(22, Make(TestData.Fighter(), 3).MaxHitPoints);
    }
 
    // Tests that a level three Fighter uses recorded hit point rolls.
    [Fact]
    public void Fighter_LevelThree_UsesRecordedRolls()
    {
        // Con 12 is +1. Level 1: 10+1, level 2: 3+1, level 3: 8+1 = 24
        var character = Make(TestData.Fighter(), 3, con: 12, rolls: [10, 3, 8]);

        Assert.Equal(24, character.MaxHitPoints);
    }

    // Tests that each level gives at least one hit point, even with a negative Constitution modifier.
    [Fact]
    public void EachLevelGivesAtLeastOneHitPoint()
    {
        // Con 3 is -4. Level 1: 6-4 = 2. Level 2: 4-4 = 0, raised to 1. Total 3.
        Assert.Equal(3, Make(TestData.Wizard(), 2, con: 3).MaxHitPoints);
    }

    // Tests that an Elf Wizard's Constitution penalty reduces hit points.
    [Fact]
    public void ElfWizard_ConPenaltyReducesHitPoints()
    {
        // Elf Con -2 turns 10 into 8, a -1 modifier: 6 - 1 = 5
        Assert.Equal(5, Make(TestData.Wizard(), 1, race: TestData.Elf()).MaxHitPoints);
    }

    // Tests that only the first class gets maximum hit points at level one in a multiclass character.
    [Fact]
    public void Multiclass_OnlyFirstClassGetsMaxHitPointsAtLevelOne()
    {
        var character = new Character();
        character.Classes.Add(new ClassLevel(TestData.Fighter(), 1));
        character.Classes.Add(new ClassLevel(TestData.Wizard(), 1));

        Assert.Equal(14, character.MaxHitPoints); // 10 (max d10) + 4 (average d6)
    }
    
    // Tests that a flat hit point modifier is added to the character's maximum hit points.
    [Fact]
    public void FlatHitPointModifier_IsAdded()
    {
        var character = Make(TestData.Fighter(), 1);
        character.Modifiers.Add(new Modifier("HP", 3, BonusType.Untyped, "Toughness"));

        Assert.Equal(13, character.MaxHitPoints);
    }

    // Tests that an invalid recorded hit point roll is rejected.
    [Fact]
    public void InvalidRecordedRoll_IsRejected()
    {
        // 11 is impossible on a d10
        var character = Make(TestData.Fighter(), 2, rolls: [10, 11]);

        Assert.Throws<ArgumentOutOfRangeException>(() => character.MaxHitPoints);
    }
}