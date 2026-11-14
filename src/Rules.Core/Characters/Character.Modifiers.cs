using Rules.Core.Data;

namespace Rules.Core;

// Everything that adds to or subtracts from the character's stats.
public partial class Character
{
    // Modifiers added directly to the character (items, spells, buffs).
    public List<Modifier> Modifiers { get; } = [];

    // Conditions the player has switched on ("Power Attack", "vs:fear", "weapon:Longsword").
    public HashSet<string> ActiveConditions { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void SetCondition(string condition, bool on)
    {
        if (on) ActiveConditions.Add(condition);
        else ActiveConditions.Remove(condition);
    }

    // Everything affecting the character, whether or not its condition is currently on.
    public IEnumerable<Modifier> AllModifiers =>
        Modifiers.Concat(RaceModifiers()).Concat(ClassFeatureModifiers()).Concat(FeatModifiers()).Concat(FamiliarModifiers());

    // The modifiers that apply right now: unconditional ones, plus conditional ones that are switched on.
    public IEnumerable<Modifier> ActiveModifiers => AllModifiers.Where(IsActive);

    private bool IsActive(Modifier m) =>
        m.Condition is null || ActiveConditions.Contains(m.Condition);

    // Every condition the character's modifiers depend on (for listing toggles on the sheet).
    public IEnumerable<string> AvailableConditions => AllModifiers
        .Where(m => m.Condition is not null)
        .Select(m => m.Condition!)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(c => c, StringComparer.OrdinalIgnoreCase);

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

    private IEnumerable<Modifier> ClassFeatureModifiers()
    {
        foreach (var feature in ClassFeatures)
            foreach (var m in feature.Modifiers ?? [])
                yield return ResolveModifier(m, $"Class feature: {feature.Name}");
    }

    private IEnumerable<Modifier> FeatModifiers()
    {
        foreach (var choice in AllFeats)
            foreach (var m in choice.Feat.Modifiers ?? [])
                yield return ResolveModifier(m, $"Feat: {choice.Feat.Name}", choice.Subject);
    }

    // Modifiers granted by the character's familiar, if any.
    private IEnumerable<Modifier> FamiliarModifiers()
    {
        if (FamiliarType is null) yield break;

        string source = $"Familiar: {FamiliarType}";

        // Familiar type bonus (skill or save bonus).
        if (FamiliarBonus is { } bonus)
            yield return new Modifier(bonus.Stat, bonus.Value, BonusType.Untyped, source);

        // Familiar abilities scale with master level (levels in classes that grant familiars stack).
        int masterLevel = Classes.Where(c => c.Class.GrantsFamiliar).Sum(c => c.Level);

        // 1st-2nd level: Alertness (+2 Perception, +2 Sense Motive).
        if (masterLevel >= 1)
        {
            yield return new Modifier("Perception", 2, BonusType.Untyped, source);
            yield return new Modifier("Sense Motive", 2, BonusType.Untyped, source);
        }

        // 5th-6th: Speak with master (no mechanical modifier needed for the character sheet).
        // 7th-8th: Speak with animals of its kind (familiar ability, not master).
        // 11th-12th: Spell resistance (familiar ability, not master).
        // 13th-14th: Scry on familiar (familiar ability, not master).
        // Deliver touch spells (3rd-4th) is a familiar ability, not a master modifier.
    }

    // Turns a data definition into a concrete modifier for this character.
    private Modifier ResolveModifier(ModifierDefinition def, string source, string? subject = null)
    {
        string stat = FillInChoice(def.Stat, source, subject);
        string? condition = def.Condition is null ? null : FillInChoice(def.Condition, source, subject);

        int value = def.Value;

        if (def.UpgradedValue is int upgraded
            && SkillRanks.GetValueOrDefault(stat) >= def.UpgradeAtRanks)
            value = upgraded;

        if (def.ExtraPerLevelBeyond is int beyond)
            value += Math.Max(0, TotalLevel - beyond);

        // Grows with level, base attack bonus, or caster level.
        if (def.Scaling is { } scaling)
        {
            int steps = Math.Max(0,
                (ScalingSourceValue(scaling) - scaling.Start) / Math.Max(1, scaling.Step));
            value += scaling.PerStep * steps;
        }

        return new Modifier(stat, value, def.Type, source, condition);
    }

    // Replaces "{subject}" with the player's choice (Skill Focus's skill, Weapon Focus's weapon).
    private static string FillInChoice(string text, string source, string? subject) =>
        text.Contains("{subject}")
            ? text.Replace("{subject}",
                subject ?? throw new InvalidOperationException($"{source} needs a choice."))
            : text;

    private int ScalingSourceValue(ScalingDefinition scaling) => scaling.Source switch
    {
        ScalingSource.CharacterLevel => TotalLevel,
        ScalingSource.BaseAttackBonus => BaseAttackBonus,
        ScalingSource.CasterLevel => CasterLevel,
        ScalingSource.ClassLevel =>
            Classes.Where(c => c.Class.Name == scaling.ClassName).Sum(c => c.Level),
        _ => throw new InvalidOperationException($"Unknown scaling source {scaling.Source}.")
    };

    // The total bonus to a stat, counting unconditional modifiers and conditions that are switched on.
    public int Total(string stat) => TotalFor(stat);

    // Like Total, but resolves stacking across several stat names at once
    // (for example "Attack" and "MeleeAttack", so two same-type bonuses don't both count).
    public int TotalFor(params string[] stats) =>
        ModifierResolver.Total(ActiveModifiers.Where(m => stats.Contains(m.Stat)));

    // What each switched-off condition would add to the stat(s), for sheet annotations like "+2 vs. enchantment".
    public Dictionary<string, int> ConditionalBonuses(params string[] stats)
    {
        var relevant = AllModifiers.Where(m => stats.Contains(m.Stat)).ToList();
        int current = ModifierResolver.Total(relevant.Where(IsActive));

        return relevant
            .Where(m => m.Condition is not null && !IsActive(m))
            .Select(m => m.Condition!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                condition => condition,
                condition => ModifierResolver.Total(relevant.Where(m =>
                    IsActive(m)
                    || string.Equals(m.Condition, condition, StringComparison.OrdinalIgnoreCase)))
                    - current,
                StringComparer.OrdinalIgnoreCase);
    }
}