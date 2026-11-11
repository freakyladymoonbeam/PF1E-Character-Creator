using Rules.Core.Data;

namespace Rules.Core;

// Spells per day and spell save DCs.
public partial class Character
{
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