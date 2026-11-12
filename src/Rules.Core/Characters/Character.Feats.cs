using Rules.Core.Data;

namespace Rules.Core;

// Feat slots, bonus feats, prerequisites, and taking feats.
public partial class Character
{
    // Feats taken in ordinary feat slots.
    public List<FeatChoice> Feats { get; } = [];

    // Feats taken in class bonus feat slots (each one's Source is the class that gave the slot).
    public List<FeatChoice> BonusFeats { get; } = [];

    // Every feat the character has taken, from either kind of slot.
    public IEnumerable<FeatChoice> AllFeats => Feats.Concat(BonusFeats);

    // One feat at level 1, then one at every odd level.
    // (Class bonus feats are tracked separately; the Human bonus feat will come with races.)
    public int FeatSlotsAvailable => (TotalLevel + 1) / 2;

    public int FeatSlotsRemaining => FeatSlotsAvailable - Feats.Count;

    // Bonus feats a class has earned by its level, and how many are still unspent.
    public int BonusFeatSlotsAvailable(ClassLevel classLevel) =>
        classLevel.Class.BonusFeatSlotsAt(classLevel.Level);

    public int BonusFeatSlotsRemaining(ClassLevel classLevel) =>
        BonusFeatSlotsAvailable(classLevel)
        - BonusFeats.Count(f => f.Source == classLevel.Class.Name);

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
            PrerequisiteType.Feat => HasFeat(p, chosenSubject),
            PrerequisiteType.ClassFeature => ClassFeatures.Any(f => f.Name == p.Name),
            PrerequisiteType.SpellcastingType => Classes.Any(c =>
                c.Class.Spellcasting is { } casting
                && casting.MagicType == Enum.Parse<MagicType>(p.Name!)
                && casting.CanCastAt(c.Level)),
            // "Critical" counts how many different feats of that type the character has.
            PrerequisiteType.FeatsOfType =>
                AllFeats.Where(f => f.Feat.Types.Contains(Enum.Parse<FeatType>(p.Name!)))
                        .Select(f => f.Feat.Id)
                        .Distinct()
                        .Count() >= value,
            _ => throw new InvalidOperationException($"Unknown prerequisite type {p.Type}.")
        };
    }

    // Feats the character's classes grant automatically (such as a fighter's armor proficiencies).
    public IEnumerable<string> GrantedFeatNames =>
        Classes.SelectMany(c => c.Class.GrantedFeats ?? new List<string>());

    // True if the character has the feat, whether taken or granted by a class.
    // "A|B" in a prerequisite means either feat will do.
    private bool HasFeat(PrerequisiteDefinition p, string? chosenSubject)
    {
        string[] names = p.Name!.Split('|');

        // Feats a class grants have no choice attached.
        if (p.Subject is null && GrantedFeatNames.Any(g => names.Contains(g)))
            return true;

        return AllFeats.Any(f => names.Contains(f.Feat.Name)
            && SubjectMatches(p.Subject, f.Subject, chosenSubject));
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

    // Checks shared by both kinds of slot: the choice, duplicates, and prerequisites.
    private List<string> EligibilityProblems(FeatDefinition feat, string? subject)
    {
        var problems = new List<string>();

        if (feat.Subject != FeatSubject.None && subject is null)
            problems.Add($"{feat.Name} needs a choice of {feat.Subject.ToString().ToLower()}.");

        if (feat.Subject == FeatSubject.None && subject is not null)
            problems.Add($"{feat.Name} does not take a choice.");

        // A feat a class already grants can't be taken again, nor can one already taken.
        bool grantedByClass = feat.Subject == FeatSubject.None
            && GrantedFeatNames.Contains(feat.Name);
        bool alreadyTaken = AllFeats.Any(f => f.Feat.Id == feat.Id
            && string.Equals(f.Subject, subject, StringComparison.OrdinalIgnoreCase));
        bool repeatable = feat.Repeatable && feat.Subject == FeatSubject.None;

        if (!repeatable && (grantedByClass || alreadyTaken))
            problems.Add($"Already has {feat.Name}.");

        problems.AddRange(
            UnmetPrerequisites(feat, subject).Select(p => $"Requires {p.Describe(subject)}."));

        return problems;
    }

    // Everything stopping this character from taking the feat in an ordinary slot. Empty means it's allowed.
    public List<string> FeatProblems(FeatDefinition feat, string? subject = null)
    {
        var problems = new List<string>();

        if (FeatSlotsRemaining <= 0)
            problems.Add("No feat slots remaining.");

        problems.AddRange(EligibilityProblems(feat, subject));
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

    // Everything stopping this character from taking the feat in one of a class's bonus slots.
    public List<string> BonusFeatProblems(
        ClassLevel classLevel, FeatDefinition feat, string? subject = null)
    {
        var problems = new List<string>();

        if (!Classes.Contains(classLevel))
            problems.Add("That class level doesn't belong to this character.");

        var rule = classLevel.Class.BonusFeats;
        if (rule is null)
        {
            problems.Add($"{classLevel.Class.Name} has no bonus feats.");
        }
        else
        {
            if (BonusFeatSlotsRemaining(classLevel) <= 0)
                problems.Add($"No {classLevel.Class.Name} bonus feats remaining.");

            if (!rule.Allows(feat))
                problems.Add(
                    $"{feat.Name} is not one of the feats a {classLevel.Class.Name} can take as a bonus feat.");
        }

        problems.AddRange(EligibilityProblems(feat, subject));
        return problems;
    }

    public void AddBonusFeat(ClassLevel classLevel, FeatDefinition feat, string? subject = null)
    {
        var problems = BonusFeatProblems(classLevel, feat, subject);
        if (problems.Count > 0)
            throw new InvalidOperationException(
                $"Cannot take {feat.Name} as a bonus feat: {string.Join(" ", problems)}");

        BonusFeats.Add(new FeatChoice(feat, subject, classLevel.Class.Name));
    }
}