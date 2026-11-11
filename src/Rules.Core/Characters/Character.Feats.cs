using Rules.Core.Data;

namespace Rules.Core;

// Feat slots, prerequisites, and taking feats.
public partial class Character
{
    public List<FeatChoice> Feats { get; } = [];

    // One feat at level 1, then one at every odd level.
    // (Class bonus feats and the Human bonus feat will be tracked separately.)
    public int FeatSlotsAvailable => (TotalLevel + 1) / 2;

    public int FeatSlotsRemaining => FeatSlotsAvailable - Feats.Count;

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
                Feats.Where(f => f.Feat.Types.Contains(Enum.Parse<FeatType>(p.Name!)))
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

        return Feats.Any(f => names.Contains(f.Feat.Name)
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