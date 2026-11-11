namespace Rules.Core.Data;

// Represents a modifier that can be applied to a character's stats, including its type, value, and any conditions for upgrading or additional bonuses per level.
public record ModifierDefinition(
    string Stat,
    int Value,
    BonusType Type,
    int? UpgradedValue = null,        // replaces Value once ranks in the stat reach UpgradeAtRanks
    int UpgradeAtRanks = 10,
    int? ExtraPerLevelBeyond = null); // +1 for each character level beyond this number

// Represents a trait that a race can have, including its name, description, and any associated modifiers.
public record TraitDefinition(
    string Name,
    string Description,
    List<ModifierDefinition>? Modifiers = null);

// Represents the definition of a race, including its ID, name, size, base speed, ability modifiers, traits, and languages.
public record RaceDefinition(
    string Id,
    string Name,
    Size Size,
    int BaseSpeed,
    Dictionary<Ability, int> AbilityModifiers,
    List<TraitDefinition> Traits,
    List<string> Languages,
    List<string> BonusLanguages);