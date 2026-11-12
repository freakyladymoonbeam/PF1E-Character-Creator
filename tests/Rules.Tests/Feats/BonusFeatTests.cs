using Rules.Core;
using Rules.Core.Data;

namespace Rules.Tests;

public class BonusFeatTests
{
    private static (Character Character, ClassLevel Class) Build(
        ClassDefinition cls, int level, params (Ability Ability, int Score)[] scores)
    {
        var classLevel = new ClassLevel(cls, level);
        var character = new Character();
        character.Classes.Add(classLevel);
        foreach (var (ability, score) in scores)
            character.BaseAbilityScores[ability] = score;
        return (character, classLevel);
    }

    // ---- Slot counts and class data ----

    // Verifies that the number of bonus feat slots follows the class table for fighters and wizards.
    [Theory]
    [InlineData("Fighter", 1, 1)]
    [InlineData("Fighter", 2, 2)]
    [InlineData("Fighter", 3, 2)]
    [InlineData("Fighter", 4, 3)]
    [InlineData("Fighter", 5, 3)]
    [InlineData("Fighter", 6, 4)]
    [InlineData("Fighter", 20, 11)]
    [InlineData("Wizard", 1, 0)]
    [InlineData("Wizard", 4, 0)]
    [InlineData("Wizard", 5, 1)]
    [InlineData("Wizard", 9, 1)]
    [InlineData("Wizard", 10, 2)]
    [InlineData("Wizard", 15, 3)]
    [InlineData("Wizard", 20, 4)]
    public void BonusFeatSlots_FollowTheClassTable(string className, int level, int expected)
    {
        var cls = className == "Fighter" ? TestData.Fighter() : TestData.Wizard();
        var (character, classLevel) = Build(cls, level);

        Assert.Equal(expected, character.BonusFeatSlotsAvailable(classLevel));
    }

    // Verifies that the class data correctly describes which feats can be taken as bonus feats.
    [Fact]
    public void ClassData_DescribesWhichFeatsBonusFeatsCanBe()
    {
        var fighterRule = TestData.Fighter().BonusFeats!;
        Assert.Equal(new[] { FeatType.Combat }, fighterRule.Types!);
        Assert.Null(fighterRule.Names);

        var wizardRule = TestData.Wizard().BonusFeats!;
        Assert.Equal(new[] { FeatType.Metamagic, FeatType.ItemCreation }, wizardRule.Types!);
        Assert.Equal(new[] { "Spell Mastery" }, wizardRule.Names!);
    }

    // Verifies that all class-granted and bonus feat names correspond to real feats.
    [Fact]
    public void ClassGrantedAndBonusFeatNames_AreRealFeats()
    {
        var names = TestData.Feats().Select(f => f.Name).ToHashSet();

        foreach (var cls in new[] { TestData.Fighter(), TestData.Wizard() })
        {
            foreach (var granted in cls.GrantedFeats ?? [])
                Assert.Contains(granted, names);
            foreach (var named in cls.BonusFeats?.Names ?? [])
                Assert.Contains(named, names);
        }
    }

    // ---- Fighter bonus feats ----

    // Verifies that a fighter's bonus feat must be a combat feat.
    [Fact]
    public void FighterBonusFeat_MustBeACombatFeat()
    {
        var (fighter, level) = Build(TestData.Fighter(), 1);

        Assert.Contains(fighter.BonusFeatProblems(level, TestData.Feat("Alertness")),
            p => p.Contains("not one of the feats"));
        Assert.Empty(fighter.BonusFeatProblems(level, TestData.Feat("Improved Initiative")));
    }

    // Verifies that bonus feats do not consume ordinary feat slots.
    [Fact]
    public void BonusFeats_DoNotUseUpOrdinaryFeatSlots()
    {
        var (fighter, level) = Build(TestData.Fighter(), 1);

        fighter.AddBonusFeat(level, TestData.Feat("Improved Initiative"));

        Assert.Single(fighter.BonusFeats);
        Assert.Equal("Fighter", fighter.BonusFeats[0].Source);
        Assert.Empty(fighter.Feats);
        Assert.Equal(0, fighter.BonusFeatSlotsRemaining(level));
        Assert.Equal(1, fighter.FeatSlotsRemaining);   // the ordinary slot is untouched

        fighter.AddFeat(TestData.Feat("Alertness"));

        Assert.Equal(0, fighter.FeatSlotsRemaining);
    }

    // Verifies that bonus feats run out after the allotted number of slots.
    [Fact]
    public void BonusFeats_RunOut()
    {
        var (fighter, level) = Build(TestData.Fighter(), 2);   // two bonus feats
        fighter.AddBonusFeat(level, TestData.Feat("Improved Initiative"));
        fighter.AddBonusFeat(level, TestData.Feat("Combat Reflexes"));

        Assert.Equal(0, fighter.BonusFeatSlotsRemaining(level));
        Assert.Contains(fighter.BonusFeatProblems(level, TestData.Feat("Weapon Finesse")),
            p => p.Contains("bonus feats remaining"));
        Assert.Throws<InvalidOperationException>(
            () => fighter.AddBonusFeat(level, TestData.Feat("Weapon Finesse")));
    }

    // Verifies that bonus feats still need to meet their prerequisites.
    [Fact]
    public void BonusFeats_StillNeedTheirPrerequisites()
    {
        var powerAttack = TestData.Feat("Power Attack");

        var (weak, weakLevel) = Build(TestData.Fighter(), 1, (Ability.Strength, 12));
        Assert.Contains(weak.BonusFeatProblems(weakLevel, powerAttack), p => p.Contains("Str 13"));

        var (strong, strongLevel) = Build(TestData.Fighter(), 1, (Ability.Strength, 13));
        Assert.Empty(strong.BonusFeatProblems(strongLevel, powerAttack));
    }

    // Verifies that bonus feats count as prerequisites for later feats.
    [Fact]
    public void BonusFeats_CountAsPrerequisitesForLaterFeats()
    {
        var (fighter, level) = Build(TestData.Fighter(), 1, (Ability.Strength, 13));
        fighter.AddBonusFeat(level, TestData.Feat("Power Attack"));

        Assert.Empty(fighter.FeatProblems(TestData.Feat("Cleave")));
    }

    // Verifies that bonus feats apply their modifiers to the character.
    [Fact]
    public void BonusFeats_ApplyTheirModifiers()
    {
        var (fighter, level) = Build(TestData.Fighter(), 1);
        fighter.AddBonusFeat(level, TestData.Feat("Improved Initiative"));

        Assert.Equal(4, fighter.Initiative);
    }

    // Verifies that a bonus feat cannot duplicate a feat the character already has.
    [Fact]
    public void ABonusFeat_CannotDuplicateAFeatAlreadyTaken()
    {
        var (fighter, level) = Build(TestData.Fighter(), 1);
        fighter.AddFeat(TestData.Feat("Improved Initiative"));

        Assert.Contains(fighter.BonusFeatProblems(level, TestData.Feat("Improved Initiative")),
            p => p.Contains("Already has"));
    }

    // Verifies that bonus feats still ask for their choice when required.
    [Fact]
    public void BonusFeats_StillAskForTheirChoice()
    {
        var (fighter, level) = Build(TestData.Fighter(), 1);
        var weaponFocus = TestData.Feat("Weapon Focus");

        Assert.Contains(fighter.BonusFeatProblems(level, weaponFocus),
            p => p.Contains("needs a choice"));
        Assert.Empty(fighter.BonusFeatProblems(level, weaponFocus, "Longsword"));
    }

    // Verifies that a bonus feat must come from a class the character has.
    [Fact]
    public void ABonusFeat_MustComeFromAClassTheCharacterHas()
    {
        var (wizard, _) = Build(TestData.Wizard(), 5);
        var borrowedFighterLevels = new ClassLevel(TestData.Fighter(), 20);

        Assert.Contains(
            wizard.BonusFeatProblems(borrowedFighterLevels, TestData.Feat("Improved Initiative")),
            p => p.Contains("doesn't belong"));
    }

    // ---- Wizard bonus feats ----

    // Verifies that a wizard is granted the Scribe Scroll and Eschew Materials feats and cannot take them again.
    [Fact]
    public void WizardIsGrantedScribeScrollAndEschewMaterials_AndCannotTakeThemAgain()
    {
        var (wizard, level) = Build(TestData.Wizard(), 5);

        Assert.Contains("Scribe Scroll", wizard.GrantedFeatNames);
        Assert.Contains("Eschew Materials", wizard.GrantedFeatNames);

        foreach (var name in new[] { "Scribe Scroll", "Eschew Materials" })
        {
            Assert.Contains(wizard.FeatProblems(TestData.Feat(name)),
                p => p.Contains("Already has"));
            Assert.Contains(wizard.BonusFeatProblems(level, TestData.Feat(name)),
                p => p.Contains("Already has"));
        }
    }

    // Verifies that a wizard has no bonus feat until level five.
    [Fact]
    public void Wizard_HasNoBonusFeatUntilLevelFive()
    {
        var (wizard, level) = Build(TestData.Wizard(), 4);

        Assert.Contains(wizard.BonusFeatProblems(level, TestData.Feat("Empower Spell")),
            p => p.Contains("bonus feats remaining"));
    }

    // Verifies that a wizard may take metamagic, item creation, and Spell Mastery as bonus feats.
    [Theory]
    [InlineData("Empower Spell")]    // metamagic
    [InlineData("Brew Potion")]      // item creation, caster level 3
    [InlineData("Craft Wand")]       // item creation, caster level 5
    [InlineData("Spell Mastery")]    // named explicitly
    public void WizardBonusFeat_AcceptsMetamagicItemCreationAndSpellMastery(string name)
    {
        var (wizard, level) = Build(TestData.Wizard(), 5);

        Assert.Empty(wizard.BonusFeatProblems(level, TestData.Feat(name)));
    }

    // Verifies that a wizard cannot take other feats as bonus feats.
    [Theory]
    [InlineData("Alertness")]
    [InlineData("Toughness")]
    [InlineData("Combat Casting")]
    public void WizardBonusFeat_RejectsOtherFeats(string name)
    {
        var (wizard, level) = Build(TestData.Wizard(), 5);

        Assert.Contains(wizard.BonusFeatProblems(level, TestData.Feat(name)),
            p => p.Contains("not one of the feats"));
    }

    // Verifies that wizard bonus item creation feats still need the appropriate caster level.
    [Theory]
    [InlineData("Forge Ring", 7)]
    [InlineData("Craft Rod", 9)]
    [InlineData("Craft Staff", 11)]
    public void WizardBonusItemCreationFeats_StillNeedTheCasterLevel(string name, int casterLevel)
    {
        var (wizard, level) = Build(TestData.Wizard(), 5);

        Assert.Contains(wizard.BonusFeatProblems(level, TestData.Feat(name)),
            p => p.Contains($"caster level {casterLevel}"));
    }

    // Verifies that a level ten wizard has two bonus feats and no remaining slots.
    [Fact]
    public void LevelTenWizard_HasTwoBonusFeats()
    {
        var (wizard, level) = Build(TestData.Wizard(), 10);
        wizard.AddBonusFeat(level, TestData.Feat("Empower Spell"));
        wizard.AddBonusFeat(level, TestData.Feat("Craft Rod"));

        Assert.Equal(2, wizard.BonusFeats.Count);
        Assert.Equal(0, wizard.BonusFeatSlotsRemaining(level));
    }

    // ---- Multiclass and granted feats ----

    // Verifies that bonus feat pools are separate for each class.
    [Fact]
    public void BonusFeatPools_AreSeparatePerClass()
    {
        var fighter = new ClassLevel(TestData.Fighter(), 2);   // 2 bonus feats
        var wizard = new ClassLevel(TestData.Wizard(), 5);     // 1 bonus feat
        var character = new Character();
        character.Classes.Add(fighter);
        character.Classes.Add(wizard);

        character.AddBonusFeat(wizard, TestData.Feat("Empower Spell"));

        Assert.Equal(0, character.BonusFeatSlotsRemaining(wizard));
        Assert.Equal(2, character.BonusFeatSlotsRemaining(fighter));
        Assert.Contains(character.BonusFeatProblems(fighter, TestData.Feat("Maximize Spell")),
            p => p.Contains("not one of the feats a Fighter"));
    }

    // Verifies that a fighter cannot spend a feat on a proficiency it already has, but a wizard can.
    [Fact]
    public void FighterCannotSpendAFeatOnAProficiencyItAlreadyHas_ButAWizardCan()
    {
        var (fighter, _) = Build(TestData.Fighter(), 3);
        var (wizard, _) = Build(TestData.Wizard(), 3);
        var shieldProficiency = TestData.Feat("Shield Proficiency");

        Assert.Contains(fighter.FeatProblems(shieldProficiency), p => p.Contains("Already has"));
        Assert.DoesNotContain(wizard.FeatProblems(shieldProficiency),
            p => p.Contains("Already has"));
    }

    // ---- The level 1 Elf Wizard milestone ----

    // Verifies that a level 1 Elf wizard has the feats described in the book.
    [Fact]
    public void LevelOneElfWizard_HasTheFeatSetupTheBookDescribes()
    {
        var character = new Character { Race = TestData.Elf() };
        var wizard = new ClassLevel(TestData.Wizard(), 1);
        character.Classes.Add(wizard);

        Assert.Equal(1, character.FeatSlotsAvailable);               // one ordinary feat
        Assert.Equal(0, character.BonusFeatSlotsAvailable(wizard));  // no chosen bonus feat yet
        Assert.Contains("Scribe Scroll", character.GrantedFeatNames);
        Assert.Contains("Eschew Materials", character.GrantedFeatNames);
    }
}