using Rules.Core;
using Rules.Core.Data;

namespace Rules.Tests;

public class CombatFeatTests
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

    // Verifies that weapon feats require a specific weapon to be chosen.
    [Theory]
    [InlineData("Weapon Focus")]
    [InlineData("Greater Weapon Focus")]
    [InlineData("Weapon Specialization")]
    [InlineData("Greater Weapon Specialization")]
    [InlineData("Improved Critical")]
    [InlineData("Exotic Weapon Proficiency")]
    [InlineData("Martial Weapon Proficiency")]
    public void WeaponFeats_AskForAWeapon(string name)
    {
        Assert.Equal(FeatSubject.Weapon, TestData.Feat(name).Subject);
    }

    // Verifies that all melee batch feats are combat feats.
    [Fact]
    public void AllMeleeBatchFeats_AreCombatFeats()
    {
        string[] names =
        [
            "Greater Weapon Focus", "Weapon Specialization", "Greater Weapon Specialization",
            "Penetrating Strike", "Greater Penetrating Strike", "Improved Critical",
            "Exotic Weapon Proficiency", "Martial Weapon Proficiency", "Simple Weapon Proficiency",
            "Weapon Finesse", "Dazzling Display", "Shatter Defenses", "Deadly Stroke",
            "Agile Maneuvers", "Defensive Combat Training", "Combat Reflexes", "Stand Still",
            "Mobility", "Spring Attack", "Whirlwind Attack", "Wind Stance", "Lightning Stance"
        ];

        foreach (var name in names)
            Assert.Contains(FeatType.Combat, TestData.Feat(name).Types);
    }

    // ---- Feats with no prerequisites ----

    // Verifies that combat feats with no prerequisites are available at level one.
    [Theory]
    [InlineData("Agile Maneuvers", null)]
    [InlineData("Defensive Combat Training", null)]
    [InlineData("Combat Reflexes", null)]
    [InlineData("Weapon Finesse", null)]
    [InlineData("Simple Weapon Proficiency", null)]
    [InlineData("Martial Weapon Proficiency", "Longsword")]
    public void NoPrerequisiteCombatFeats_AreAvailableAtLevelOne(string name, string? subject)
    {
        Assert.Empty(Build(TestData.Wizard(), 1).FeatProblems(TestData.Feat(name), subject));
    }

    // Verifies that Exotic Weapon Proficiency requires a base attack bonus of +1.
    [Fact]
    public void ExoticWeaponProficiency_NeedsBaseAttackBonusOne()
    {
        var feat = TestData.Feat("Exotic Weapon Proficiency");

        Assert.Contains(Build(TestData.Wizard(), 1).FeatProblems(feat, "Whip"),
            p => p.Contains("base attack bonus +1"));
        Assert.Empty(Build(TestData.Wizard(), 2).FeatProblems(feat, "Whip"));
    }

    // Verifies that Improved Critical requires a base attack bonus of +8.
    [Theory]
    [InlineData("Fighter", 7, false)]
    [InlineData("Fighter", 8, true)]
    [InlineData("Wizard", 15, false)]   // BAB +7
    [InlineData("Wizard", 16, true)]    // BAB +8
    public void ImprovedCritical_NeedsBaseAttackBonusEight(string className, int level, bool allowed)
    {
        var cls = className == "Fighter" ? TestData.Fighter() : TestData.Wizard();
        var problems = Build(cls, level).FeatProblems(TestData.Feat("Improved Critical"), "Longsword");

        Assert.Equal(allowed, problems.Count == 0);
    }

    // ---- Weapon chains ----

    // Verifies that Greater Weapon Focus requires Weapon Focus in the same weapon and a fighter level of 8.
    [Fact]
    public void GreaterWeaponFocus_NeedsWeaponFocusInTheSameWeapon_AndFighterLevelEight()
    {
        var greater = TestData.Feat("Greater Weapon Focus");

        var level8 = Build(TestData.Fighter(), 8);
        level8.AddFeat(TestData.Feat("Weapon Focus"), "Longsword");

        Assert.Contains(level8.FeatProblems(greater, "Rapier"),
            p => p.Contains("Weapon Focus (Rapier)"));
        Assert.Empty(level8.FeatProblems(greater, "Longsword"));

        var level7 = Build(TestData.Fighter(), 7);
        level7.AddFeat(TestData.Feat("Weapon Focus"), "Longsword");

        Assert.Contains(level7.FeatProblems(greater, "Longsword"),
            p => p.Contains("Fighter level 8"));
    }

    // Verifies that Weapon Specialization requires a fighter level of 4 and Weapon Focus in the same weapon.
    [Fact]
    public void WeaponSpecialization_NeedsFighterLevelFour_AndWeaponFocusInTheSameWeapon()
    {
        var specialization = TestData.Feat("Weapon Specialization");

        var level3 = Build(TestData.Fighter(), 3);
        level3.AddFeat(TestData.Feat("Weapon Focus"), "Longsword");
        Assert.Contains(level3.FeatProblems(specialization, "Longsword"),
            p => p.Contains("Fighter level 4"));

        var level4 = Build(TestData.Fighter(), 4);
        level4.AddFeat(TestData.Feat("Weapon Focus"), "Longsword");
        Assert.Empty(level4.FeatProblems(specialization, "Longsword"));
        Assert.NotEmpty(level4.FeatProblems(specialization, "Rapier"));
    }

    // Verifies that Weapon Specialization is closed to non-fighters.
    [Fact]
    public void WeaponSpecialization_IsClosedToNonFighters()
    {
        var wizard = Build(TestData.Wizard(), 8);   // BAB +4 is plenty, but not a fighter
        wizard.AddFeat(TestData.Feat("Weapon Focus"), "Dagger");

        Assert.Contains(wizard.FeatProblems(TestData.Feat("Weapon Specialization"), "Dagger"),
            p => p.Contains("Fighter level 4"));
    }

    // Verifies that Greater Weapon Specialization requires the whole chain in one weapon.
    [Fact]
    public void GreaterWeaponSpecialization_NeedsTheWholeChainInOneWeapon()
    {
        var character = Build(TestData.Fighter(), 12);
        var greater = TestData.Feat("Greater Weapon Specialization");
        character.AddFeat(TestData.Feat("Weapon Focus"), "Longsword");
        character.AddFeat(TestData.Feat("Greater Weapon Focus"), "Longsword");

        var problems = character.FeatProblems(greater, "Longsword");
        Assert.Single(problems);
        Assert.Contains("Weapon Specialization (Longsword)", problems[0]);

        character.AddFeat(TestData.Feat("Weapon Specialization"), "Longsword");

        Assert.Empty(character.FeatProblems(greater, "Longsword"));
        Assert.NotEmpty(character.FeatProblems(greater, "Rapier"));

        character.AddFeat(greater, "Longsword");
        Assert.Equal(4, character.Feats.Count);
    }

    // Verifies that Penetrating Strike requires a fighter level of 12.
    [Theory]
    [InlineData(11, false)]
    [InlineData(12, true)]
    public void PenetratingStrike_NeedsFighterLevelTwelve(int level, bool allowed)
    {
        var character = Build(TestData.Fighter(), level);
        character.AddFeat(TestData.Feat("Weapon Focus"), "Longsword");

        var problems = character.FeatProblems(TestData.Feat("Penetrating Strike"));

        Assert.Equal(allowed, problems.Count == 0);
    }

    // Verifies that Greater Penetrating Strike requires a fighter level of 16.
    [Fact]
    public void GreaterPenetratingStrike_NeedsFighterLevelSixteen()
    {
        var greater = TestData.Feat("Greater Penetrating Strike");

        var level15 = Build(TestData.Fighter(), 15);
        level15.AddFeat(TestData.Feat("Weapon Focus"), "Longsword");
        Take(level15, "Penetrating Strike");
        Assert.Contains(level15.FeatProblems(greater), p => p.Contains("Fighter level 16"));

        var level16 = Build(TestData.Fighter(), 16);
        level16.AddFeat(TestData.Feat("Weapon Focus"), "Longsword");
        Take(level16, "Penetrating Strike");
        Assert.Empty(level16.FeatProblems(greater));
    }

    // Verifies that Shatter Defenses requires Dazzling Display and a base attack bonus of +6.
    [Fact]
    public void ShatterDefenses_NeedsDazzlingDisplayAndBaseAttackBonusSix()
    {
        var shatter = TestData.Feat("Shatter Defenses");

        var level5 = Build(TestData.Fighter(), 5);
        level5.AddFeat(TestData.Feat("Weapon Focus"), "Longsword");
        Take(level5, "Dazzling Display");
        var problems = level5.FeatProblems(shatter);
        Assert.Single(problems);
        Assert.Contains("base attack bonus +6", problems[0]);

        var level6 = Build(TestData.Fighter(), 6);
        level6.AddFeat(TestData.Feat("Weapon Focus"), "Longsword");
        Take(level6, "Dazzling Display");
        Assert.Empty(level6.FeatProblems(shatter));
    }

    // Verifies that Deadly Stroke requires the whole Dazzling Display chain.
    [Fact]
    public void DeadlyStroke_NeedsTheWholeDazzlingDisplayChain()
    {
        var deadly = TestData.Feat("Deadly Stroke");

        var level10 = Build(TestData.Fighter(), 10);
        level10.AddFeat(TestData.Feat("Weapon Focus"), "Longsword");
        level10.AddFeat(TestData.Feat("Greater Weapon Focus"), "Longsword");
        Take(level10, "Dazzling Display", "Shatter Defenses");
        var problems = level10.FeatProblems(deadly);
        Assert.Single(problems);
        Assert.Contains("base attack bonus +11", problems[0]);

        var level11 = Build(TestData.Fighter(), 11);
        level11.AddFeat(TestData.Feat("Weapon Focus"), "Longsword");
        Take(level11, "Dazzling Display", "Shatter Defenses");
        Assert.Contains(level11.FeatProblems(deadly), p => p.Contains("Greater Weapon Focus"));

        level11.AddFeat(TestData.Feat("Greater Weapon Focus"), "Longsword");
        Assert.Empty(level11.FeatProblems(deadly));
    }

    // ---- Combat maneuvers ----

    // Verifies that Improved Disarm, Improved Feint, and Improved Trip require Intelligence 13 and Combat Expertise.
    [Fact]
    public void ImprovedFeintDisarmAndTrip_NeedIntelligenceAndCombatExpertise()
    {
        var wizard = Build(TestData.Wizard(), 3, (Ability.Intelligence, 13));
        string[] improved = ["Improved Disarm", "Improved Feint", "Improved Trip"];

        foreach (var name in improved)
            Assert.Contains(wizard.FeatProblems(TestData.Feat(name)),
                p => p.Contains("Combat Expertise"));

        Take(wizard, "Combat Expertise");

        foreach (var name in improved)
            Assert.Empty(wizard.FeatProblems(TestData.Feat(name)));
    }

    // Verifies that Improved Strength Maneuvers require Power Attack.
    [Theory]
    [InlineData("Improved Bull Rush")]
    [InlineData("Improved Overrun")]
    [InlineData("Improved Sunder")]
    public void ImprovedStrengthManeuvers_NeedPowerAttack(string name)
    {
        var fighter = Build(TestData.Fighter(), 3, (Ability.Strength, 13));
        var feat = TestData.Feat(name);

        Assert.Contains(fighter.FeatProblems(feat), p => p.Contains("Power Attack"));

        Take(fighter, "Power Attack");

        Assert.Empty(fighter.FeatProblems(feat));
    }

    // Verifies that Greater Maneuver feats require a base attack bonus of +6 and their respective chain feats.
    [Theory]
    [InlineData("Greater Bull Rush", "Power Attack,Improved Bull Rush")]
    [InlineData("Greater Overrun", "Power Attack,Improved Overrun")]
    [InlineData("Greater Sunder", "Power Attack,Improved Sunder")]
    [InlineData("Greater Disarm", "Combat Expertise,Improved Disarm")]
    [InlineData("Greater Feint", "Combat Expertise,Improved Feint")]
    [InlineData("Greater Trip", "Combat Expertise,Improved Trip")]
    public void GreaterManeuverFeats_NeedBaseAttackBonusSixAndTheirChain(string greater, string chain)
    {
        var feat = TestData.Feat(greater);
        string[] chainFeats = chain.Split(',');

        var level5 = Build(TestData.Fighter(), 5,
            (Ability.Strength, 13), (Ability.Intelligence, 13));
        Take(level5, chainFeats);
        var problems = level5.FeatProblems(feat);
        Assert.Single(problems);
        Assert.Contains("base attack bonus +6", problems[0]);

        var level6 = Build(TestData.Fighter(), 6,
            (Ability.Strength, 13), (Ability.Intelligence, 13));
        Take(level6, chainFeats);
        Assert.Empty(level6.FeatProblems(feat));
    }

    // Verifies that Stand Still requires Combat Reflexes.
    [Fact]
    public void StandStill_NeedsCombatReflexes()
    {
        var character = Build(TestData.Fighter(), 3);   // level 3 has two feat slots
        var standStill = TestData.Feat("Stand Still");

        Assert.Contains(character.FeatProblems(standStill), p => p.Contains("Combat Reflexes"));

        Take(character, "Combat Reflexes");

        Assert.Empty(character.FeatProblems(standStill));
    }

    // ---- The Dodge line ----

    // Verifies that Mobility requires Dodge.
    [Fact]
    public void Mobility_NeedsDodge()
    {
        var wizard = Build(TestData.Wizard(), 3, (Ability.Dexterity, 13));
        var mobility = TestData.Feat("Mobility");

        Assert.Contains(wizard.FeatProblems(mobility), p => p.Contains("Dodge"));

        Take(wizard, "Dodge");

        Assert.Empty(wizard.FeatProblems(mobility));
    }

    // Verifies that Spring Attack requires a base attack bonus of +4.
    [Fact]
    public void SpringAttack_NeedsBaseAttackBonusFour()
    {
        var springAttack = TestData.Feat("Spring Attack");

        var level3 = Build(TestData.Fighter(), 3, (Ability.Dexterity, 13));
        Take(level3, "Dodge", "Mobility");
        Assert.Contains(level3.FeatProblems(springAttack), p => p.Contains("base attack bonus +4"));

        var level5 = Build(TestData.Fighter(), 5, (Ability.Dexterity, 13));
        Take(level5, "Dodge", "Mobility");
        Assert.Empty(level5.FeatProblems(springAttack));
    }

    // Verifies that Whirlwind Attack requires the whole chain of prerequisites.
    [Fact]
    public void WhirlwindAttack_NeedsTheWholeChain()
    {
        var character = Build(TestData.Fighter(), 9,
            (Ability.Dexterity, 13), (Ability.Intelligence, 13));
        var whirlwind = TestData.Feat("Whirlwind Attack");

        Take(character, "Combat Expertise", "Dodge", "Mobility");
        Assert.Contains(character.FeatProblems(whirlwind), p => p.Contains("Spring Attack"));

        Take(character, "Spring Attack");
        Assert.Empty(character.FeatProblems(whirlwind));

        character.AddFeat(whirlwind);
        Assert.Equal(5, character.Feats.Count);
    }

    // Verifies that Wind Stance requires Dexterity 15 and a base attack bonus of +6.
    [Fact]
    public void WindStance_NeedsDex15AndBaseAttackBonusSix()
    {
        var windStance = TestData.Feat("Wind Stance");

        var lowDex = Build(TestData.Fighter(), 6, (Ability.Dexterity, 13));
        Take(lowDex, "Dodge");
        Assert.Contains(lowDex.FeatProblems(windStance), p => p.Contains("Dex 15"));

        var ready = Build(TestData.Fighter(), 6, (Ability.Dexterity, 15));
        Take(ready, "Dodge");
        Assert.Empty(ready.FeatProblems(windStance));
    }

    // Verifies that Lightning Stance requires Wind Stance, Dexterity 17, and a base attack bonus of +11.
    [Fact]
    public void LightningStance_NeedsWindStanceDex17AndBaseAttackBonusEleven()
    {
        var lightning = TestData.Feat("Lightning Stance");

        var level10 = Build(TestData.Fighter(), 10, (Ability.Dexterity, 17));
        Take(level10, "Dodge", "Wind Stance");
        var problems = level10.FeatProblems(lightning);
        Assert.Single(problems);
        Assert.Contains("base attack bonus +11", problems[0]);

        var level11 = Build(TestData.Fighter(), 11, (Ability.Dexterity, 17));
        Take(level11, "Dodge", "Wind Stance");
        Assert.Empty(level11.FeatProblems(lightning));
    }
}