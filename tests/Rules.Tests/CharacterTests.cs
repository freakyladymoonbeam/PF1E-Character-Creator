using Rules.Core;

namespace Rules.Tests;

public class CharacterTests
{
    // Tests for the AbilityMath.ModifierFor method, ensuring it rounds down correctly.
    [Theory]
    [InlineData(10, 0)]
    [InlineData(11, 0)]
    [InlineData(12, 1)]
    [InlineData(14, 2)]
    [InlineData(18, 4)]
    [InlineData(9, -1)]
    [InlineData(8, -1)]
    [InlineData(7, -2)]
    public void AbilityModifier_RoundsDown(int score, int expected)
    {
        Assert.Equal(expected, AbilityMath.ModifierFor(score));
    }

    // Tests that an enhancement bonus correctly raises the character's ability score and modifier.
    [Fact]
    public void EnhancementBonus_RaisesAbilityScoreAndModifier()
    {
        var character = new Character();
        character.BaseAbilityScores[Ability.Strength] = 14;
        character.Modifiers.Add(
            new Modifier("Strength", 2, BonusType.Enhancement, "Belt of Giant Strength +2"));

        Assert.Equal(16, character.AbilityScore(Ability.Strength));
        Assert.Equal(3, character.AbilityModifier(Ability.Strength));
    }

    // Tests that the character's Armor Class correctly combines base 10, Dexterity modifier, and AC modifiers from armor and shield.
    [Fact]
    public void ArmorClass_CombinesBaseDexArmorAndShield()
    {
        var character = new Character();
        character.BaseAbilityScores[Ability.Dexterity] = 14;
        character.Modifiers.Add(new Modifier("AC", 4, BonusType.Armor, "Chainmail"));
        character.Modifiers.Add(new Modifier("AC", 2, BonusType.Shield, "Heavy steel shield"));

        Assert.Equal(18, character.ArmorClass); // 10 + 2 (Dex) + 4 + 2
    }
}