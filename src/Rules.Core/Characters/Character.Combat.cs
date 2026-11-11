namespace Rules.Core;

// Saves, attacks, armor class, initiative, and hit points.
public partial class Character
{
    // Saves
    public int FortitudeSave =>
        BaseFortitude + AbilityModifier(Ability.Constitution) + Total("Fortitude");

    public int ReflexSave =>
        BaseReflex + AbilityModifier(Ability.Dexterity) + Total("Reflex");

    public int WillSave =>
        BaseWill + AbilityModifier(Ability.Wisdom) + Total("Will");

    // Attacks
    public int MeleeAttackBonus =>
        BaseAttackBonus + AbilityModifier(Ability.Strength) + Total("Attack");

    public int RangedAttackBonus =>
        BaseAttackBonus + AbilityModifier(Ability.Dexterity) + Total("Attack");

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