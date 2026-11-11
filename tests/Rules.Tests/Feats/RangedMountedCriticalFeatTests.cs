using Rules.Core;
using Rules.Core.Data;

namespace Rules.Tests;

public class RangedMountedCriticalFeatTests
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

    // Verifies that every feat in this batch exists and is tagged as a combat or general feat.
    [Fact]
    public void EveryFeatInThisBatch_Exists_AndIsTaggedCorrectly()
    {
        string[] combat =
        [
            "Arcane Strike", "Channel Smite", "Deadly Aim", "Spellbreaker",
            "Blinding Critical", "Critical Mastery", "Deafening Critical", "Sickening Critical",
            "Staggering Critical", "Stunning Critical", "Tiring Critical", "Exhausting Critical",
            "Mounted Archery", "Ride-By Attack", "Spirited Charge", "Trample", "Unseat",
            "Point-Blank Shot", "Far Shot", "Precise Shot", "Improved Precise Shot",
            "Pinpoint Targeting", "Shot on the Run", "Rapid Shot", "Manyshot",
            "Quick Draw", "Rapid Reload"
        ];

        Assert.Equal(27, combat.Length);
        foreach (var name in combat)
            Assert.Contains(FeatType.Combat, TestData.Feat(name).Types);

        // Combat Casting is the one general feat in the batch.
        var casting = TestData.Feat("Combat Casting");
        Assert.Contains(FeatType.General, casting.Types);
        Assert.DoesNotContain(FeatType.Combat, casting.Types);
    }

    // Verifies that the critical feats are correctly tagged in the data.
    [Fact]
    public void CriticalFeats_AreTheEightTheBookTags()
    {
        string[] expected =
        [
            "Bleeding Critical", "Blinding Critical", "Deafening Critical", "Exhausting Critical",
            "Sickening Critical", "Staggering Critical", "Stunning Critical", "Tiring Critical"
        ];

        var tagged = TestData.Feats()
            .Where(f => f.Types.Contains(FeatType.Critical))
            .Select(f => f.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expected, tagged);
    }

    // ---- Spellcasting feats ----

    // Verifies that the Arcane Strike feat requires the ability to cast arcane spells.
    [Fact]
    public void ArcaneStrike_NeedsTheAbilityToCastArcaneSpells()
    {
        var arcaneStrike = TestData.Feat("Arcane Strike");

        Assert.Empty(Build(TestData.Wizard(), 1).FeatProblems(arcaneStrike));
        Assert.Contains(Build(TestData.Fighter(), 1).FeatProblems(arcaneStrike),
            p => p.Contains("ability to cast arcane spells"));
    }

    // Verifies that the Arcane Strike feat rejects a divine caster.
    [Fact]
    public void ArcaneStrike_RejectsADivineCaster()
    {
        var wizard = TestData.Wizard();
        var divineCaster = wizard with
        {
            Name = "Divine Test Class",
            Spellcasting = wizard.Spellcasting! with { MagicType = MagicType.Divine }
        };

        Assert.Contains(Build(divineCaster, 1).FeatProblems(TestData.Feat("Arcane Strike")),
            p => p.Contains("arcane spells"));
    }

    // Verifies that the CanCastAt method correctly identifies levels with spell slots.
    [Fact]
    public void CanCastAt_IsTrueOnlyForLevelsWithSpellSlots()
    {
        var casting = TestData.Wizard().Spellcasting!;

        Assert.False(casting.CanCastAt(0));
        Assert.True(casting.CanCastAt(1));
        Assert.True(casting.CanCastAt(20));
        Assert.False(casting.CanCastAt(21));
    }

    // Verifies that the Channel Smite feat requires the Channel Energy class feature.
    [Fact]
    public void ChannelSmite_NeedsChannelEnergy()
    {
        Assert.Contains(Build(TestData.Wizard(), 1).FeatProblems(TestData.Feat("Channel Smite")),
            p => p.Contains("Channel Energy class feature"));
    }

    // Verifies that the Combat Casting feat has no prerequisites.
    [Fact]
    public void CombatCasting_HasNoPrerequisites()
    {
        Assert.Empty(Build(TestData.Fighter(), 1).FeatProblems(TestData.Feat("Combat Casting")));
    }

    // ---- Deadly Aim and Spellbreaker ----

    // Verifies that the Deadly Aim feat requires Dexterity 13 and base attack bonus +1.
    [Fact]
    public void DeadlyAim_NeedsDex13AndBaseAttackBonusOne()
    {
        var deadlyAim = TestData.Feat("Deadly Aim");

        Assert.Empty(Build(TestData.Fighter(), 1, (Ability.Dexterity, 13)).FeatProblems(deadlyAim));
        Assert.Contains(
            Build(TestData.Fighter(), 1, (Ability.Dexterity, 12)).FeatProblems(deadlyAim),
            p => p.Contains("Dex 13"));
        Assert.Contains(
            Build(TestData.Wizard(), 1, (Ability.Dexterity, 13)).FeatProblems(deadlyAim),
            p => p.Contains("base attack bonus +1"));
    }

    // Verifies that the Spellbreaker feat requires the Disruptive feat and Fighter level 10.
    [Fact]
    public void Spellbreaker_NeedsDisruptiveAndFighterLevelTen()
    {
        var spellbreaker = TestData.Feat("Spellbreaker");

        var level9 = Build(TestData.Fighter(), 9);
        Take(level9, "Disruptive");
        var problems = level9.FeatProblems(spellbreaker);
        Assert.Single(problems);
        Assert.Contains("Fighter level 10", problems[0]);

        var level10 = Build(TestData.Fighter(), 10);
        Take(level10, "Disruptive");
        Assert.Empty(level10.FeatProblems(spellbreaker));
    }

    // ---- Critical feats ----

    // Verifies that critical feats require Critical Focus, their prerequisite in the chain, and the appropriate base attack bonus.
    [Theory]
    [InlineData("Sickening Critical", "", 11)]
    [InlineData("Staggering Critical", "", 13)]
    [InlineData("Tiring Critical", "", 13)]
    [InlineData("Deafening Critical", "", 13)]
    [InlineData("Blinding Critical", "", 15)]
    [InlineData("Stunning Critical", "Staggering Critical", 17)]
    [InlineData("Exhausting Critical", "Tiring Critical", 15)]
    public void CriticalFeats_NeedCriticalFocusTheirChainAndBaseAttackBonus(
        string name, string chain, int bab)
    {
        string[] prerequisites = chain.Length == 0
            ? new[] { "Critical Focus" }
            : new[] { "Critical Focus", chain };

        // A fighter's base attack bonus equals their level.
        var tooLow = Build(TestData.Fighter(), bab - 1);
        Take(tooLow, prerequisites);
        var problems = tooLow.FeatProblems(TestData.Feat(name));
        Assert.Single(problems);
        Assert.Contains($"base attack bonus +{bab}", problems[0]);

        var ready = Build(TestData.Fighter(), bab);
        Take(ready, prerequisites);
        Assert.Empty(ready.FeatProblems(TestData.Feat(name)));
    }
    
    // Verifies that the Critical Mastery feat requires two other Critical feats, not counting Critical Focus.
    [Fact]
    public void CriticalMastery_NeedsTwoCriticalFeats_NotCountingCriticalFocus()
    {
        var mastery = TestData.Feat("Critical Mastery");
        var fighter = Build(TestData.Fighter(), 14);
        Take(fighter, "Critical Focus");

        Assert.Contains(fighter.FeatProblems(mastery), p => p.Contains("any 2 Critical feats"));

        Take(fighter, "Bleeding Critical");
        Assert.Contains(fighter.FeatProblems(mastery), p => p.Contains("any 2 Critical feats"));

        Take(fighter, "Sickening Critical");
        Assert.Empty(fighter.FeatProblems(mastery));
    }

    // Verifies that the Critical Mastery feat requires Fighter level 14.
    [Fact]
    public void CriticalMastery_NeedsFighterLevelFourteen()
    {
        var level13 = Build(TestData.Fighter(), 13);
        Take(level13, "Critical Focus", "Bleeding Critical", "Sickening Critical");

        var problems = level13.FeatProblems(TestData.Feat("Critical Mastery"));

        Assert.Single(problems);
        Assert.Contains("Fighter level 14", problems[0]);
    }

    // ---- Mounted combat ----

    // Verifies that mounted combat feats require the Mounted Combat feat.
    [Theory]
    [InlineData("Mounted Archery")]
    [InlineData("Ride-By Attack")]
    [InlineData("Trample")]
    public void MountedFeats_NeedMountedCombat(string name)
    {
        var fighter = Build(TestData.Fighter(), 3);
        fighter.SetSkillRanks(TestData.Skill("Ride"), 1);
        var feat = TestData.Feat(name);

        Assert.Contains(fighter.FeatProblems(feat), p => p.Contains("Mounted Combat"));

        Take(fighter, "Mounted Combat");

        Assert.Empty(fighter.FeatProblems(feat));
    }

    // Verifies that the Spirited Charge feat requires the Ride-By Attack feat.
    [Fact]
    public void SpiritedCharge_NeedsRideByAttack()
    {
        var fighter = Build(TestData.Fighter(), 5);
        fighter.SetSkillRanks(TestData.Skill("Ride"), 1);
        var charge = TestData.Feat("Spirited Charge");
        Take(fighter, "Mounted Combat");

        Assert.Contains(fighter.FeatProblems(charge), p => p.Contains("Ride-By Attack"));

        Take(fighter, "Ride-By Attack");

        Assert.Empty(fighter.FeatProblems(charge));
    }

    // Verifies that the Unseat feat requires the full mounted combat chain.
    [Fact]
    public void Unseat_NeedsTheWholeChain()
    {
        var fighter = Build(TestData.Fighter(), 7, (Ability.Strength, 13));
        fighter.SetSkillRanks(TestData.Skill("Ride"), 1);
        var unseat = TestData.Feat("Unseat");
        Take(fighter, "Power Attack", "Mounted Combat");

        var problems = fighter.FeatProblems(unseat);
        Assert.Single(problems);
        Assert.Contains("Improved Bull Rush", problems[0]);

        Take(fighter, "Improved Bull Rush");

        Assert.Empty(fighter.FeatProblems(unseat));
    }

    // Verifies that the Unseat feat reports a missing Ride skill rank if not met.
    [Fact]
    public void Unseat_ReportsAMissingRideRank()
    {
        var fighter = Build(TestData.Fighter(), 7, (Ability.Strength, 13));
        Take(fighter, "Power Attack", "Improved Bull Rush");

        var problems = fighter.FeatProblems(TestData.Feat("Unseat"));

        Assert.Contains(problems, p => p.Contains("1 rank(s) in Ride"));
        Assert.Contains(problems, p => p.Contains("Mounted Combat"));
    }

    // ---- Ranged combat ----

    // Verifies that the Point-Blank Shot feat has no prerequisites.
    [Fact]
    public void PointBlankShot_HasNoPrerequisites()
    {
        Assert.Empty(Build(TestData.Wizard(), 1).FeatProblems(TestData.Feat("Point-Blank Shot")));
    }

    // Verifies that basic ranged feats require the Point-Blank Shot feat.
    [Theory]
    [InlineData("Far Shot")]
    [InlineData("Precise Shot")]
    [InlineData("Rapid Shot")]
    public void BasicRangedFeats_NeedPointBlankShot(string name)
    {
        var fighter = Build(TestData.Fighter(), 3, (Ability.Dexterity, 13));
        var feat = TestData.Feat(name);

        Assert.Contains(fighter.FeatProblems(feat), p => p.Contains("Point-Blank Shot"));

        Take(fighter, "Point-Blank Shot");

        Assert.Empty(fighter.FeatProblems(feat));
    }

    // Verifies that the Rapid Shot feat requires Dexterity 13.
    [Fact]
    public void RapidShot_NeedsDex13()
    {
        var fighter = Build(TestData.Fighter(), 3, (Ability.Dexterity, 12));
        Take(fighter, "Point-Blank Shot");

        Assert.Contains(fighter.FeatProblems(TestData.Feat("Rapid Shot")),
            p => p.Contains("Dex 13"));
    }
    
    // Verifies that the Improved Precise Shot feat requires Dexterity 19 and base attack bonus +11.
    [Fact]
    public void ImprovedPreciseShot_NeedsDex19AndBaseAttackBonusEleven()
    {
        var improved = TestData.Feat("Improved Precise Shot");

        var level10 = Build(TestData.Fighter(), 10, (Ability.Dexterity, 19));
        Take(level10, "Point-Blank Shot", "Precise Shot");
        var problems = level10.FeatProblems(improved);
        Assert.Single(problems);
        Assert.Contains("base attack bonus +11", problems[0]);

        var level11 = Build(TestData.Fighter(), 11, (Ability.Dexterity, 19));
        Take(level11, "Point-Blank Shot", "Precise Shot");
        Assert.Empty(level11.FeatProblems(improved));

        var lowDex = Build(TestData.Fighter(), 11, (Ability.Dexterity, 18));
        Take(lowDex, "Point-Blank Shot", "Precise Shot");
        Assert.Contains(lowDex.FeatProblems(improved), p => p.Contains("Dex 19"));
    }

    // Verifies that the Pinpoint Targeting feat requires base attack bonus +16.
    [Fact]
    public void PinpointTargeting_NeedsBaseAttackBonusSixteen()
    {
        var pinpoint = TestData.Feat("Pinpoint Targeting");

        var level15 = Build(TestData.Fighter(), 15, (Ability.Dexterity, 19));
        Take(level15, "Point-Blank Shot", "Precise Shot", "Improved Precise Shot");
        var problems = level15.FeatProblems(pinpoint);
        Assert.Single(problems);
        Assert.Contains("base attack bonus +16", problems[0]);

        var level16 = Build(TestData.Fighter(), 16, (Ability.Dexterity, 19));
        Take(level16, "Point-Blank Shot", "Precise Shot", "Improved Precise Shot");
        Assert.Empty(level16.FeatProblems(pinpoint));
    }

    // Verifies that the Shot on the Run feat requires base attack bonus +4.
    [Fact]
    public void ShotOnTheRun_NeedsBaseAttackBonusFour()
    {
        var shot = TestData.Feat("Shot on the Run");

        // A wizard's base attack bonus is half their level: 7 gives +3, 8 gives +4.
        // (A fighter would run out of feat slots before collecting all the prerequisites.)
        var level7 = Build(TestData.Wizard(), 7, (Ability.Dexterity, 13));
        Take(level7, "Dodge", "Mobility", "Point-Blank Shot");
        Assert.Contains(level7.FeatProblems(shot), p => p.Contains("base attack bonus +4"));

        var level8 = Build(TestData.Wizard(), 8, (Ability.Dexterity, 13));
        Take(level8, "Dodge", "Mobility", "Point-Blank Shot");
        Assert.Empty(level8.FeatProblems(shot));
    }

    // Verifies that the Manyshot feat requires Rapid Shot, Dexterity 17, and base attack bonus +6.
    [Fact]
    public void Manyshot_NeedsRapidShotDex17AndBaseAttackBonusSix()
    {
        var manyshot = TestData.Feat("Manyshot");

        var level5 = Build(TestData.Fighter(), 5, (Ability.Dexterity, 17));
        Take(level5, "Point-Blank Shot", "Rapid Shot");
        var problems = level5.FeatProblems(manyshot);
        Assert.Single(problems);
        Assert.Contains("base attack bonus +6", problems[0]);

        var level6 = Build(TestData.Fighter(), 6, (Ability.Dexterity, 17));
        Take(level6, "Point-Blank Shot", "Rapid Shot");
        Assert.Empty(level6.FeatProblems(manyshot));

        var lowDex = Build(TestData.Fighter(), 6, (Ability.Dexterity, 15));
        Take(lowDex, "Point-Blank Shot", "Rapid Shot");
        Assert.Contains(lowDex.FeatProblems(manyshot), p => p.Contains("Dex 17"));
    }

    // Verifies that the Quick Draw feat requires base attack bonus +1.
    [Fact]
    public void QuickDraw_NeedsBaseAttackBonusOne()
    {
        var quickDraw = TestData.Feat("Quick Draw");

        Assert.Contains(Build(TestData.Wizard(), 1).FeatProblems(quickDraw),   // BAB +0
            p => p.Contains("base attack bonus +1"));
        Assert.Empty(Build(TestData.Wizard(), 2).FeatProblems(quickDraw));     // BAB +1
    }

    // Verifies that the Rapid Reload feat asks which crossbow is being used.
    [Fact]
    public void RapidReload_AsksWhichCrossbow()
    {
        var rapidReload = TestData.Feat("Rapid Reload");
        var wizard = Build(TestData.Wizard(), 1);

        Assert.Contains(wizard.FeatProblems(rapidReload), p => p.Contains("needs a choice"));
        Assert.Empty(wizard.FeatProblems(rapidReload, "Light crossbow"));
    }
}