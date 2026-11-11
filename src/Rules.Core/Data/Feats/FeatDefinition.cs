namespace Rules.Core.Data;

public enum FeatType { General, Combat, Critical, ItemCreation, Metamagic }

// What a feat asks you to choose when you take it.
public enum FeatSubject
{
    None, Skill, Weapon, SpellSchool, AlignmentSubtype, ElementalSubtype, Mercy
}

public enum PrerequisiteType
{
    AbilityScore,
    BaseAttackBonus,
    Feat,
    ClassLevel,
    CasterLevel,
    CharacterLevel,
    SkillRanks,
    ClassFeature,
    SpellcastingType,
    FeatsOfType
}

public record PrerequisiteDefinition(
    PrerequisiteType Type,
    Ability? Ability = null,
    int? Value = null,
    string? Name = null,
    // For Feat prerequisites: a specific choice ("Conjuration"), or "{subject}"
    // meaning the same choice as the feat being taken.
    string? Subject = null)
{
    public string Describe(string? chosenSubject = null) => Type switch
    {
        PrerequisiteType.AbilityScore => $"{Ability!.Value.ToString()[..3]} {Value}",
        PrerequisiteType.BaseAttackBonus => $"base attack bonus +{Value}",
        PrerequisiteType.CharacterLevel => $"character level {Value}",
        PrerequisiteType.CasterLevel => $"caster level {Value}",
        PrerequisiteType.ClassLevel => $"{Name} level {Value}",
        PrerequisiteType.SkillRanks => $"{Value} rank(s) in {Name!.Replace("|", " or ")}",
        PrerequisiteType.Feat => DescribeFeat(chosenSubject),
        PrerequisiteType.ClassFeature => $"{Name} class feature",
        PrerequisiteType.SpellcastingType => $"ability to cast {Name!.ToLower()} spells",
        PrerequisiteType.FeatsOfType => $"any {Value} {Name} feats",
        _ => Type.ToString()
    };

    // Describes a feat prerequisite, including the chosen subject if applicable.
    private string DescribeFeat(string? chosenSubject)
    {
        string name = Name!.Replace("|", " or ");
        if (Subject is null) return name;

        string subject = Subject == "{subject}" ? chosenSubject ?? "same choice" : Subject;
        return $"{name} ({subject})";
    }
}

public record FeatDefinition(
    string Id,
    string Name,
    List<FeatType> Types,
    string Description,
    List<PrerequisiteDefinition>? Prerequisites = null,
    FeatSubject Subject = FeatSubject.None,
    List<ModifierDefinition>? Modifiers = null,
    bool Repeatable = false);   // can be taken more than once, with effects that stack

// A feat as taken by a character, with its choice (if any).
public record FeatChoice(FeatDefinition Feat, string? Subject = null);