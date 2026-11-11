using Rules.Core;
using Rules.Core.Data;

namespace Rules.Tests;

public class RaceTests
{
    // Loads the Elf race definition from the JSON data file.
    private static RaceDefinition LoadElf() => DataPackLoader.LoadRace(
        Path.Combine(AppContext.BaseDirectory, "data", "core", "races", "elf.json"));

    // Verifies that the Elf race definition is correctly loaded from the JSON file.
    [Fact]
    public void Elf_LoadsFromJson()
    {
        var elf = LoadElf();

        Assert.Equal("Elf", elf.Name);
        Assert.Equal(Size.Medium, elf.Size);
        Assert.Equal(30, elf.BaseSpeed);
        Assert.Equal(5, elf.Traits.Count);
        Assert.Contains("Elven", elf.Languages);
    }

    // Verifies that the Elf racial ability adjustments are correctly applied to a character.
    [Fact]
    public void Elf_AppliesRacialAbilityAdjustments()
    {
        var character = new Character { Race = LoadElf() };

        Assert.Equal(12, character.AbilityScore(Ability.Dexterity));
        Assert.Equal(12, character.AbilityScore(Ability.Intelligence));
        Assert.Equal(8, character.AbilityScore(Ability.Constitution));
        Assert.Equal(-1, character.AbilityModifier(Ability.Constitution));
        Assert.Equal(10, character.AbilityScore(Ability.Strength));
    }

    // Verifies that the Elf's Keen Senses trait provides a bonus to Perception checks.
    [Fact]
    public void Elf_KeenSensesGivesPerceptionBonus()
    {
        var character = new Character { Race = LoadElf() };

        Assert.Equal(2, character.Total("Perception"));
    }

    // Verifies that racial bonuses stack correctly with enhancement bonuses.
    [Fact]
    public void RacialBonus_StacksWithEnhancementBonus()
    {
        var character = new Character { Race = LoadElf() };
        character.BaseAbilityScores[Ability.Dexterity] = 14;
        character.Modifiers.Add(
            new Modifier("Dexterity", 2, BonusType.Enhancement, "Gloves of Dexterity +2"));

        Assert.Equal(18, character.AbilityScore(Ability.Dexterity)); // 14 + 2 racial + 2 enhancement
    }
}