namespace Rules.Core.Data;

// What a scaling value grows with. (Ability scores are deliberately not a source: they depend on modifiers themselves.)
public enum ScalingSource { CharacterLevel, BaseAttackBonus, CasterLevel, ClassLevel }

// Makes a modifier's value grow: Value + PerStep * floor((source - Start) / Step), never fewer than 0 steps.
public record ScalingDefinition(
    ScalingSource Source,
    int Step = 1,
    int PerStep = 1,
    int Start = 0,
    string? ClassName = null);   // for ClassLevel: which class's levels to count