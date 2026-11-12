using Rules.Core;
using Rules.Core.Data;

namespace Rules.Tests;

public class ConditionalModifierTests
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

    // ---- The condition mechanism itself ----

    [Fact]
    public void ConditionalModifier_AppliesOnlyWhileItsConditionIsOn()
    {
        var character = new Character();
        character.Modifiers.Add(
            new Modifier("AC", 4, BonusType.Dodge, "Defensive Training", "vs:giant"));

        Assert.Equal(0, character.Total("AC"));

        character.SetCondition("vs:giant", true);
        Assert.Equal(4, character.Total("AC"));
        Assert.Equal(14, character.ArmorClass);

        character.SetCondition("vs:giant", false);
        Assert.Equal(0, character.Total("AC"));
    }

    [Fact]
    public void ConditionNames_IgnoreCase()
    {
        var character = new Character();
        character.Modifiers.Add(new Modifier("AC", 2, BonusType.Dodge, "Test", "Power Attack"));

        character.SetCondition("power attack", true);

        Assert.Equal(2, character.Total("AC"));
    }

    [Fact]
    public void ConditionalBonuses_ReportWhatEachSwitchedOffConditionWouldAdd()
    {
        var character = new Character();
        character.Modifiers.Add(new Modifier("Will", 2, BonusType.Racial, "Race", "vs:enchantment"));
        character.Modifiers.Add(new Modifier("Will", 1, BonusType.Untyped, "Class", "vs:fear"));

        var bonuses = character.ConditionalBonuses("Will");
        Assert.Equal(2, bonuses["vs:enchantment"]);
        Assert.Equal(1, bonuses["vs:fear"]);

        character.SetCondition("vs:fear", true);

        var afterwards = character.ConditionalBonuses("Will");
        Assert.False(afterwards.ContainsKey("vs:fear"));
        Assert.Equal(2, afterwards["vs:enchantment"]);
    }

    [Fact]
    public void ConditionalBonuses_AccountForStacking()
    {
        var character = new Character();
        character.Modifiers.Add(new Modifier("AC", 2, BonusType.Enhancement, "Ring"));
        character.Modifiers.Add(new Modifier("AC", 4, BonusType.Enhancement, "Spell", "Shield spell"));

        Assert.Equal(2, character.Total("AC"));
        // The +4 replaces the +2 instead of adding to it, so the condition is worth +2 more.
        Assert.Equal(2, character.ConditionalBonuses("AC")["Shield spell"]);

        character.SetCondition("Shield spell", true);
        Assert.Equal(4, character.Total("AC"));
    }

    [Fact]
    public void TotalFor_ResolvesStackingAcrossStatNames()
    {
        var character = new Character();
        character.Modifiers.Add(new Modifier("Attack", 2, BonusType.Morale, "Bless"));
        character.Modifiers.Add(new Modifier("MeleeAttack", 3, BonusType.Morale, "Heroism"));
        character.Modifiers.Add(new Modifier("Attack", -1, BonusType.Untyped, "Fatigued"));
        character.Modifiers.Add(new Modifier("MeleeAttack", -2, BonusType.Untyped, "Shaken"));

        Assert.Equal(0, character.TotalFor("Attack", "MeleeAttack"));  // best morale +3, penalties -3
        Assert.Equal(1, character.Total("Attack"));                     // one stat only: +2 - 1
    }

    // ---- Scaling ----

    private static FeatDefinition Scaled(
        int value, ScalingDefinition scaling, BonusType type = BonusType.Dodge) => new(
        "test.scaled", "Scaled Test Feat", [FeatType.General], "Test feat.",
        Modifiers: [new ModifierDefinition("AC", value, type, Scaling: scaling)]);

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 2)]
    [InlineData(4, 3)]
    [InlineData(10, 6)]
    public void Scaling_CanFollowCharacterLevel(int level, int expected)
    {
        var character = Build(TestData.Wizard(), level);
        var scaling = new ScalingDefinition(ScalingSource.CharacterLevel, Step: 2);
        character.Feats.Add(new FeatChoice(Scaled(1, scaling)));

        Assert.Equal(expected, character.Total("AC"));   // 1 + floor(level / 2)
    }

    [Fact]
    public void Scaling_CanFollowBaseAttackBonus()
    {
        var scaling = new ScalingDefinition(ScalingSource.BaseAttackBonus, Step: 4);

        var fighter = Build(TestData.Fighter(), 8);      // BAB +8
        fighter.Feats.Add(new FeatChoice(Scaled(1, scaling)));
        Assert.Equal(3, fighter.Total("AC"));

        var wizard = Build(TestData.Wizard(), 8);        // BAB +4
        wizard.Feats.Add(new FeatChoice(Scaled(1, scaling)));
        Assert.Equal(2, wizard.Total("AC"));
    }

    [Fact]
    public void Scaling_CanFollowCasterLevel()
    {
        var scaling = new ScalingDefinition(ScalingSource.CasterLevel, Step: 5);

        var wizard = Build(TestData.Wizard(), 10);
        wizard.Feats.Add(new FeatChoice(Scaled(1, scaling)));
        Assert.Equal(3, wizard.Total("AC"));

        var fighter = Build(TestData.Fighter(), 10);      // no caster level at all
        fighter.Feats.Add(new FeatChoice(Scaled(1, scaling)));
        Assert.Equal(1, fighter.Total("AC"));
    }

    [Fact]
    public void Scaling_ByClassLevelOnlyCountsThatClass()
    {
        var character = new Character();
        character.Classes.Add(new ClassLevel(TestData.Fighter(), 2));
        character.Classes.Add(new ClassLevel(TestData.Wizard(), 6));
        var scaling = new ScalingDefinition(ScalingSource.ClassLevel, Step: 2, ClassName: "Wizard");
        character.Feats.Add(new FeatChoice(Scaled(1, scaling)));

        Assert.Equal(4, character.Total("AC"));   // 1 + floor(6 / 2); the fighter levels are ignored
    }

    [Fact]
    public void Scaling_CanMakeAPenaltyWorse()
    {
        var character = Build(TestData.Fighter(), 8);
        var scaling = new ScalingDefinition(ScalingSource.BaseAttackBonus, Step: 4, PerStep: -1);
        character.Feats.Add(new FeatChoice(Scaled(-1, scaling, BonusType.Untyped)));

        Assert.Equal(-3, character.Total("AC"));
    }

    // ---- Power Attack, Combat Expertise, and the ranged trades ----

    [Theory]
    [InlineData(1, -1, 2)]
    [InlineData(3, -1, 2)]
    [InlineData(4, -2, 4)]
    [InlineData(7, -2, 4)]
    [InlineData(8, -3, 6)]
    [InlineData(20, -6, 12)]
    public void PowerAttack_TradesAccuracyForDamage_ScalingWithBaseAttackBonus(
        int level, int attackPenalty, int damage)
    {
        var fighter = Build(TestData.Fighter(), level, (Ability.Strength, 13));
        fighter.AddFeat(TestData.Feat("Power Attack"));

        Assert.Equal(0, fighter.TotalFor("Attack", "MeleeAttack"));
        Assert.Equal(0, fighter.TotalFor("Damage", "MeleeDamage"));

        fighter.SetCondition("Power Attack", true);

        Assert.Equal(attackPenalty, fighter.TotalFor("Attack", "MeleeAttack"));
        Assert.Equal(damage, fighter.TotalFor("Damage", "MeleeDamage"));
    }

    [Fact]
    public void PowerAttack_LowersOnlyTheMeleeAttackBonus_AndOnlyWhileOn()
    {
        var fighter = Build(TestData.Fighter(), 1, (Ability.Strength, 13));   // BAB +1, Str +1
        Assert.Empty(fighter.AvailableConditions);

        fighter.AddFeat(TestData.Feat("Power Attack"));
        Assert.Contains("Power Attack", fighter.AvailableConditions);
        Assert.Equal(2, fighter.MeleeAttackBonus);
        Assert.Equal(1, fighter.RangedAttackBonus);   // BAB +1, Dex +0

        fighter.SetCondition("Power Attack", true);

        Assert.Equal(1, fighter.MeleeAttackBonus);
        Assert.Equal(1, fighter.RangedAttackBonus);   // untouched
    }

    [Fact]
    public void CombatExpertise_TradesAccuracyForADodgeBonus()
    {
        var fighter = Build(TestData.Fighter(), 4, (Ability.Intelligence, 13));
        fighter.AddFeat(TestData.Feat("Combat Expertise"));

        Assert.Equal(10, fighter.ArmorClass);
        Assert.Equal(4, fighter.MeleeAttackBonus);

        fighter.SetCondition("Combat Expertise", true);

        Assert.Equal(12, fighter.ArmorClass);         // +2 dodge at base attack bonus +4
        Assert.Equal(2, fighter.MeleeAttackBonus);    // -2 at base attack bonus +4
    }

    [Fact]
    public void DeadlyAim_TradesRangedAccuracyForRangedDamage()
    {
        var fighter = Build(TestData.Fighter(), 4, (Ability.Dexterity, 13));
        fighter.AddFeat(TestData.Feat("Deadly Aim"));

        Assert.Equal(5, fighter.RangedAttackBonus);   // BAB +4, Dex +1

        fighter.SetCondition("Deadly Aim", true);

        Assert.Equal(3, fighter.RangedAttackBonus);
        Assert.Equal(4, fighter.TotalFor("Damage", "RangedDamage"));
        Assert.Equal(4, fighter.MeleeAttackBonus);    // melee is untouched
    }

    [Fact]
    public void RapidShot_CostsTwoOnRangedAttacks()
    {
        var fighter = Build(TestData.Fighter(), 3, (Ability.Dexterity, 13));
        fighter.AddFeat(TestData.Feat("Point-Blank Shot"));
        fighter.AddFeat(TestData.Feat("Rapid Shot"));

        Assert.Equal(4, fighter.RangedAttackBonus);

        fighter.SetCondition("Rapid Shot", true);

        Assert.Equal(2, fighter.RangedAttackBonus);
    }

    [Fact]
    public void PointBlankShot_AppliesWithinThirtyFeet()
    {
        var fighter = Build(TestData.Fighter(), 1);
        fighter.AddFeat(TestData.Feat("Point-Blank Shot"));

        Assert.Equal(1, fighter.RangedAttackBonus);
        Assert.Equal(1,
            fighter.ConditionalBonuses("Attack", "RangedAttack")["when:target-within-30-feet"]);

        fighter.SetCondition("when:target-within-30-feet", true);

        Assert.Equal(2, fighter.RangedAttackBonus);
        Assert.Equal(1, fighter.TotalFor("Damage", "RangedDamage"));
    }

    // ---- Weapon-specific bonuses ----

    [Fact]
    public void WeaponFocus_AppliesOnlyToTheChosenWeapon()
    {
        var fighter = Build(TestData.Fighter(), 1);
        fighter.AddFeat(TestData.Feat("Weapon Focus"), "Longsword");

        Assert.Equal(1, fighter.MeleeAttackBonus);

        fighter.SetCondition("weapon:Rapier", true);
        Assert.Equal(1, fighter.MeleeAttackBonus);

        fighter.SetCondition("weapon:Longsword", true);
        Assert.Equal(2, fighter.MeleeAttackBonus);
    }

    [Fact]
    public void GreaterWeaponFocus_StacksWithWeaponFocus()
    {
        var fighter = Build(TestData.Fighter(), 8);
        fighter.AddFeat(TestData.Feat("Weapon Focus"), "Longsword");
        fighter.AddFeat(TestData.Feat("Greater Weapon Focus"), "Longsword");
        fighter.SetCondition("weapon:Longsword", true);

        Assert.Equal(10, fighter.MeleeAttackBonus);   // BAB +8, +1, +1
    }

    [Fact]
    public void WeaponSpecializationChain_AddsFourDamageWithTheChosenWeapon()
    {
        var fighter = Build(TestData.Fighter(), 12);
        foreach (var name in new[]
        {
            "Weapon Focus", "Greater Weapon Focus",
            "Weapon Specialization", "Greater Weapon Specialization"
        })
            fighter.AddFeat(TestData.Feat(name), "Longsword");

        Assert.Equal(0, fighter.Total("Damage"));

        fighter.SetCondition("weapon:Longsword", true);

        Assert.Equal(4, fighter.Total("Damage"));
    }

    // ---- Other feats ----

    [Theory]
    [InlineData(1, 1)]
    [InlineData(4, 1)]
    [InlineData(5, 2)]
    [InlineData(9, 2)]
    [InlineData(10, 3)]
    [InlineData(15, 4)]
    [InlineData(20, 5)]
    public void ArcaneStrike_GrowsEveryFiveCasterLevels(int level, int expected)
    {
        var wizard = Build(TestData.Wizard(), level);
        wizard.AddFeat(TestData.Feat("Arcane Strike"));
        wizard.SetCondition("Arcane Strike", true);

        Assert.Equal(expected, wizard.Total("Damage"));
    }

    [Fact]
    public void Mobility_AddsADodgeBonusAgainstAttacksOfOpportunityFromMovement()
    {
        var fighter = Build(TestData.Fighter(), 3, (Ability.Dexterity, 13));
        fighter.AddFeat(TestData.Feat("Dodge"));
        fighter.AddFeat(TestData.Feat("Mobility"));

        Assert.Equal(12, fighter.ArmorClass);   // 10 + 1 Dex + 1 Dodge
        Assert.Equal(4,
            fighter.ConditionalBonuses("AC")["vs:attacks-of-opportunity-from-movement"]);
    }

    [Fact]
    public void CombatCasting_IsAConditionalConcentrationBonus()
    {
        var wizard = Build(TestData.Wizard(), 1);
        wizard.AddFeat(TestData.Feat("Combat Casting"));

        Assert.Equal(0, wizard.Total("Concentration"));
        Assert.Equal(4,
            wizard.ConditionalBonuses("Concentration")["when:casting-defensively-or-grappled"]);
    }

    // ---- Class features ----

    [Theory]
    [InlineData(2, 1)]
    [InlineData(5, 1)]
    [InlineData(6, 2)]
    [InlineData(10, 3)]
    [InlineData(14, 4)]
    [InlineData(18, 5)]
    [InlineData(20, 5)]
    public void Bravery_GrowsEveryFourLevelsAfterSecond(int level, int expected)
    {
        var fighter = Build(TestData.Fighter(), level);

        Assert.Equal(expected, fighter.ConditionalBonuses("Will", "Saves")["vs:fear"]);
    }

    [Fact]
    public void Bravery_DoesNotExistAtLevelOne()
    {
        Assert.DoesNotContain("vs:fear", Build(TestData.Fighter(), 1).AvailableConditions);
    }

    [Fact]
    public void Bravery_RaisesTheWillSaveWhenFacingFear()
    {
        var fighter = Build(TestData.Fighter(), 6);   // base Will +2, base Fortitude +5

        Assert.Equal(2, fighter.WillSave);

        fighter.SetCondition("vs:fear", true);

        Assert.Equal(4, fighter.WillSave);
        Assert.Equal(5, fighter.FortitudeSave);       // Bravery only helps Will saves
    }

    // ---- Racial traits ----

    private static Character ElfWizard()
    {
        var character = new Character { Race = TestData.Elf() };
        character.Classes.Add(new ClassLevel(TestData.Wizard(), 1));
        return character;
    }

    private static Character DwarfFighter()
    {
        var character = new Character { Race = TestData.Dwarf() };
        character.Classes.Add(new ClassLevel(TestData.Fighter(), 1));
        return character;
    }

    [Fact]
    public void ElvenImmunities_AddTwoToEverySaveAgainstEnchantment()
    {
        var elf = ElfWizard();   // Fort -1 (Con 8), Ref +1 (Dex 12), Will +2

        Assert.Equal(-1, elf.FortitudeSave);
        Assert.Equal(1, elf.ReflexSave);
        Assert.Equal(2, elf.WillSave);

        elf.SetCondition("vs:enchantment", true);

        Assert.Equal(1, elf.FortitudeSave);
        Assert.Equal(3, elf.ReflexSave);
        Assert.Equal(4, elf.WillSave);
    }

    [Fact]
    public void ElvenMagic_HasItsTwoSituationalBonuses()
    {
        var elf = ElfWizard();

        Assert.Equal(2, elf.ConditionalBonuses("CasterLevelCheck")["vs:spell-resistance"]);
        Assert.Equal(2,
            elf.ConditionalBonuses("Spellcraft")["when:identifying-magic-item-properties"]);
    }

    [Fact]
    public void ElfWizard_ListsExactlyTheElfConditions()
    {
        Assert.Equal(
            new[] { "vs:enchantment", "vs:spell-resistance", "when:identifying-magic-item-properties" },
            ElfWizard().AvailableConditions);
    }

    [Fact]
    public void Dwarf_HasItsSituationalBonuses()
    {
        var dwarf = DwarfFighter();

        Assert.Equal(4, dwarf.ConditionalBonuses("AC")["vs:giant"]);

        var attack = dwarf.ConditionalBonuses("Attack");
        Assert.Equal(1, attack["vs:orc"]);
        Assert.Equal(1, attack["vs:goblinoid"]);

        var saves = dwarf.ConditionalBonuses("Saves");
        Assert.Equal(2, saves["vs:poison"]);
        Assert.Equal(2, saves["vs:spells"]);
        Assert.Equal(2, saves["vs:spell-like-abilities"]);

        Assert.Equal(4, dwarf.ConditionalBonuses("CMD")
            ["when:resisting-bull-rush-or-trip-while-standing-on-the-ground"]);
        Assert.Equal(2, dwarf.ConditionalBonuses("Perception")["when:noticing-unusual-stonework"]);
        Assert.Equal(2,
            dwarf.ConditionalBonuses("Appraise")["when:appraising-nonmagical-precious-goods"]);
    }

    [Fact]
    public void Hardy_DoesNotStackWithItself()
    {
        var dwarf = DwarfFighter();   // Con 12: Fortitude base +2, Con +1

        Assert.Equal(3, dwarf.FortitudeSave);

        dwarf.SetCondition("vs:poison", true);
        dwarf.SetCondition("vs:spells", true);   // a poison spell: both apply, but racial bonuses don't stack

        Assert.Equal(5, dwarf.FortitudeSave);
    }

    // ---- Guards for the data files ----

    private static IEnumerable<(string Owner, ModifierDefinition Modifier)> AllModifierDefinitions()
    {
        foreach (var feat in TestData.Feats())
            foreach (var m in feat.Modifiers ?? [])
                yield return (feat.Name, m);

        foreach (var cls in new[] { TestData.Fighter(), TestData.Wizard() })
            foreach (var feature in cls.Features)
                foreach (var m in feature.Modifiers ?? [])
                    yield return (feature.Name, m);

        foreach (var race in new[] { TestData.Elf(), TestData.Dwarf() })
            foreach (var trait in race.Traits)
                foreach (var m in trait.Modifiers ?? [])
                    yield return (trait.Name, m);
    }

    [Fact]
    public void EveryConditionInTheData_FollowsTheNamingConvention()
    {
        string[] prefixes = ["vs:", "when:", "weapon:"];

        foreach (var (owner, modifier) in AllModifierDefinitions())
        {
            var condition = modifier.Condition;
            if (condition is null) continue;

            // A bare name is a toggle and must match the feat, feature, or trait that owns it.
            bool prefixed = prefixes.Any(p => condition.StartsWith(p));
            Assert.True(prefixed || condition == owner,
                $"{owner}: unexpected condition '{condition}'");
        }
    }

    [Fact]
    public void EveryStatNamedInTheData_IsOneTheEngineKnowsAbout()
    {
        var known = new HashSet<string>(Enum.GetValues<Ability>().Select(a => a.ToString()))
        {
            "AC", "Attack", "MeleeAttack", "RangedAttack", "Damage", "MeleeDamage", "RangedDamage",
            "Saves", "Fortitude", "Reflex", "Will", "HP", "Initiative", "CMD",
            "CasterLevelCheck", "Concentration", "{subject}"
        };
        foreach (var skill in TestData.Skills())
            known.Add(skill.Name);

        foreach (var (owner, modifier) in AllModifierDefinitions())
            Assert.True(known.Contains(modifier.Stat), $"{owner}: unknown stat '{modifier.Stat}'");
    }
}