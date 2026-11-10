using Rules.Core;
using Rules.Core.Data;

namespace Rules.Tests;

public class FighterTests
{
    // Helper methods to load the fighter class and elf race, and to create a fighter character of a specific level.
    private static ClassDefinition LoadFighter() => DataPackLoader.LoadClass(
        Path.Combine(AppContext.BaseDirectory, "data", "core", "classes", "fighter.json"));

    // Helper method to load the elf race from JSON.
    private static RaceDefinition LoadElf() => DataPackLoader.LoadRace(
        Path.Combine(AppContext.BaseDirectory, "data", "core", "races", "elf.json"));

    // Helper method to create a fighter character of a specific level.
    private static Character FighterOfLevel(int level)
    {
        var character = new Character();
        character.Classes.Add(new ClassLevel(LoadFighter(), level));
        return character;
    }

    // Tests that the fighter class loads correctly from its JSON definition.
    [Fact]
    public void Fighter_LoadsFromJson()
    {
        var fighter = LoadFighter();

        Assert.Equal("Fighter", fighter.Name);
        Assert.Equal(10, fighter.HitDie);
        Assert.Equal(2, fighter.SkillRanksPerLevel);
        Assert.Equal(BabProgression.Full, fighter.BabProgression);
        Assert.Equal(SaveProgression.Good, fighter.Fortitude);
        Assert.Equal(SaveProgression.Poor, fighter.Reflex);
        Assert.Equal(SaveProgression.Poor, fighter.Will);
        Assert.Equal(16, fighter.Features.Count);
        Assert.Contains("Swim", fighter.ClassSkills);
    }

    // Tests that the fighter's progression matches the core table for BAB and saves.
    [Theory]
    //        level  bab  fort  ref  will
    [InlineData(1,    1,   2,    0,   0)]
    [InlineData(2,    2,   3,    0,   0)]
    [InlineData(3,    3,   3,    1,   1)]
    [InlineData(4,    4,   4,    1,   1)]
    [InlineData(5,    5,   4,    1,   1)]
    [InlineData(10,  10,   7,    3,   3)]
    [InlineData(20,  20,  12,    6,   6)]
    public void Fighter_ProgressionMatchesCoreTable(
        int level, int bab, int fort, int reflex, int will)
    {
        var character = FighterOfLevel(level);

        Assert.Equal(bab, character.BaseAttackBonus);
        Assert.Equal(fort, character.BaseFortitude);
        Assert.Equal(reflex, character.BaseReflex);
        Assert.Equal(will, character.BaseWill);
    }

    // Tests that the fighter gains the correct number of bonus feats at each level.
    [Theory]
    [InlineData(1, 1)]
    [InlineData(4, 3)]
    [InlineData(20, 11)]
    public void Fighter_BonusFeatsAtLevel(int level, int expectedBonusFeats)
    {
        var character = FighterOfLevel(level);

        Assert.Equal(expectedBonusFeats,
            character.ClassFeatures.Count(f => f.Name == "Bonus Feat"));
    }

    // Tests that the fighter's features unlock at the correct levels.
    [Fact]
    public void Fighter_FeaturesUnlockAtTheRightLevel()
    {
        var names = FighterOfLevel(4).ClassFeatures.Select(f => f.Name).ToList();

        Assert.Contains("Bravery", names);
        Assert.Contains("Armor Training", names);
        Assert.DoesNotContain("Weapon Training", names);
    }

    // Tests that an elf fighter correctly combines race, class, and ability scores.
    [Fact]
    public void ElfFighter_CombinesRaceClassAndAbilityScores()
    {
        var character = new Character { Race = LoadElf() };
        character.Classes.Add(new ClassLevel(LoadFighter(), 1));
        character.BaseAbilityScores[Ability.Strength] = 16;
        character.BaseAbilityScores[Ability.Dexterity] = 14;     // 16 with elf bonus
        character.BaseAbilityScores[Ability.Constitution] = 14;  // 12 with elf penalty

        Assert.Equal(1, character.BaseAttackBonus);
        Assert.Equal(4, character.MeleeAttackBonus);   // 1 BAB + 3 Str
        Assert.Equal(4, character.RangedAttackBonus);  // 1 BAB + 3 Dex
        Assert.Equal(3, character.FortitudeSave);      // 2 base + 1 Con
        Assert.Equal(3, character.ReflexSave);         // 0 base + 3 Dex
        Assert.Equal(0, character.WillSave);           // 0 base + 0 Wis
    }

    // Tests that a resistance bonus applies to all three saves.
    [Fact]
    public void ResistanceBonus_AppliesToAllThreeSaves()
    {
        var character = FighterOfLevel(1);
        foreach (var save in new[] { "Fortitude", "Reflex", "Will" })
            character.Modifiers.Add(
                new Modifier(save, 1, BonusType.Resistance, "Cloak of Resistance +1"));

        Assert.Equal(3, character.FortitudeSave);
        Assert.Equal(1, character.ReflexSave);
        Assert.Equal(1, character.WillSave);

    }

    // Tests that the fighter allows any alignment.
    [Fact]
    public void Fighter_AllowsAnyAlignment()
    {
        var fighter = LoadFighter();

        foreach (var alignment in Enum.GetValues<Alignment>())
            Assert.True(fighter.AllowsAlignment(alignment));
    }

    
    // Tests that the fighter's starting wealth is 5d6 times 10.
    [Fact]
    public void Fighter_StartingWealth_Is5d6Times10()
    {
        var wealth = LoadFighter().StartingWealth;

        Assert.Equal(5, wealth.Dice);
        Assert.Equal(6, wealth.Sides);
        Assert.Equal(10, wealth.Multiplier);
        Assert.Equal(175m, wealth.Average);
        Assert.Equal(50, wealth.Minimum);
        Assert.Equal(300, wealth.Maximum);
    }

    // Tests that rolling the fighter's starting wealth stays within the expected range and is a multiple of 10.
    [Fact]
    public void StartingWealth_RollStaysInRangeAndIsMultipleOfTen()
    {
        var wealth = LoadFighter().StartingWealth;
        var random = new Random(12345); // fixed seed keeps the test repeatable

        for (int i = 0; i < 1000; i++)
        {
            int roll = wealth.Roll(random);

            Assert.InRange(roll, wealth.Minimum, wealth.Maximum);
            Assert.Equal(0, roll % 10);

        }
    }
}