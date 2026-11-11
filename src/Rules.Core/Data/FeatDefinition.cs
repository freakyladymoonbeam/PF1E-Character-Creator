namespace Rules.Core.Data;

public enum FeatType { General, Combat, Critical, ItemCreation, Metamagic }

// What a feat asks you to choose when you take it (Skill Focus: a skill).
public enum FeatSubject { None, Skill, Weapon, SpellSchool }

public enum PrerequisiteType
{
    AbilityScore, BaseAttackBonus, Feat, ClassLevel,
    CasterLevel, CharacterLevel, SkillRanks, ClassFeature
}

public record PrerequisiteDefinition(
    PrerequisiteType Type,
    Ability? Ability = null,
    int? Value = null,
    string? Name = null)
{
    public string Describe() => Type switch
    {
        PrerequisiteType.AbilityScore => $"{Ability!.Value.ToString()[..3]} {Value}",
        PrerequisiteType.BaseAttackBonus => $"base attack bonus +{Value}",
        PrerequisiteType.CharacterLevel => $"character level {Value}",
        PrerequisiteType.CasterLevel => $"caster level {Value}",
        PrerequisiteType.ClassLevel => $"{Name} level {Value}",
        PrerequisiteType.SkillRanks => $"{Value} rank(s) in {Name}",
        PrerequisiteType.Feat => Name!,
        PrerequisiteType.ClassFeature => $"{Name} class feature",
        _ => Type.ToString()
    };
}

public record FeatDefinition(
    string Id,
    string Name,
    List<FeatType> Types,
    string Description,
    List<PrerequisiteDefinition>? Prerequisites = null,
    FeatSubject Subject = FeatSubject.None,
    List<ModifierDefinition>? Modifiers = null);

// A feat as taken by a character, with its choice (if any).
public record FeatChoice(FeatDefinition Feat, string? Subject = null);