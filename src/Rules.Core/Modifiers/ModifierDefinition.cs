namespace Rules.Core.Data;

// Represents a modifier that can be applied to a character's stats, including its type, value, and any conditions for upgrading or additional bonuses per level.
public record ModifierDefinition(
    string Stat,
    int Value,
    BonusType Type,
    int? UpgradedValue = null,          // replaces Value once ranks in the stat reach UpgradeAtRanks
    int UpgradeAtRanks = 10,
    int? ExtraPerLevelBeyond = null,    // +1 for each character level beyond this number
    string? Condition = null,           // applies only while this condition is on ("Power Attack", "vs:fear", "weapon:{subject}")
    ScalingDefinition? Scaling = null); // grows with level, base attack bonus, or caster level