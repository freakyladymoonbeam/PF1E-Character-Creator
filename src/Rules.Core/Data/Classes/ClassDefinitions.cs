using System.Text.Json.Serialization;

namespace Rules.Core.Data;

// Defines the progression of base attack bonus for a character class.
public enum BabProgression { Full, ThreeQuarters, Half }
// Defines the progression of saving throws for a character class.
public enum SaveProgression { Good, Poor }

// Defines a class feature, including the level at which it is gained, its name, and description.
public record ClassFeatureDefinition(int Level, string Name, string Description);

// Defines a character class, including its hit die, skill ranks, base attack bonus progression, saving throw progressions, class skills, proficiencies, and features.
public record ClassDefinition(
    string Id,
    string Name,
    [property: JsonPropertyName("alignment")] List<Alignment> AllowedAlignments,
    int HitDie,
    int SkillRanksPerLevel,
    BabProgression BabProgression,
    SaveProgression Fortitude,
    SaveProgression Reflex,
    SaveProgression Will,
    List<string> ClassSkills,
    List<string> WeaponProficiencies,
    List<string> ArmorProficiencies,
    StartingWealth StartingWealth,
    List<ClassFeatureDefinition> Features,
    SpellcastingDefinition? Spellcasting = null)
{
    // Checks if the class allows a specific alignment.
    public bool AllowsAlignment(Alignment alignment) =>
        AllowedAlignments.Contains(alignment);
    
    // Fixed (average) roll per level: d6 gives 4, d8 gives 5, d10 gives 6, d12 gives 7.
    public int AverageHitPointRoll => HitDie / 2 + 1;

    // "Knowledge (all)" in a class skill list covers every Knowledge skill.
    public bool IsClassSkill(string skillName) =>
        ClassSkills.Contains(skillName)
        || (skillName.StartsWith("Knowledge (") && ClassSkills.Contains("Knowledge (all)"));

    // Calculates the base attack bonus for a given level based on the class's progression.
    public int BaseAttackBonusAt(int level) => level <= 0 ? 0 : BabProgression switch
    {
        BabProgression.Full => level,
        BabProgression.ThreeQuarters => level * 3 / 4,
        BabProgression.Half => level / 2,
        _ => throw new InvalidOperationException($"Unknown progression {BabProgression}.")
    };

    // Calculates the saving throw for a given level based on the class's progression.
    public static int SaveAt(SaveProgression progression, int level) =>
        level <= 0 ? 0 : progression == SaveProgression.Good ? 2 + level / 2 : level / 3;
}

// Represents a character's level in a specific class, including the class definition and the level attained.
public record ClassLevel(ClassDefinition Class, int Level, List<int>? HitPointRolls = null)
{
    // Roll for one level of this class (1-based). Uses the average if none was recorded.
    public int HitPointRoll(int level)
    {
        if (HitPointRolls is null || level > HitPointRolls.Count)
            return Class.AverageHitPointRoll;

        int roll = HitPointRolls[level - 1];
        if (roll < 1 || roll > Class.HitDie)
            throw new ArgumentOutOfRangeException(nameof(level),
                $"Recorded roll {roll} is not valid for a d{Class.HitDie}.");

        return roll;
    }
}