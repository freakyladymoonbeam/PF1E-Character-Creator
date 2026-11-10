namespace Rules.Core;

// Represents the six core abilities in Pathfinder 1e.
public enum Ability
{
    Strength,
    Dexterity,
    Constitution,
    Intelligence,
    Wisdom,
    Charisma
}

public static class AbilityMath
{
    // PF1e: (score - 10) / 2, rounded down. 9 gives -1, 7 gives -2, 12 gives +1, etc.
    public static int ModifierFor(int score) => (int)Math.Floor((score - 10) / 2.0);
}