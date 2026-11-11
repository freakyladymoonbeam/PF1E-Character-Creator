using Rules.Core;
using Rules.Core.Data;

namespace Rules.Tests;

public class SkillTests
{
    // Base Int 15 plus the elf's +2 makes Int 17, a +3 modifier.
    private static Character ElfWizard()
    {
        var character = new Character { Race = TestData.Elf() };
        character.Classes.Add(new ClassLevel(TestData.Wizard(), 1));
        character.BaseAbilityScores[Ability.Intelligence] = 15;
        return character;
    }

    // Tests that skills are correctly loaded from the JSON test data.
    [Fact]
    public void Skills_LoadFromJson()
    {
        var skills = TestData.Skills();

        Assert.Contains(skills, s => s.Name == "Perception" && s.Ability == Ability.Wisdom);
        Assert.True(TestData.Skill("Spellcraft").TrainedOnly);
        Assert.True(TestData.Skill("Stealth").ArmorCheckPenalty);
        Assert.False(TestData.Skill("Perception").TrainedOnly);
    }

    // Tests that an Elf Wizard has the correct number of skill ranks available.
    [Fact]
    public void ElfWizard_SkillRanksAvailable()
    {
        Assert.Equal(5, ElfWizard().SkillRanksAvailable); // 2 + 3 (Int)
    }

    // Tests that skill ranks have a minimum of one per level.
    [Fact]
    public void SkillRanks_HaveAMinimumOfOnePerLevel()
    {
        var character = new Character();
        character.Classes.Add(new ClassLevel(TestData.Fighter(), 3));
        character.BaseAbilityScores[Ability.Intelligence] = 6; // -2 would zero out 2 ranks

        Assert.Equal(3, character.SkillRanksAvailable);
    }

    // Tests that a class skill gets the +3 bonus only once trained.
    [Fact]
    public void ClassSkill_GetsPlusThreeOnlyOnceTrained()
    {
        var character = ElfWizard();
        var spellcraft = TestData.Skill("Spellcraft");

        Assert.Equal(3, character.SkillBonus(spellcraft));   // Int only, no ranks yet
        Assert.False(character.CanUse(spellcraft));          // trained only

        character.SetSkillRanks(spellcraft, 1);

        Assert.Equal(7, character.SkillBonus(spellcraft));   // 1 rank + 3 class + 3 Int
        Assert.True(character.CanUse(spellcraft));
    }
  
    // Tests that the Knowledge (all) skill covers every knowledge skill for Wizards.
    [Fact]
    public void KnowledgeAll_CoversEveryKnowledgeSkillForWizards()
    {
        var wizard = ElfWizard();
        var fighter = new Character();
        fighter.Classes.Add(new ClassLevel(TestData.Fighter(), 1));

        Assert.True(wizard.IsClassSkill(TestData.Skill("Knowledge (arcana)")));
        Assert.True(wizard.IsClassSkill(TestData.Skill("Knowledge (religion)")));
        Assert.False(fighter.IsClassSkill(TestData.Skill("Knowledge (arcana)")));
        Assert.True(fighter.IsClassSkill(TestData.Skill("Knowledge (dungeoneering)")));
    }
    
    // Tests that the Elf racial trait Keen Senses applies to Perception.
    [Fact]
    public void ElfKeenSenses_AppliesToPerception()
    {
        var character = ElfWizard();
        var perception = TestData.Skill("Perception");

        Assert.Equal(2, character.SkillBonus(perception));   // +2 racial, Wis +0

        character.SetSkillRanks(perception, 1);

        Assert.Equal(3, character.SkillBonus(perception));   // not a wizard class skill
    }

    // Tests that a Fighter's Climb skill uses Strength and the class skill bonus.
    [Fact]
    public void Fighter_Climb_UsesStrengthAndClassSkillBonus()
    {
        var character = new Character();
        character.Classes.Add(new ClassLevel(TestData.Fighter(), 1));
        character.BaseAbilityScores[Ability.Strength] = 16;
        var climb = TestData.Skill("Climb");

        character.SetSkillRanks(climb, 1);

        Assert.Equal(7, character.SkillBonus(climb));        // 1 + 3 class + 3 Str
    }

    // Tests that the class skill bonus does not stack across multiple classes.
    [Fact]
    public void ClassSkillBonus_DoesNotStackAcrossClasses()
    {
        // Craft is a class skill for both Fighter and Wizard.
        var character = new Character();
        character.Classes.Add(new ClassLevel(TestData.Fighter(), 1));
        character.Classes.Add(new ClassLevel(TestData.Wizard(), 1));
        var craft = TestData.Skill("Craft");

        character.SetSkillRanks(craft, 1);

        Assert.Equal(4, character.SkillBonus(craft));        // 1 + 3, not 1 + 6
    }

    // Tests that spending skill ranks is tracked correctly.
    [Fact]
    public void SkillRanksRemaining_TracksSpending()
    {
        var character = ElfWizard();
        character.SetSkillRanks(TestData.Skill("Spellcraft"), 1);
        character.SetSkillRanks(TestData.Skill("Appraise"), 1);

        Assert.Equal(2, character.SkillRanksSpent);
        Assert.Equal(3, character.SkillRanksRemaining);
    }

    // Tests that skill ranks cannot exceed the character's level.
    [Fact]
    public void SkillRanks_CannotExceedCharacterLevel()
    {
        var character = ElfWizard();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => character.SetSkillRanks(TestData.Skill("Spellcraft"), 2));
    }
}