using Rules.Core;
using Rules.Core.Data;

namespace Rules.Tests;

public class GeneralFeatTests
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

    // ---- Data guards ----

    // Verifies that the total number of feats matches the expected count from the data file.
    [Fact]
    public void FeatTypeCounts_MatchTable5_1()
    {
        var feats = TestData.Feats();

        Assert.Equal(8, feats.Count(f => f.Types.Contains(FeatType.ItemCreation)));
        Assert.Equal(9, feats.Count(f => f.Types.Contains(FeatType.Metamagic)));
    }

    // Verifies that metamagic feats have no prerequisites.
    [Fact]
    public void MetamagicFeats_HaveNoPrerequisites()
    {
        foreach (var feat in TestData.Feats().Where(f => f.Types.Contains(FeatType.Metamagic)))
            Assert.True(feat.Prerequisites is null || feat.Prerequisites.Count == 0, feat.Name);
    }

    // Verifies that feats with a choice in their modifiers declare a subject.
    [Fact]
    public void FeatsWithAChoiceInTheirModifiers_DeclareASubject()
    {
        foreach (var feat in TestData.Feats())
            if (feat.Modifiers?.Any(m => m.Stat == "{subject}") == true)
                Assert.NotEqual(FeatSubject.None, feat.Subject);
    }

    // Verifies that repeatable feats are the ones the book says stack.
    [Fact]
    public void RepeatableFeats_AreTheOnesTheBookSaysStack()
    {
        var repeatable = TestData.Feats()
            .Where(f => f.Repeatable)
            .Select(f => f.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(
            new[] { "Extra Ki", "Extra Lay On Hands", "Extra Performance", "Extra Rage", "Fleet" },
            repeatable);
    }

    // Verifies that channel feats ask for the right kind of choice.
    [Fact]
    public void ChannelFeats_AskForTheRightKindOfChoice()
    {
        Assert.Equal(FeatSubject.AlignmentSubtype, TestData.Feat("Alignment Channel").Subject);
        Assert.Equal(FeatSubject.ElementalSubtype, TestData.Feat("Elemental Channel").Subject);
        Assert.Equal(FeatSubject.Mercy, TestData.Feat("Extra Mercy").Subject);
    }

    // ---- Item creation ----

    // Verifies that item creation feats require the appropriate caster level.
    [Theory]
    [InlineData("Craft Magic Arms and Armor", 5)]
    [InlineData("Craft Rod", 9)]
    [InlineData("Craft Staff", 11)]
    [InlineData("Craft Wand", 5)]
    [InlineData("Craft Wondrous Item", 3)]
    [InlineData("Forge Ring", 7)]
    public void ItemCreationFeats_NeedTheBooksCasterLevel(string name, int casterLevel)
    {
        var feat = TestData.Feat(name);

        Assert.Empty(Build(TestData.Wizard(), casterLevel).FeatProblems(feat));
        Assert.Contains(Build(TestData.Wizard(), casterLevel - 1).FeatProblems(feat),
            p => p.Contains($"caster level {casterLevel}"));
    }

    // ---- Skill feats ----

    // Verifies that skill feats provide the correct bonuses based on skill ranks.
    [Theory]
    [InlineData("Animal Affinity", "Handle Animal", "Ride")]
    [InlineData("Athletic", "Climb", "Swim")]
    [InlineData("Deceitful", "Bluff", "Disguise")]
    [InlineData("Deft Hands", "Disable Device", "Sleight of Hand")]
    [InlineData("Self-Sufficient", "Heal", "Survival")]
    [InlineData("Stealthy", "Escape Artist", "Stealth")]
    public void SkillFeats_GiveTwoThenFourAtTenRanksPerSkill(
        string featName, string skillA, string skillB)
    {
        var character = Build(TestData.Wizard(), 10);
        character.AddFeat(TestData.Feat(featName));

        Assert.Equal(2, character.Total(skillA));
        Assert.Equal(2, character.Total(skillB));

        character.SetSkillRanks(TestData.Skill(skillA), 10);

        Assert.Equal(4, character.Total(skillA));
        Assert.Equal(2, character.Total(skillB));   // each skill upgrades on its own ranks
    }

    // ---- Feat chains and misc prerequisites ----

    [Fact]
    public void ImprovedGreatFortitude_NeedsGreatFortitude_AndAddsNoBonusItself()
    {
        var character = Build(TestData.Wizard(), 3);

        Assert.Contains(character.FeatProblems(TestData.Feat("Improved Great Fortitude")),
            p => p.Contains("Great Fortitude"));

        character.AddFeat(TestData.Feat("Great Fortitude"));
        character.AddFeat(TestData.Feat("Improved Great Fortitude"));

        Assert.Equal(3, character.FortitudeSave);   // 1 base + 2 from Great Fortitude only
    }

    // Verifies that the Improved Lightning Reflexes feat requires Lightning Reflexes and adds no bonus itself.
    [Fact]
    public void ImprovedLightningReflexes_NeedsLightningReflexes_AndAddsNoBonusItself()
    {
        var character = Build(TestData.Wizard(), 3);

        Assert.Contains(character.FeatProblems(TestData.Feat("Improved Lightning Reflexes")),
            p => p.Contains("Lightning Reflexes"));

        character.AddFeat(TestData.Feat("Lightning Reflexes"));
        character.AddFeat(TestData.Feat("Improved Lightning Reflexes"));

        Assert.Equal(3, character.ReflexSave);      // 1 base + 2 from Lightning Reflexes only
    }

    // Verifies that the Diehard feat requires the Endurance feat.
    [Fact]
    public void Diehard_NeedsEndurance()
    {
        var character = Build(TestData.Fighter(), 3);

        Assert.Throws<InvalidOperationException>(
            () => character.AddFeat(TestData.Feat("Diehard")));

        character.AddFeat(TestData.Feat("Endurance"));
        character.AddFeat(TestData.Feat("Diehard"));

        Assert.Equal(2, character.Feats.Count);
    }

    // Verifies that the Acrobatic Steps feat requires Dexterity 15, even if the character has Nimble Moves.
    [Fact]
    public void AcrobaticSteps_NeedsDex15_EvenWithNimbleMoves()
    {
        var character = Build(TestData.Fighter(), 3, (Ability.Dexterity, 14));
        character.AddFeat(TestData.Feat("Nimble Moves"));

        var problems = character.FeatProblems(TestData.Feat("Acrobatic Steps"));

        Assert.Single(problems);
        Assert.Contains("Dex 15", problems[0]);
    }

    // Verifies that the Acrobatic Steps feat works once both prerequisites are met.
    [Fact]
    public void AcrobaticSteps_WorksOnceBothPrerequisitesAreMet()
    {
        var character = Build(TestData.Fighter(), 3, (Ability.Dexterity, 15));
        character.AddFeat(TestData.Feat("Nimble Moves"));
        character.AddFeat(TestData.Feat("Acrobatic Steps"));

        Assert.Equal(2, character.Feats.Count);
    }

    // Verifies that the Natural Spell feat requires Wisdom 13 and the Wild Shape class feature.
    [Fact]
    public void NaturalSpell_NeedsWisdomAndWildShape()
    {
        var problems = Build(TestData.Wizard(), 1).FeatProblems(TestData.Feat("Natural Spell"));

        Assert.Contains(problems, p => p.Contains("Wis 13"));
        Assert.Contains(problems, p => p.Contains("Wild Shape class feature"));
    }

    // ---- Repeatable feats ----

    // Verifies that the Fleet feat can be taken more than once, but other feats cannot.
    [Fact]
    public void Fleet_CanBeTakenMoreThanOnce_ButOtherFeatsCannot()
    {
        var character = Build(TestData.Fighter(), 7);
        character.AddFeat(TestData.Feat("Fleet"));
        character.AddFeat(TestData.Feat("Fleet"));

        Assert.Equal(2, character.Feats.Count);

        character.AddFeat(TestData.Feat("Run"));

        Assert.Throws<InvalidOperationException>(
            () => character.AddFeat(TestData.Feat("Run")));
    }

    // ---- Prerequisites that name a specific choice ----

    // Verifies that the Augment Summoning feat requires Spell Focus in Conjuration.
    [Fact]
    public void AugmentSummoning_NeedsSpellFocusInConjuration()
    {
        var character = Build(TestData.Wizard(), 5);
        var augment = TestData.Feat("Augment Summoning");
        character.AddFeat(TestData.Feat("Spell Focus"), "Evocation");

        Assert.Contains(character.FeatProblems(augment),
            p => p.Contains("Spell Focus (Conjuration)"));

        character.AddFeat(TestData.Feat("Spell Focus"), "Conjuration");

        Assert.Empty(character.FeatProblems(augment));
    }

    // Verifies that the Greater Spell Focus feat must match a school in which the character already has Spell Focus.
    [Fact]
    public void GreaterSpellFocus_MustMatchASchoolYouHaveSpellFocusIn()
    {
        var character = Build(TestData.Wizard(), 5);
        var greater = TestData.Feat("Greater Spell Focus");
        character.AddFeat(TestData.Feat("Spell Focus"), "Evocation");

        Assert.Contains(character.FeatProblems(greater, "Conjuration"),
            p => p.Contains("Spell Focus (Conjuration)"));
        Assert.Empty(character.FeatProblems(greater, "Evocation"));

        character.AddFeat(greater, "Evocation");

        Assert.Equal(2, character.Feats.Count);
    }

    // Verifies that the Master Craftsman feat accepts ranks in either Craft or Profession.
    [Fact]
    public void MasterCraftsman_AcceptsRanksInCraftOrProfession()
    {
        var character = Build(TestData.Fighter(), 5);
        var feat = TestData.Feat("Master Craftsman");

        character.SetSkillRanks(TestData.Skill("Craft"), 4);
        Assert.Contains(character.FeatProblems(feat, "Craft"),
            p => p.Contains("Craft or Profession"));

        character.SetSkillRanks(TestData.Skill("Profession"), 5);
        Assert.Empty(character.FeatProblems(feat, "Profession"));

        character.AddFeat(feat, "Profession");

        Assert.Equal(2, character.Total("Profession"));
    }

    // ---- Class feature prerequisites ----

    // Verifies that feats with class feature prerequisites are correctly blocked if the character lacks the required class feature.
    [Theory]
    [InlineData("Extra Channel", null, "Channel Energy class feature")]
    [InlineData("Improved Channel", null, "Channel Energy class feature")]
    [InlineData("Selective Channeling", null, "Channel Energy class feature")]
    [InlineData("Alignment Channel", "Chaos", "Channel Energy class feature")]
    [InlineData("Elemental Channel", "Fire", "Channel Energy class feature")]
    [InlineData("Command Undead", null, "Channel Negative Energy class feature")]
    [InlineData("Turn Undead", null, "Channel Positive Energy class feature")]
    [InlineData("Extra Ki", null, "Ki Pool class feature")]
    [InlineData("Extra Lay On Hands", null, "Lay on Hands class feature")]
    [InlineData("Extra Mercy", "Fatigued", "Mercy class feature")]
    [InlineData("Extra Performance", null, "Bardic Performance class feature")]
    [InlineData("Extra Rage", null, "Rage class feature")]
    public void ClassFeatureFeats_NeedTheirClassFeature(string name, string? subject, string expected)
    {
        var wizard = Build(TestData.Wizard(), 1, (Ability.Charisma, 14));

        Assert.Contains(wizard.FeatProblems(TestData.Feat(name), subject),
            p => p.Contains(expected));
    }
}