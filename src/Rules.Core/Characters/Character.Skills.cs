using Rules.Core.Data;

namespace Rules.Core;

// Skill ranks, class skills, and skill bonuses.
public partial class Character
{
    public Dictionary<string, int> SkillRanks { get; } = [];

    public int RanksIn(SkillDefinition skill) =>
        SkillRanks.GetValueOrDefault(skill.Name);

    public bool IsClassSkill(SkillDefinition skill) =>
        Classes.Any(c => c.Class.IsClassSkill(skill.Name));

    public bool CanUse(SkillDefinition skill) =>
        !skill.TrainedOnly || RanksIn(skill) > 0;

    public int SkillBonus(SkillDefinition skill)
    {
        int ranks = RanksIn(skill);
        int classSkillBonus = ranks > 0 && IsClassSkill(skill) ? 3 : 0;

        return ranks + classSkillBonus
            + AbilityModifier(skill.Ability)
            + Total(skill.Name);
    }

    // Each level gives class ranks + Int modifier, with a minimum of 1 per level.
    public int SkillRanksAvailable
    {
        get
        {
            int intModifier = AbilityModifier(Ability.Intelligence);
            return Classes.Sum(c => c.Level * Math.Max(1, c.Class.SkillRanksPerLevel + intModifier));
        }
    }

    public int SkillRanksSpent => SkillRanks.Values.Sum();

    public int SkillRanksRemaining => SkillRanksAvailable - SkillRanksSpent;

    public void SetSkillRanks(SkillDefinition skill, int ranks)
    {
        // You can't have more ranks in a skill than your total level.
        if (ranks < 0 || ranks > TotalLevel)
            throw new ArgumentOutOfRangeException(nameof(ranks),
                $"{skill.Name} ranks must be between 0 and {TotalLevel}.");

        SkillRanks[skill.Name] = ranks;
    }
}