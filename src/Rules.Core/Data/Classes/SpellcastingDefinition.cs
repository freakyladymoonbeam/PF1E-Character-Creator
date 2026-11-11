namespace Rules.Core.Data;

// Represents the spellcasting capabilities of a character class, including the casting ability, magic type, preparation style, and spells per day.
public enum MagicType { Arcane, Divine }

// Represents the type of magic a character can cast, either Arcane or Divine.
public record SpellcastingDefinition(
    Ability CastingAbility,
    MagicType MagicType,
    bool PreparesSpells,
    List<List<int>> SpellsPerDay)
{
    // Base slots from the class table, or null if this class level
    // has no slots at all for that spell level.
    public int? BaseSpellsPerDay(int classLevel, int spellLevel)
    {
        if (classLevel < 1 || classLevel > SpellsPerDay.Count) return null;

        var row = SpellsPerDay[classLevel - 1];
        return spellLevel >= 0 && spellLevel < row.Count ? row[spellLevel] : null;
    }

    // True if a class of this level has at least one spell slot to cast with.
    public bool CanCastAt(int classLevel) =>
        classLevel >= 1
        && classLevel <= SpellsPerDay.Count
        && SpellsPerDay[classLevel - 1].Any(slots => slots > 0);

    // Bonus spells from a high casting ability. Cantrips never get any.
    public static int BonusSpells(int abilityModifier, int spellLevel) =>
        spellLevel < 1 || abilityModifier < spellLevel
            ? 0
            : (abilityModifier - spellLevel) / 4 + 1;
}