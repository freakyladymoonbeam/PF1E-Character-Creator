using Rules.Core.Data;

namespace Rules.Core;

// Represents a character in the game. The class is split across several files (Character.*.cs), one per area of the rules. This file holds the basic facts.
public partial class Character
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

    // Languages the character knows, gathered from race and class features.
    public HashSet<string> Languages { get; } = new(StringComparer.OrdinalIgnoreCase);
}