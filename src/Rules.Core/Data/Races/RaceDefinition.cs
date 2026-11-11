namespace Rules.Core.Data;

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