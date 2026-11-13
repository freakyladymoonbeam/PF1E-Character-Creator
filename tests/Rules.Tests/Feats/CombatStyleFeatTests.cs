using Rules.Core;
using Rules.Core.Data;

namespace Rules.Tests;

public class CombatStyleFeatTests
{
    private static Character Build(
        ClassDefinition cls, int level, params (Ability Ability, int Score)[] scores)
    {
        var character = new Character();
        character.Classes.Add(new ClassLevel(cls, level));
        foreach (var (ability, score) in scores)
            character.BaseAbilityScores[ability] = score;
        return character;
    }

    private static void Take(Character character, params string[] names)
    {
        foreach (var name in names)
            character.AddFeat(TestData.Feat(name));
    }

    // ---- Data guards ----

    // Verifies that every feat in this batch is classified as a combat feat.
    [Fact]
    public void EveryFeatInThisBatch_IsACombatFeat()
    {
        string[] names =
        [
            "Improved Unarmed Strike", "Deflect Arrows", "Snatch Arrows", "Improved Grapple",
            "Greater Grapple", "Scorpion Style", "Gorgon's Fist", "Medusa's Wrath", "Stunning Fist",
            "Two-Weapon Fighting", "Double Slice", "Improved Two-Weapon Fighting",
            "Greater Two-Weapon Fighting", "Two-Weapon Rend", "Two-Weapon Defense",
            "Shield Proficiency", "Improved Shield Bash", "Shield Slam", "Shield Master",
            "Shield Focus", "Greater Shield Focus", "Tower Shield Proficiency",
            "Armor Proficiency, Light", "Armor Proficiency, Medium", "Armor Proficiency, Heavy",
            "Arcane Armor Training", "Arcane Armor Mastery",
            "Vital Strike", "Improved Vital Strike", "Greater Vital Strike", "Lunge", "Step Up",
            "Strike Back", "Blind-Fight", "Catch Off-Guard", "Throw Anything",
            "Improvised Weapon Mastery", "Intimidating Prowess"
        ];

        Assert.Equal(38, names.Length);
        foreach (var name in names)
            Assert.Contains(FeatType.Combat, TestData.Feat(name).Types);
    }

    // Verifies that feats with no prerequisites are available at level one.
    [Theory]
    [InlineData("Improved Unarmed Strike")]
    [InlineData("Shield Proficiency")]
    [InlineData("Armor Proficiency, Light")]
    [InlineData("Blind-Fight")]
    [InlineData("Catch Off-Guard")]
    [InlineData("Throw Anything")]
    [InlineData("Intimidating Prowess")]
    public void NoPrerequisiteFeats_AreAvailableAtLevelOne(string name)
    {
        Assert.Empty(Build(TestData.Wizard(), 1).FeatProblems(TestData.Feat(name)));
    }

    // ---- Feats a class grants automatically ----

    // Verifies that the Fighter grants armor and shield proficiencies and the Wizard grants its two level 1 feats.
    [Fact]
    public void Fighter_GrantsArmorAndShieldProficiencies_Wizard_GrantsItsTwoFeats()
    {
        Assert.Equal(5, TestData.Fighter().GrantedFeats!.Count);
        Assert.Equal(new[] { "Scribe Scroll" }, TestData.Wizard().GrantedFeats);
    }

    // Verifies that all feats granted by a class are real feats.
    [Fact]
    public void GrantedFeats_AreRealFeats()
    {
        var names = TestData.Feats().Select(f => f.Name).ToHashSet();

        foreach (var granted in TestData.Fighter().GrantedFeats ?? [])
            Assert.Contains(granted, names);
    }

    // Verifies that a Fighter can take the Shield Focus feat without first taking Shield Proficiency.
    [Fact]
    public void Fighter_CanTakeShieldFocus_WithoutTakingShieldProficiency()
    {
        var fighter = Build(TestData.Fighter(), 1);

        Assert.Empty(fighter.FeatProblems(TestData.Feat("Shield Focus")));
    }

    // Verifies that a Wizard needs Shield Proficiency before taking Shield Focus.
    [Fact]
    public void Wizard_NeedsShieldProficiencyFirst()
    {
        var wizard = Build(TestData.Wizard(), 3);
        var shieldFocus = TestData.Feat("Shield Focus");

        Assert.Contains(wizard.FeatProblems(shieldFocus), p => p.Contains("Shield Proficiency"));

        Take(wizard, "Shield Proficiency");

        Assert.Empty(wizard.FeatProblems(shieldFocus));
    }

    // ---- Unarmed and grappling ----

    // Verifies that the Deflect Arrows feat requires Improved Unarmed Strike and Dexterity 13.
    [Fact]
    public void DeflectArrows_NeedsImprovedUnarmedStrikeAndDex13()
    {
        var wizard = Build(TestData.Wizard(), 3, (Ability.Dexterity, 13));
        var deflect = TestData.Feat("Deflect Arrows");

        Assert.Contains(wizard.FeatProblems(deflect), p => p.Contains("Improved Unarmed Strike"));

        Take(wizard, "Improved Unarmed Strike");

        Assert.Empty(wizard.FeatProblems(deflect));
    }

    // Verifies that the Snatch Arrows feat requires Dexterity 15, even if the character already has Deflect Arrows.
    [Fact]
    public void SnatchArrows_NeedsDex15_EvenWithDeflectArrows()
    {
        var snatch = TestData.Feat("Snatch Arrows");

        var lowDex = Build(TestData.Fighter(), 5, (Ability.Dexterity, 13));
        Take(lowDex, "Improved Unarmed Strike", "Deflect Arrows");
        var problems = lowDex.FeatProblems(snatch);
        Assert.Single(problems);
        Assert.Contains("Dex 15", problems[0]);

        var ready = Build(TestData.Fighter(), 5, (Ability.Dexterity, 15));
        Take(ready, "Improved Unarmed Strike", "Deflect Arrows");
        Assert.Empty(ready.FeatProblems(snatch));
    }

    // Verifies that the Greater Grapple feat requires a base attack bonus of +6.
    [Fact]
    public void GreaterGrapple_NeedsBaseAttackBonusSix()
    {
        var greater = TestData.Feat("Greater Grapple");

        var level5 = Build(TestData.Fighter(), 5, (Ability.Dexterity, 13));
        Take(level5, "Improved Unarmed Strike", "Improved Grapple");
        var problems = level5.FeatProblems(greater);
        Assert.Single(problems);
        Assert.Contains("base attack bonus +6", problems[0]);

        var level6 = Build(TestData.Fighter(), 6, (Ability.Dexterity, 13));
        Take(level6, "Improved Unarmed Strike", "Improved Grapple");
        Assert.Empty(level6.FeatProblems(greater));
    }

    // Verifies that the Medusa's Wrath feat requires the entire Scorpion Style chain and a base attack bonus of +11.
    [Fact]
    public void MedusasWrath_NeedsTheWholeScorpionChain()
    {
        var medusa = TestData.Feat("Medusa's Wrath");

        var level10 = Build(TestData.Fighter(), 10);
        Take(level10, "Improved Unarmed Strike", "Scorpion Style", "Gorgon's Fist");
        var problems = level10.FeatProblems(medusa);
        Assert.Single(problems);
        Assert.Contains("base attack bonus +11", problems[0]);

        var level11 = Build(TestData.Fighter(), 11);
        Take(level11, "Improved Unarmed Strike", "Scorpion Style", "Gorgon's Fist", "Medusa's Wrath");
        Assert.Equal(4, level11.Feats.Count);
    }

    // Verifies that the Stunning Fist feat requires Dexterity 13, Wisdom 13, a base attack bonus of +8, and Improved Unarmed Strike.
    [Fact]
    public void StunningFist_NeedsDexWisBaseAttackBonusAndImprovedUnarmedStrike()
    {
        var stunning = TestData.Feat("Stunning Fist");

        var level7 = Build(TestData.Fighter(), 7,
            (Ability.Dexterity, 13), (Ability.Wisdom, 13));
        Take(level7, "Improved Unarmed Strike");
        var problems = level7.FeatProblems(stunning);
        Assert.Single(problems);
        Assert.Contains("base attack bonus +8", problems[0]);

        var level8 = Build(TestData.Fighter(), 8,
            (Ability.Dexterity, 13), (Ability.Wisdom, 13));
        Take(level8, "Improved Unarmed Strike");
        Assert.Empty(level8.FeatProblems(stunning));

        var lowWis = Build(TestData.Fighter(), 8,
            (Ability.Dexterity, 13), (Ability.Wisdom, 12));
        Take(lowWis, "Improved Unarmed Strike");
        Assert.Contains(lowWis.FeatProblems(stunning), p => p.Contains("Wis 13"));
    }

    // ---- Two-weapon fighting ----

    // Verifies that the Two-Weapon Fighting feat requires Dexterity 15.
    [Fact]
    public void TwoWeaponFighting_NeedsDex15()
    {
        var twf = TestData.Feat("Two-Weapon Fighting");

        Assert.Contains(Build(TestData.Fighter(), 1, (Ability.Dexterity, 14)).FeatProblems(twf),
            p => p.Contains("Dex 15"));
        Assert.Empty(Build(TestData.Fighter(), 1, (Ability.Dexterity, 15)).FeatProblems(twf));
    }

    // Verifies that the Double Slice and Two-Weapon Defense feats require the Two-Weapon Fighting feat.
    [Theory]
    [InlineData("Double Slice")]
    [InlineData("Two-Weapon Defense")]
    public void DoubleSliceAndTwoWeaponDefense_NeedTwoWeaponFighting(string name)
    {
        var fighter = Build(TestData.Fighter(), 3, (Ability.Dexterity, 15));
        var feat = TestData.Feat(name);

        Assert.Contains(fighter.FeatProblems(feat), p => p.Contains("Two-Weapon Fighting"));

        Take(fighter, "Two-Weapon Fighting");

        Assert.Empty(fighter.FeatProblems(feat));
    }
    
    // Verifies that the Improved Two-Weapon Fighting feat requires Dexterity 17 and a base attack bonus of +6.
    [Fact]
    public void ImprovedTwoWeaponFighting_NeedsDex17AndBaseAttackBonusSix()
    {
        var improved = TestData.Feat("Improved Two-Weapon Fighting");

        var level5 = Build(TestData.Fighter(), 5, (Ability.Dexterity, 17));
        Take(level5, "Two-Weapon Fighting");
        var problems = level5.FeatProblems(improved);
        Assert.Single(problems);
        Assert.Contains("base attack bonus +6", problems[0]);

        var level6 = Build(TestData.Fighter(), 6, (Ability.Dexterity, 17));
        Take(level6, "Two-Weapon Fighting");
        Assert.Empty(level6.FeatProblems(improved));

        var lowDex = Build(TestData.Fighter(), 6, (Ability.Dexterity, 15));
        Take(lowDex, "Two-Weapon Fighting");
        Assert.Contains(lowDex.FeatProblems(improved), p => p.Contains("Dex 17"));
    }

    // Verifies that the Greater Two-Weapon Fighting feat requires Dexterity 19 and a base attack bonus of +11.
    [Fact]
    public void GreaterTwoWeaponFighting_NeedsDex19AndBaseAttackBonusEleven()
    {
        var greater = TestData.Feat("Greater Two-Weapon Fighting");

        var level10 = Build(TestData.Fighter(), 10, (Ability.Dexterity, 19));
        Take(level10, "Two-Weapon Fighting", "Improved Two-Weapon Fighting");
        var problems = level10.FeatProblems(greater);
        Assert.Single(problems);
        Assert.Contains("base attack bonus +11", problems[0]);

        var level11 = Build(TestData.Fighter(), 11, (Ability.Dexterity, 19));
        Take(level11, "Two-Weapon Fighting", "Improved Two-Weapon Fighting");
        Assert.Empty(level11.FeatProblems(greater));
    }

    // Verifies that the Two-Weapon Rend feat requires Double Slice and Improved Two-Weapon Fighting.
    [Fact]
    public void TwoWeaponRend_NeedsDoubleSliceAndImprovedTwoWeaponFighting()
    {
        var rend = TestData.Feat("Two-Weapon Rend");

        var character = Build(TestData.Fighter(), 11, (Ability.Dexterity, 17));
        Take(character, "Two-Weapon Fighting", "Improved Two-Weapon Fighting");
        Assert.Contains(character.FeatProblems(rend), p => p.Contains("Double Slice"));

        Take(character, "Double Slice");
        Assert.Empty(character.FeatProblems(rend));
    }

    // ---- Shields ----

    // Verifies that the Greater Shield Focus feat requires a Fighter level of 8.
    [Fact]
    public void GreaterShieldFocus_NeedsFighterLevelEight()
    {
        var greater = TestData.Feat("Greater Shield Focus");

        var level7 = Build(TestData.Fighter(), 7);
        Take(level7, "Shield Focus");
        Assert.Contains(level7.FeatProblems(greater), p => p.Contains("Fighter level 8"));

        var level8 = Build(TestData.Fighter(), 8);
        Take(level8, "Shield Focus");
        Assert.Empty(level8.FeatProblems(greater));
    }

    // Verifies that the Shield Slam feat requires Improved Shield Bash, Two-Weapon Fighting, and a base attack bonus of +6.
    [Fact]
    public void ShieldSlam_NeedsImprovedShieldBashTwoWeaponFightingAndBaseAttackBonusSix()
    {
        var slam = TestData.Feat("Shield Slam");

        var level5 = Build(TestData.Fighter(), 5, (Ability.Dexterity, 15));
        Take(level5, "Improved Shield Bash", "Two-Weapon Fighting");
        var problems = level5.FeatProblems(slam);
        Assert.Single(problems);
        Assert.Contains("base attack bonus +6", problems[0]);

        var level6 = Build(TestData.Fighter(), 6, (Ability.Dexterity, 15));
        Take(level6, "Improved Shield Bash", "Two-Weapon Fighting");
        Assert.Empty(level6.FeatProblems(slam));
    }

    // Verifies that the Shield Master feat requires Shield Slam and a base attack bonus of +11.
    [Fact]
    public void ShieldMaster_NeedsShieldSlamAndBaseAttackBonusEleven()
    {
        var master = TestData.Feat("Shield Master");

        var level10 = Build(TestData.Fighter(), 10, (Ability.Dexterity, 15));
        Take(level10, "Improved Shield Bash", "Two-Weapon Fighting", "Shield Slam");
        var problems = level10.FeatProblems(master);
        Assert.Single(problems);
        Assert.Contains("base attack bonus +11", problems[0]);

        var level11 = Build(TestData.Fighter(), 11, (Ability.Dexterity, 15));
        Take(level11, "Improved Shield Bash", "Two-Weapon Fighting", "Shield Slam");
        Assert.Empty(level11.FeatProblems(master));
    }

    // ---- Armor ----

    // Verifies that the Armor Proficiency, Medium feat requires Armor Proficiency, Light first.
    [Fact]
    public void ArmorProficiencyMedium_NeedsLightFirst_ForAWizard()
    {
        var wizard = Build(TestData.Wizard(), 3);
        var medium = TestData.Feat("Armor Proficiency, Medium");

        Assert.Contains(wizard.FeatProblems(medium), p => p.Contains("Armor Proficiency, Light"));

        Take(wizard, "Armor Proficiency, Light");

        Assert.Empty(wizard.FeatProblems(medium));
    }

    // Verifies that the Armor Proficiency, Heavy feat requires both lighter armor proficiencies first.
    [Fact]
    public void ArmorProficiencyHeavy_NeedsBothLighterProficiencies_ForAWizard()
    {
        var wizard = Build(TestData.Wizard(), 5);
        var heavy = TestData.Feat("Armor Proficiency, Heavy");

        Take(wizard, "Armor Proficiency, Light");
        Assert.Contains(wizard.FeatProblems(heavy), p => p.Contains("Armor Proficiency, Medium"));

        Take(wizard, "Armor Proficiency, Medium");
        Assert.Empty(wizard.FeatProblems(heavy));
    }

    // Verifies that a Fighter already meets the armor proficiency chain.
    [Fact]
    public void Fighter_AlreadyMeetsTheArmorProficiencyChain()
    {
        var fighter = Build(TestData.Fighter(), 1);

        Assert.Empty(fighter.UnmetPrerequisites(TestData.Feat("Armor Proficiency, Heavy")));
    }

    // Verifies that the Arcane Armor Training feat requires light armor proficiency and a caster level of 3.
    [Fact]
    public void ArcaneArmorTraining_NeedsLightArmorAndCasterLevelThree()
    {
        var training = TestData.Feat("Arcane Armor Training");

        var level3 = Build(TestData.Wizard(), 3);
        Assert.Contains(level3.FeatProblems(training), p => p.Contains("Armor Proficiency, Light"));
        Take(level3, "Armor Proficiency, Light");
        Assert.Empty(level3.FeatProblems(training));

        var level2 = Build(TestData.Wizard(), 2);
        Take(level2, "Armor Proficiency, Light");
        Assert.Contains(level2.FeatProblems(training), p => p.Contains("caster level 3"));
    }

    // Verifies that the Arcane Armor Training feat requires a caster level of 3 even when armor proficiency is granted.
    [Fact]
    public void ArcaneArmorTraining_NeedsACasterEvenWhenArmorIsGranted()
    {
        var fighter = Build(TestData.Fighter(), 3);

        var problems = fighter.FeatProblems(TestData.Feat("Arcane Armor Training"));

        Assert.Contains(problems, p => p.Contains("caster level 3"));
        Assert.DoesNotContain(problems, p => p.Contains("Armor Proficiency"));
    }

    // Verifies that the Arcane Armor Mastery feat requires Arcane Armor Training, medium armor proficiency, and a caster level of 7.
    [Fact]
    public void ArcaneArmorMastery_NeedsTrainingMediumArmorAndCasterLevelSeven()
    {
        var mastery = TestData.Feat("Arcane Armor Mastery");

        var level7 = Build(TestData.Wizard(), 7);
        Take(level7, "Armor Proficiency, Light", "Armor Proficiency, Medium", "Arcane Armor Training");
        Assert.Empty(level7.FeatProblems(mastery));

        var level6 = Build(TestData.Wizard(), 6);
        Take(level6, "Armor Proficiency, Light", "Armor Proficiency, Medium", "Arcane Armor Training");
        Assert.Contains(level6.FeatProblems(mastery), p => p.Contains("caster level 7"));
    }

    // ---- Vital Strike line and base attack bonus feats ----

    // Verifies that base attack bonus feats unlock at their required base attack bonus.
    [Theory]
    [InlineData("Vital Strike", "", 6)]
    [InlineData("Improved Vital Strike", "Vital Strike", 11)]
    [InlineData("Greater Vital Strike", "Vital Strike,Improved Vital Strike", 16)]
    [InlineData("Lunge", "", 6)]
    [InlineData("Strike Back", "", 11)]
    public void BaseAttackBonusFeats_UnlockAtTheirBaseAttackBonus(string name, string chain, int bab)
    {
        string[] chainFeats = chain.Length == 0 ? [] : chain.Split(',');

        // A fighter's base attack bonus equals their level.
        var tooLow = Build(TestData.Fighter(), bab - 1);
        Take(tooLow, chainFeats);
        var problems = tooLow.FeatProblems(TestData.Feat(name));
        Assert.Single(problems);
        Assert.Contains($"base attack bonus +{bab}", problems[0]);

        var ready = Build(TestData.Fighter(), bab);
        Take(ready, chainFeats);
        Assert.Empty(ready.FeatProblems(TestData.Feat(name)));
    }

    // Verifies that the Step Up feat requires a base attack bonus of +1.
    [Fact]
    public void StepUp_NeedsBaseAttackBonusOne()
    {
        var stepUp = TestData.Feat("Step Up");

        Assert.Contains(Build(TestData.Wizard(), 1).FeatProblems(stepUp),   // BAB +0
            p => p.Contains("base attack bonus +1"));
        Assert.Empty(Build(TestData.Wizard(), 2).FeatProblems(stepUp));     // BAB +1
    }

    // ---- Either-or prerequisites ----

    // Verifies that the Improvised Weapon Mastery feat accepts either Catch Off-Guard or Throw Anything as a prerequisite.
    [Theory]
    [InlineData("Catch Off-Guard")]
    [InlineData("Throw Anything")]
    public void ImprovisedWeaponMastery_AcceptsEitherPrerequisiteFeat(string prerequisite)
    {
        var fighter = Build(TestData.Fighter(), 8);   // BAB +8
        var mastery = TestData.Feat("Improvised Weapon Mastery");

        Assert.Contains(fighter.FeatProblems(mastery),
            p => p.Contains("Catch Off-Guard or Throw Anything"));

        Take(fighter, prerequisite);

        Assert.Empty(fighter.FeatProblems(mastery));
    }

    // Verifies that the Improvised Weapon Mastery feat also requires a base attack bonus of +8.
    [Fact]
    public void ImprovisedWeaponMastery_AlsoNeedsBaseAttackBonusEight()
    {
        var fighter = Build(TestData.Fighter(), 7);
        Take(fighter, "Throw Anything");

        var problems = fighter.FeatProblems(TestData.Feat("Improvised Weapon Mastery"));

        Assert.Single(problems);
        Assert.Contains("base attack bonus +8", problems[0]);
    }
}