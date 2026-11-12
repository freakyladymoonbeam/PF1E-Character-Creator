using Rules.Core.Data;

namespace Rules.Core;

// Everything that adds to or subtracts from the character's stats.
public partial class Character
{
    // Modifiers added directly to the character (items, spells, buffs).
    public List<Modifier> Modifiers { get; } = [];

    // Everything affecting the character: direct modifiers, race, and feats.
    public IEnumerable<Modifier> AllModifiers =>
        Modifiers.Concat(RaceModifiers()).Concat(FeatModifiers());

    private IEnumerable<Modifier> RaceModifiers()
    {
        if (Race is null) yield break;

        string source = $"Race: {Race.Name}";

        foreach (var (ability, value) in Race.AbilityModifiers)
            yield return new Modifier(ability.ToString(), value, BonusType.Racial, source);

        foreach (var trait in Race.Traits)
            foreach (var m in trait.Modifiers ?? [])
                yield return ResolveModifier(m, $"{source} ({trait.Name})");
    }

    private IEnumerable<Modifier> FeatModifiers()
    {
        foreach (var choice in AllFeats)
            foreach (var m in choice.Feat.Modifiers ?? [])
                yield return ResolveModifier(m, $"Feat: {choice.Feat.Name}", choice.Subject);
    }

    // Turns a data definition into a concrete modifier for this character.
    private Modifier ResolveModifier(ModifierDefinition def, string source, string? subject = null)
    {
        string stat = def.Stat == "{subject}"
            ? subject ?? throw new InvalidOperationException($"{source} needs a choice.")
            : def.Stat;

        int value = def.Value;

        if (def.UpgradedValue is int upgraded
            && SkillRanks.GetValueOrDefault(stat) >= def.UpgradeAtRanks)
            value = upgraded;

        if (def.ExtraPerLevelBeyond is int beyond)
            value += Math.Max(0, TotalLevel - beyond);

        return new Modifier(stat, value, def.Type, source);
    }

    public int Total(string stat) =>
        ModifierResolver.Total(AllModifiers.Where(m => m.Stat == stat));
}