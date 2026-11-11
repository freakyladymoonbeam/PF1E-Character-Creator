using Rules.Core;
using Rules.Core.Data;

namespace Rules.Tests;

public class FeatTests
{
    private static Character Build(
        ClassDefinition cls, int level, RaceDefinition? race = null,
        params (Ability Ability, int Score)[] scores)
    {
        var character = new Character { Race = race };
        character.Classes.Add(new ClassLevel(cls, level));
        foreach (var (ability, score) in scores)
            character.BaseAbilityScores[ability] = score;
        return character;
    }

    // ---- The data itself ----

    // Verifies that feats are correctly loaded from the JSON data file.
    [Fact]
    public void Feats_LoadFromJson()
    {
        var feats = TestData.Feats();

        Assert.Equal(109, feats.Count);
        Assert.Equal(feats.Count, feats.Select(f => f.Id).Distinct().Count());
        Assert.Equal(feats.Count, feats.Select(f => f.Name).Distinct().Count());

        var powerAttack = TestData.Feat("Power Attack");
        Assert.Contains(FeatType.Combat, powerAttack.Types);
        Assert.Equal(2, powerAttack.Prerequisites!.Count);

        Assert.Equal(FeatSubject.Skill, TestData.Feat("Skill Focus").Subject);
        Assert.Contains(FeatType.Critical, TestData.Feat("Bleeding Critical").Types);
        Assert.Contains(FeatType.ItemCreation, TestData.Feat("Scribe Scroll").Types);
    }

    // Verifies that every feat prerequisite references a real feat.
    [Fact]
    public void EveryFeatPrerequisite_NamesARealFeat()
    {
        // Catches typos in the data file, such as "Power Atack".
        var feats = TestData.Feats();
        var names = feats.Select(f => f.Name).ToHashSet();

        foreach (var feat in feats)
            foreach (var p in feat.Prerequisites ?? [])
                if (p.Type == PrerequisiteType.Feat)
                    Assert.Contains(p.Name!, names);
    }

    // ---- Feat slots ----

    // Verifies that feat slots are correctly calculated: one at level 1, then one at every odd level.
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    [InlineData(5, 3)]
    [InlineData(20, 10)]
    public void FeatSlots_OneAtLevelOneThenEveryOddLevel(int level, int expected)
    {
        Assert.Equal(expected, Build(TestData.Fighter(), level).FeatSlotsAvailable);
    }

    // Verifies that attempting to add a feat when no slots are left is blocked.
    [Fact]
    public void NoSlotsLeft_BlocksAnotherFeat()
    {
        var character = Build(TestData.Wizard(), 1);
        character.AddFeat(TestData.Feat("Alertness"));

        Assert.Equal(0, character.FeatSlotsRemaining);
        Assert.Contains(character.FeatProblems(TestData.Feat("Iron Will")),
            p => p.Contains("feat slot"));
        Assert.Throws<InvalidOperationException>(
            () => character.AddFeat(TestData.Feat("Iron Will")));
    }

    // Verifies that adding the same feat twice is rejected.
    [Fact]
    public void SameFeatTwice_IsRejected()
    {
        var character = Build(TestData.Fighter(), 3);
        character.AddFeat(TestData.Feat("Alertness"));

        Assert.Throws<InvalidOperationException>(
            () => character.AddFeat(TestData.Feat("Alertness")));
    }

    // ---- Flat modifiers ----

    // Verifies that feats providing flat bonuses to saves and initiative are applied correctly.
    [Fact]
    public void GreatFortitude_AddsTwoToFortitudeSave()
    {
        var character = Build(TestData.Wizard(), 1);
        character.AddFeat(TestData.Feat("Great Fortitude"));

        Assert.Equal(2, character.FortitudeSave);   // 0 base + 2
    }

    // Verifies that the Iron Will feat correctly adds to the Will save.
    [Fact]
    public void IronWill_AddsTwoToWillSave()
    {
        var character = Build(TestData.Wizard(), 1);
        character.AddFeat(TestData.Feat("Iron Will"));

        Assert.Equal(4, character.WillSave);        // 2 base + 2
    }

    // Verifies that the Improved Initiative feat correctly adds to the initiative.
    [Fact]
    public void ImprovedInitiative_AddsFourToInitiative()
    {
        var character = Build(TestData.Fighter(), 1, null, (Ability.Dexterity, 14));
        character.AddFeat(TestData.Feat("Improved Initiative"));

        Assert.Equal(6, character.Initiative);      // +2 Dex, +4 feat
    }

    // Verifies that the Dodge feat correctly adds a dodge bonus to Armor Class.
    [Fact]
    public void Dodge_GivesDodgeBonusToAC()
    {
        // Base Dex 11 plus the elf's +2 makes 13, which meets the prerequisite.
        var character = Build(TestData.Wizard(), 1, TestData.Elf(), (Ability.Dexterity, 11));
        character.AddFeat(TestData.Feat("Dodge"));

        Assert.Equal(12, character.ArmorClass);     // 10 + 1 (Dex) + 1 (dodge)
    }

    // Verifies that the Toughness feat correctly scales with hit dice beyond the third.
    [Fact]
    public void Toughness_ScalesWithHitDiceBeyondThree()
    {
        var level1 = Build(TestData.Fighter(), 1);
        level1.AddFeat(TestData.Feat("Toughness"));
        Assert.Equal(13, level1.MaxHitPoints);      // 10 + 3

        var level5 = Build(TestData.Fighter(), 5);
        level5.AddFeat(TestData.Feat("Toughness"));
        Assert.Equal(39, level5.MaxHitPoints);      // 34 + 3 + 2
    }

    // ---- Skill feats ----

    // Verifies that skill feats provide the correct bonus, which increases at ten ranks.
    [Fact]
    public void SkillFeat_BonusRisesAtTenRanks()
    {
        var character = Build(TestData.Wizard(), 10);
        character.AddFeat(TestData.Feat("Alertness"));
        var perception = TestData.Skill("Perception");

        character.SetSkillRanks(perception, 9);
        Assert.Equal(11, character.SkillBonus(perception));   // 9 ranks + 2

        character.SetSkillRanks(perception, 10);
        Assert.Equal(14, character.SkillBonus(perception));   // 10 ranks + 4
    }

    // Verifies that the Skill Focus feat applies its bonus to the chosen skill.
    [Fact]
    public void SkillFocus_AppliesToTheChosenSkill()
    {
        var character = Build(TestData.Wizard(), 1, null, (Ability.Intelligence, 15));
        character.AddFeat(TestData.Feat("Skill Focus"), "Spellcraft");
        var spellcraft = TestData.Skill("Spellcraft");
        character.SetSkillRanks(spellcraft, 1);

        Assert.Equal(9, character.SkillBonus(spellcraft));    // 1 rank + 3 class + 2 Int + 3 focus
        Assert.Equal(0, character.Total("Appraise"));
    }

    // Verifies that the Skill Focus feat's bonus increases when the character has ten ranks in the skill.
    [Fact]
    public void SkillFocus_UpgradesAtTenRanks()
    {
        var character = Build(TestData.Wizard(), 10, null, (Ability.Intelligence, 15));
        character.AddFeat(TestData.Feat("Skill Focus"), "Spellcraft");
        var spellcraft = TestData.Skill("Spellcraft");
        character.SetSkillRanks(spellcraft, 10);

        Assert.Equal(21, character.SkillBonus(spellcraft));   // 10 + 3 + 2 + 6
    }

    // Verifies that the Skill Focus feat requires a choice of skill and that other feats reject an inappropriate choice.
    [Fact]
    public void SkillFocus_NeedsAChoice_AndOtherFeatsRejectOne()
    {
        var character = Build(TestData.Wizard(), 3);

        Assert.Throws<InvalidOperationException>(
            () => character.AddFeat(TestData.Feat("Skill Focus")));
        Assert.Throws<InvalidOperationException>(
            () => character.AddFeat(TestData.Feat("Alertness"), "Perception"));
    }

    // Verifies that the Skill Focus feat can be taken for different skills but not the same one twice.
    [Fact]
    public void SkillFocus_CanBeTakenForDifferentSkillsButNotTheSameOneTwice()
    {
        var character = Build(TestData.Wizard(), 3);
        var skillFocus = TestData.Feat("Skill Focus");

        character.AddFeat(skillFocus, "Spellcraft");

        Assert.Throws<InvalidOperationException>(
            () => character.AddFeat(skillFocus, "Spellcraft"));

        character.AddFeat(skillFocus, "Appraise");
        Assert.Equal(2, character.Feats.Count);
    }

    // Verifies that racial bonuses and feat bonuses stack when they are of different types.
    [Fact]
    public void RacialAndFeatBonuses_StackBecauseTheyAreDifferentTypes()
    {
        var character = Build(TestData.Wizard(), 1, TestData.Elf());
        character.AddFeat(TestData.Feat("Alertness"));

        Assert.Equal(4, character.Total("Perception"));        // +2 racial, +2 untyped
    }

    // ---- Prerequisites ----

    // Verifies that feats with prerequisites must be taken in the correct order.
    [Fact]
    public void FeatChain_MustBeTakenInOrder()
    {
        var character = Build(TestData.Fighter(), 5, null, (Ability.Strength, 13));

        Assert.Throws<InvalidOperationException>(
            () => character.AddFeat(TestData.Feat("Cleave")));

        character.AddFeat(TestData.Feat("Power Attack"));
        character.AddFeat(TestData.Feat("Cleave"));
        character.AddFeat(TestData.Feat("Great Cleave"));

        Assert.Equal(3, character.Feats.Count);
    }

    // Verifies that the Ability Score prerequisite is correctly reported when not met.
    [Fact]
    public void AbilityScorePrerequisite_ReportsWhatIsMissing()
    {
        var character = Build(TestData.Wizard(), 1, null, (Ability.Dexterity, 12));

        Assert.Contains(character.FeatProblems(TestData.Feat("Dodge")),
            p => p.Contains("Dex 13"));
    }

    // Verifies that the Base Attack Bonus prerequisite is correctly checked.
    [Fact]
    public void BaseAttackBonusPrerequisite_IsChecked()
    {
        // A level 3 fighter has BAB +3, one short of Great Cleave's +4.
        var character = Build(TestData.Fighter(), 3, null, (Ability.Strength, 13));
        character.AddFeat(TestData.Feat("Power Attack"));
        character.AddFeat(TestData.Feat("Cleave"));

        Assert.Contains(character.FeatProblems(TestData.Feat("Great Cleave")),
            p => p.Contains("base attack bonus +4"));
    }

    // Verifies that the Caster Level prerequisite requires a spellcasting class.
    [Fact]
    public void CasterLevelPrerequisite_NeedsASpellcastingClass()
    {
        var scribeScroll = TestData.Feat("Scribe Scroll");

        Assert.Empty(Build(TestData.Wizard(), 1).FeatProblems(scribeScroll));
        Assert.Contains(Build(TestData.Fighter(), 1).FeatProblems(scribeScroll),
            p => p.Contains("caster level 1"));
    }

    // Verifies that the Brew Potion feat requires a caster level of 3.
    [Theory]
    [InlineData(2, false)]
    [InlineData(3, true)]
    public void BrewPotion_NeedsCasterLevelThree(int level, bool allowed)
    {
        var problems = Build(TestData.Wizard(), level).FeatProblems(TestData.Feat("Brew Potion"));

        Assert.Equal(allowed, problems.Count == 0);
    }

    // Verifies that the Disruptive feat requires a sixth-level fighter.
    [Theory]
    [InlineData(5, false)]
    [InlineData(6, true)]
    public void Disruptive_NeedsSixthLevelFighter(int level, bool allowed)
    {
        var problems = Build(TestData.Fighter(), level).FeatProblems(TestData.Feat("Disruptive"));

        Assert.Equal(allowed, problems.Count == 0);
    }

    // Verifies that the Skill Ranks prerequisite is correctly checked.
    [Fact]
    public void SkillRanksPrerequisite_IsChecked()
    {
        var character = Build(TestData.Fighter(), 1);
        var mountedCombat = TestData.Feat("Mounted Combat");

        Assert.Contains(character.FeatProblems(mountedCombat), p => p.Contains("Ride"));

        character.SetSkillRanks(TestData.Skill("Ride"), 1);
        Assert.Empty(character.FeatProblems(mountedCombat));
    }

    // Verifies that the Leadership feat requires a character level of 7.
    [Theory]
    [InlineData(6, false)]
    [InlineData(7, true)]
    public void Leadership_NeedsCharacterLevelSeven(int level, bool allowed)
    {
        var problems = Build(TestData.Fighter(), level).FeatProblems(TestData.Feat("Leadership"));

        Assert.Equal(allowed, problems.Count == 0);
    }

    // Verifies that the Class Feature prerequisite is correctly checked.
    [Fact]
    public void ClassFeaturePrerequisite_IsChecked()
    {
        var character = Build(TestData.Wizard(), 1);

        Assert.Contains(character.FeatProblems(TestData.Feat("Improved Channel")),
            p => p.Contains("Channel Energy class feature"));
    }

    // Verifies that the Spell Mastery feat requires a wizard level.
    [Fact]
    public void SpellMastery_NeedsAWizardLevel()
    {
        var spellMastery = TestData.Feat("Spell Mastery");

        Assert.Empty(Build(TestData.Wizard(), 1).FeatProblems(spellMastery));
        Assert.NotEmpty(Build(TestData.Fighter(), 1).FeatProblems(spellMastery));
    }
}