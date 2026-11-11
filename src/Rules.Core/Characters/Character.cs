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
        foreach (var choice in Feats)
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

    // Hit points
    public int MaxHitPoints
    {
        get
        {
            int conModifier = AbilityModifier(Ability.Constitution);
            int total = 0;

            for (int c = 0; c < Classes.Count; c++)
            {
                for (int level = 1; level <= Classes[c].Level; level++)
                {
                    // Only the very first level of the first class gets maximum HP.
                    bool isCharacterLevelOne = c == 0 && level == 1;
                    int roll = isCharacterLevelOne
                        ? Classes[c].Class.HitDie
                        : Classes[c].HitPointRoll(level);

                    total += Math.Max(1, roll + conModifier);
                }
            }

            return total + Total("HP");
        }
    }

    // Skills
    public Dictionary<string, int> SkillRanks { get; } = [];

    public int RanksIn(SkillDefinition skill) =>
        SkillRanks.GetValueOrDefault(skill.Name);

    public bool IsClassSkill(SkillDefinition skill) =>
        Classes.Any(c => c.Class.IsClassSkill(skill.Name));

    public bool CanUse(SkillDefinition skill) =>
        !skill.TrainedOnly || RanksIn(skill) > 0;

    public int SkillBonus(SkillDefinition skill)
    {
        int ranks = RanksIn(skill);
        int classSkillBonus = ranks > 0 && IsClassSkill(skill) ? 3 : 0;

        return ranks + classSkillBonus
            + AbilityModifier(skill.Ability)
            + Total(skill.Name);
    }

    // Each level gives class ranks + Int modifier, with a minimum of 1 per level.
    public int SkillRanksAvailable
    {
        get
        {
            int intModifier = AbilityModifier(Ability.Intelligence);
            return Classes.Sum(c => c.Level * Math.Max(1, c.Class.SkillRanksPerLevel + intModifier));
        }
    }

    public int SkillRanksSpent => SkillRanks.Values.Sum();

    public int SkillRanksRemaining => SkillRanksAvailable - SkillRanksSpent;

    public void SetSkillRanks(SkillDefinition skill, int ranks)
    {
        // You can't have more ranks in a skill than your total level.
        if (ranks < 0 || ranks > TotalLevel)
            throw new ArgumentOutOfRangeException(nameof(ranks),
                $"{skill.Name} ranks must be between 0 and {TotalLevel}.");

        SkillRanks[skill.Name] = ranks;
    }

    // Feats
    public List<FeatChoice> Feats { get; } = [];

    // One feat at level 1, then one at every odd level.
    // (Class bonus feats and the Human bonus feat will be tracked separately.)
    public int FeatSlotsAvailable => (TotalLevel + 1) / 2;

    public int FeatSlotsRemaining => FeatSlotsAvailable - Feats.Count;

    // Highest caster level among the character's spellcasting classes.
    public int CasterLevel => Classes
        .Where(c => c.Class.Spellcasting is not null)
        .Select(c => c.Level)
        .DefaultIfEmpty(0)
        .Max();

    public int Initiative =>
        AbilityModifier(Ability.Dexterity) + Total("Initiative");

    public bool MeetsPrerequisite(PrerequisiteDefinition p, string? chosenSubject = null)
    {
        int value = p.Value ?? 0;

        return p.Type switch
        {
            PrerequisiteType.AbilityScore => AbilityScore(p.Ability!.Value) >= value,
            PrerequisiteType.BaseAttackBonus => BaseAttackBonus >= value,
            PrerequisiteType.CharacterLevel => TotalLevel >= value,
            PrerequisiteType.CasterLevel => CasterLevel >= value,
            PrerequisiteType.ClassLevel =>
                Classes.Any(c => c.Class.Name == p.Name && c.Level >= value),
            // "Craft|Profession" means ranks in any one of the listed skills.
            PrerequisiteType.SkillRanks =>
                p.Name!.Split('|').Any(n => SkillRanks.GetValueOrDefault(n) >= value),
            PrerequisiteType.Feat => Feats.Any(f =>
                f.Feat.Name == p.Name && SubjectMatches(p.Subject, f.Subject, chosenSubject)),
            PrerequisiteType.ClassFeature => ClassFeatures.Any(f => f.Name == p.Name),
            _ => throw new InvalidOperationException($"Unknown prerequisite type {p.Type}.")
        };
    }

    private static bool SubjectMatches(string? required, string? actual, string? chosen)
    {
        if (required is null) return true;

        string wanted = required == "{subject}" ? chosen ?? "" : required;
        return string.Equals(actual, wanted, StringComparison.OrdinalIgnoreCase);
    }

    public IEnumerable<PrerequisiteDefinition> UnmetPrerequisites(
        FeatDefinition feat, string? subject = null) =>
        (feat.Prerequisites ?? []).Where(p => !MeetsPrerequisite(p, subject));

    // Everything stopping this character from taking the feat. Empty means it's allowed.
    public List<string> FeatProblems(FeatDefinition feat, string? subject = null)
    {
        var problems = new List<string>();

        if (feat.Subject != FeatSubject.None && subject is null)
            problems.Add($"{feat.Name} needs a choice of {feat.Subject.ToString().ToLower()}.");

        if (feat.Subject == FeatSubject.None && subject is not null)
            problems.Add($"{feat.Name} does not take a choice.");

        bool alreadyTaken = Feats.Any(f => f.Feat.Id == feat.Id
            && string.Equals(f.Subject, subject, StringComparison.OrdinalIgnoreCase));
        bool duplicateAllowed = feat.Repeatable && feat.Subject == FeatSubject.None;

        if (alreadyTaken && !duplicateAllowed)
            problems.Add($"Already has {feat.Name}.");

        if (FeatSlotsRemaining <= 0)
            problems.Add("No feat slots remaining.");

        problems.AddRange(
            UnmetPrerequisites(feat, subject).Select(p => $"Requires {p.Describe(subject)}."));

        return problems;
    }

    public void AddFeat(FeatDefinition feat, string? subject = null)
    {
        var problems = FeatProblems(feat, subject);
        if (problems.Count > 0)
            throw new InvalidOperationException(
                $"Cannot take {feat.Name}: {string.Join(" ", problems)}");

        Feats.Add(new FeatChoice(feat, subject));
    }
}