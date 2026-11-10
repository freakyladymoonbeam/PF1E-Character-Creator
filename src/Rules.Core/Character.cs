using Rules.Core.Data;

namespace Rules.Core;

// Represents a character in the game, including base ability scores, race, and modifiers.
public class Character
{
    // The name of the character.
    public string Name { get; set; } = "";

    // The race of the character.
    public RaceDefinition? Race { get; set; }

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

    public int AbilityScore(Ability ability) =>
        BaseAbilityScores[ability] + Total(ability.ToString());

    public int AbilityModifier(Ability ability) =>
        AbilityMath.ModifierFor(AbilityScore(ability));

    public int ArmorClass =>
        10 + AbilityModifier(Ability.Dexterity) + Total("AC");
}