namespace Rules.Core;

// Ability scores and their modifiers.
public partial class Character
{
    public int AbilityScore(Ability ability) =>
        BaseAbilityScores[ability] + Total(ability.ToString());

    public int AbilityModifier(Ability ability) =>
        AbilityMath.ModifierFor(AbilityScore(ability));
}