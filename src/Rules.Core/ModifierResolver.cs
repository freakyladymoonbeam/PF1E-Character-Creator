namespace Rules.Core;

// Resolves the total value of modifiers, taking into account stacking rules.
public static class ModifierResolver
{
    // The types of bonuses that can stack with each other.
    private static readonly HashSet<BonusType> StackingTypes =
        [BonusType.Untyped, BonusType.Dodge, BonusType.Circumstance];

    // Calculates the total value of the given modifiers, considering stacking rules.
    public static int Total(IEnumerable<Modifier> modifiers)
    {
        var all = modifiers.ToList();

        int penalties = all.Where(m => m.Value < 0).Sum(m => m.Value);
        var bonuses = all.Where(m => m.Value > 0).ToList();

        int stacking = bonuses
            .Where(m => StackingTypes.Contains(m.Type))
            .Sum(m => m.Value);

        int nonStacking = bonuses
            .Where(m => !StackingTypes.Contains(m.Type))
            .GroupBy(m => m.Type)
            .Sum(g => g.Max(m => m.Value));

        return stacking + nonStacking + penalties;
    }
}