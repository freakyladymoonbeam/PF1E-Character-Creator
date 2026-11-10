namespace Rules.Core;

// Represents the different types of bonuses that can be applied to a character's stats.
public enum BonusType
{
    Untyped,
    Alchemical,
    Armor,
    Circumstance,
    Competence,
    Deflection,
    Dodge,
    Enhancement,
    Inherent,
    Insight,
    Luck,
    Morale,
    NaturalArmor,
    Profane,
    Racial,
    Resistance,
    Sacred,
    Shield,
    Size
}

// Represents a modifier to a character's stat, including its value, type, and source.
public record Modifier(string Stat, int Value, BonusType Type, string Source);