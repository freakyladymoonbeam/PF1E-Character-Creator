namespace Rules.Core.Data;

// Represents the definition of a skill, including its name, associated ability, and whether it requires training or is affected by armor check penalties.
public record SkillDefinition(
    string Name,
    Ability Ability,
    bool TrainedOnly,
    bool ArmorCheckPenalty);