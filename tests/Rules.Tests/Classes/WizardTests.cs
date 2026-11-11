using Rules.Core;
using Rules.Core.Data;

namespace Rules.Tests;

public class WizardTests
{
    private static ClassDefinition LoadWizard() => DataPackLoader.LoadClass(
        Path.Combine(AppContext.BaseDirectory, "data", "core", "classes", "wizard.json"));

    private static ClassDefinition LoadFighter() => DataPackLoader.LoadClass(
        Path.Combine(AppContext.BaseDirectory, "data", "core", "classes", "fighter.json"));

    private static (Character Character, ClassLevel Wizard) WizardOfLevel(int level, int intelligence)
    {
        var wizard = new ClassLevel(LoadWizard(), level);
        var character = new Character();
        character.Classes.Add(wizard);
        character.BaseAbilityScores[Ability.Intelligence] = intelligence;
        return (character, wizard);
    }

    // Tests that the wizard class loads correctly from its JSON definition.
    [Fact]
    public void Wizard_LoadsFromJson()
    {
        var wizard = LoadWizard();

        Assert.Equal("Wizard", wizard.Name);
        Assert.Equal(6, wizard.HitDie);
        Assert.Equal(BabProgression.Half, wizard.BabProgression);
        Assert.Equal(SaveProgression.Good, wizard.Will);
        Assert.Equal(70m, wizard.StartingWealth.Average);
        Assert.Equal(10, wizard.Features.Count);
        Assert.NotNull(wizard.Spellcasting);
        Assert.Equal(Ability.Intelligence, wizard.Spellcasting!.CastingAbility);
        Assert.Equal(MagicType.Arcane, wizard.Spellcasting.MagicType);
        Assert.Equal(20, wizard.Spellcasting.SpellsPerDay.Count);
    }

    // Tests that the fighter class has no spellcasting ability.
    [Fact]
    public void Fighter_HasNoSpellcasting()
    {
        Assert.Null(LoadFighter().Spellcasting);
    }

    // Tests that the wizard's base attack bonus and saving throw progression matches the core table.
    [Theory]
    //        level  bab  fort  ref  will
    [InlineData(1,    0,   0,    0,   2)]
    [InlineData(2,    1,   0,    0,   3)]
    [InlineData(5,    2,   1,    1,   4)]
    [InlineData(20,  10,   6,    6,  12)]
    public void Wizard_ProgressionMatchesCoreTable(
        int level, int bab, int fort, int reflex, int will)
    {
        var (character, _) = WizardOfLevel(level, 10);

        Assert.Equal(bab, character.BaseAttackBonus);
        Assert.Equal(fort, character.BaseFortitude);
        Assert.Equal(reflex, character.BaseReflex);
        Assert.Equal(will, character.BaseWill);
    }

    [Theory]
    //        level  int  spellLevel  expected
    [InlineData(1,    10,  0,          3)]   // cantrips never get bonus spells
    [InlineData(1,    11,  1,          1)]   // Int 11 is the minimum for 1st-level spells
    [InlineData(1,    12,  1,          2)]   // Int 12: +1 first-level
    [InlineData(1,    20,  1,          3)]   // Int 20: +2 first-level
    [InlineData(1,    20,  2,          0)]   // no base slot, so no bonus slot
    [InlineData(5,    13,  3,          1)]
    [InlineData(20,   19,  9,          4)]
    public void Wizard_SpellsPerDay(int level, int intelligence, int spellLevel, int expected)
    {
        var (character, wizard) = WizardOfLevel(level, intelligence);

        Assert.Equal(expected, character.SpellsPerDay(wizard, spellLevel));
    }

    // Tests that the wizard needs a high enough Intelligence score to cast spells of a given level.
    [Fact]
    public void Wizard_NeedsHighEnoughIntelligenceToCastSpellLevel()
    {
        // Int 11 can't cast 2nd-level spells (needs 12), even at level 3
        var (character, wizard) = WizardOfLevel(3, 11);

        Assert.Equal(2, character.SpellsPerDay(wizard, 1));
        Assert.Equal(0, character.SpellsPerDay(wizard, 2));
    }

    // Tests that the wizard's spell save DC is calculated correctly based on spell level and Intelligence modifier.
    [Theory]
    [InlineData(16, 3, 16)]   // 10 + 3 (spell) + 3 (Int 16)
    [InlineData(14, 1, 13)]   // 10 + 1 + 2
    [InlineData(10, 0, 10)]   // a cantrip with no Int bonus
    public void Wizard_SpellSaveDC(int intelligence, int spellLevel, int expectedDC)
    {
        var (character, wizard) = WizardOfLevel(5, intelligence);

        Assert.Equal(expectedDC, character.SpellSaveDC(wizard, spellLevel));
    }

    // Tests that the wizard gains bonus feats at levels 5, 10, 15, and 20.
    [Fact]
    public void Wizard_BonusFeatsAtFiveTenFifteenTwenty()
    {
        var (character, _) = WizardOfLevel(20, 10);

        Assert.Equal(4, character.ClassFeatures.Count(f => f.Name == "Bonus Feat"));
    }

    // Tests that multiclassing as a fighter and wizard correctly stacks levels, base attack bonus, and saving throws.
    [Fact]
    public void Fighter_Wizard_MulticlassProgressionStacks()
    {
        var character = new Character();
        character.Classes.Add(new ClassLevel(LoadFighter(), 2));
        character.Classes.Add(new ClassLevel(LoadWizard(), 3));

        Assert.Equal(5, character.TotalLevel);
        Assert.Equal(3, character.BaseAttackBonus); // 2 (fighter) + 1 (wizard)
        Assert.Equal(4, character.BaseFortitude);   // 3 + 1
        Assert.Equal(1, character.BaseReflex);      // 0 + 1
        Assert.Equal(3, character.BaseWill);        // 0 + 3
    }

    // Tests that a non-caster class, like the fighter, cannot cast spells.
    [Fact]
    public void NonCaster_CannotCastSpells()
    {
        var fighter = new ClassLevel(LoadFighter(), 5);
        var character = new Character();
        character.Classes.Add(fighter);

        Assert.Equal(0, character.SpellsPerDay(fighter, 1));
        Assert.Throws<InvalidOperationException>(() => character.SpellSaveDC(fighter, 1));
    }
}