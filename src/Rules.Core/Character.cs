using Rules.Core.Data;

namespace Rules.Core;

// Represents a character in the game, including base ability scores, race, and modifiers.
public class Character
{
    // The name of the character.
    public string Name { get; set; } = "";

    // The race of the character.
    public RaceDefinition? Race { get; set; }

    // The classes and levels of the character.
    public List<ClassLevel> Classes { get; } = [];

    // The base ability scores of the character.
    public Dictionary<Ability, int> BaseAbilityScores { get; } =
        Enum.GetValues<Ability>().ToDictionary(a => a, _ => 10);

    // Modifiers added directly to the character (items, spells, buffs).
    public List<Modifier> Modifiers { get; } = [];

    // Everything affecting the character, including what the race provides.
    public IEnumerable<Modifier> AllModifiers => Modifiers.Concat(RaceModifiers());

    private IEnumerable<Modifier> RaceModifiers()
    {
        if (Race is null) yield break;

        string source = $"Race: {Race.Name}";

        foreach (var (ability, value) in Race.AbilityModifiers)
            yield return new Modifier(ability.ToString(), value, BonusType.Racial, source);

        foreach (var trait in Race.Traits)
            foreach (var m in trait.Modifiers ?? [])
                yield return new Modifier(m.Stat, m.Value, m.Type, $"{source} ({trait.Name})");
    }

    public int Total(string stat) =>
        ModifierResolver.Total(AllModifiers.Where(m => m.Stat == stat));

    // Abilities
    public int AbilityScore(Ability ability) =>
        BaseAbilityScores[ability] + Total(ability.ToString());

    public int AbilityModifier(Ability ability) =>
        AbilityMath.ModifierFor(AbilityScore(ability));

    // Levels and class progression (summed across classes, as multiclassing requires)
    public int TotalLevel => Classes.Sum(c => c.Level);

    public int BaseAttackBonus =>
        Classes.Sum(c => c.Class.BaseAttackBonusAt(c.Level));

    public int BaseFortitude =>
        Classes.Sum(c => ClassDefinition.SaveAt(c.Class.Fortitude, c.Level));

    public int BaseReflex =>
        Classes.Sum(c => ClassDefinition.SaveAt(c.Class.Reflex, c.Level));

    public int BaseWill =>
        Classes.Sum(c => ClassDefinition.SaveAt(c.Class.Will, c.Level));

    public IEnumerable<ClassFeatureDefinition> ClassFeatures =>
        Classes.SelectMany(c => c.Class.Features.Where(f => f.Level <= c.Level));

    // Saves
    public int FortitudeSave =>
        BaseFortitude + AbilityModifier(Ability.Constitution) + Total("Fortitude");

    public int ReflexSave =>
        BaseReflex + AbilityModifier(Ability.Dexterity) + Total("Reflex");

    public int WillSave =>
        BaseWill + AbilityModifier(Ability.Wisdom) + Total("Will");

    // Attacks
    public int MeleeAttackBonus =>
        BaseAttackBonus + AbilityModifier(Ability.Strength) + Total("Attack");

    public int RangedAttackBonus =>
        BaseAttackBonus + AbilityModifier(Ability.Dexterity) + Total("Attack");

    public int ArmorClass =>
        10 + AbilityModifier(Ability.Dexterity) + Total("AC");
    
    // Spells
    public int SpellsPerDay(ClassLevel classLevel, int spellLevel)
    {
        var casting = classLevel.Class.Spellcasting;
        if (casting is null) return 0;

        var baseSlots = casting.BaseSpellsPerDay(classLevel.Level, spellLevel);
        if (baseSlots is null) return 0;

        // A caster needs a casting score of at least 10 + spell level.
        if (AbilityScore(casting.CastingAbility) < 10 + spellLevel) return 0;

        int bonus = SpellcastingDefinition.BonusSpells(
            AbilityModifier(casting.CastingAbility), spellLevel);

        return baseSlots.Value + bonus;
    }

    public int SpellSaveDC(ClassLevel classLevel, int spellLevel)
    {
        var casting = classLevel.Class.Spellcasting
            ?? throw new InvalidOperationException(
                $"{classLevel.Class.Name} cannot cast spells.");

        return 10 + spellLevel + AbilityModifier(casting.CastingAbility);
    }
}