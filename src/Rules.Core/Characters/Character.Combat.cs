namespace Rules.Core;

// Saves, attacks, armor class, initiative, and hit points.
public partial class Character
{
    // Saves ("Saves" bonuses apply to all three)
    public int FortitudeSave =>
        BaseFortitude + AbilityModifier(Ability.Constitution) + TotalFor("Fortitude", "Saves");

    public int ReflexSave =>
        BaseReflex + AbilityModifier(Ability.Dexterity) + TotalFor("Reflex", "Saves");

    public int WillSave =>
        BaseWill + AbilityModifier(Ability.Wisdom) + TotalFor("Will", "Saves");

    // Attacks ("Attack" bonuses apply to both melee and ranged)
    public int MeleeAttackBonus =>
        BaseAttackBonus + AbilityModifier(Ability.Strength) + TotalFor("Attack", "MeleeAttack");

    public int RangedAttackBonus =>
        BaseAttackBonus + AbilityModifier(Ability.Dexterity) + TotalFor("Attack", "RangedAttack");

    // Defense
    public int ArmorClass =>
        10 + AbilityModifier(Ability.Dexterity) + Total("AC");

    public int Initiative =>
        AbilityModifier(Ability.Dexterity) + Total("Initiative");

    // Hit points
    public int MaxHitPoints
    {
        get
        {
            int conModifier = AbilityModifier(Ability.Constitution);
            int total = 0;

            for (int c = 0; c < Classes.Count; c++)
            {
                for (int level = 1; level <= Classes[c].Level; level++)
                {
                    // Only the very first level of the first class gets maximum HP.
                    bool isCharacterLevelOne = c == 0 && level == 1;
                    int roll = isCharacterLevelOne
                        ? Classes[c].Class.HitDie
                        : Classes[c].HitPointRoll(level);

                    total += Math.Max(1, roll + conModifier);
                }
            }

            return total + Total("HP");
        }
    }
}