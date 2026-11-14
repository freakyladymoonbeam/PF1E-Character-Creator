using Rules.Core.Data;

namespace Rules.Core;

// Familiar selection and bonuses granted to the master.
public partial class Character
{
    // The type of familiar the character has chosen (null if none).
    public string? FamiliarType { get; set; }

    // Familiar type -> (stat bonus, value) granted to the master.
    private static readonly Dictionary<string, (string Stat, int Value)> FamiliarTypeBonuses = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Bat"] = ("Fly", 3),
        ["Cat"] = ("Stealth", 3),
        ["Hawk"] = ("Perception", 3),
        ["Lizard"] = ("Climb", 3),
        ["Monkey"] = ("Acrobatics", 3),
        ["Owl"] = ("Perception", 3),
        ["Rat"] = ("Fortitude", 2),
        ["Raven"] = ("Appraise", 3),
        ["Viper"] = ("Bluff", 3),
        ["Toad"] = ("HP", 3),
        ["Weasel"] = ("Reflex", 2),
    };

    // The bonus granted by the character's familiar type, if any.
    public (string Stat, int Value)? FamiliarBonus =>
        FamiliarType is not null && FamiliarTypeBonuses.TryGetValue(FamiliarType, out var bonus)
            ? bonus
            : null;

    // The list of valid familiar types.
    public static IEnumerable<string> FamiliarTypes => FamiliarTypeBonuses.Keys;
}
