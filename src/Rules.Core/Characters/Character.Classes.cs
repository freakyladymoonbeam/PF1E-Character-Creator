using Rules.Core.Data;

namespace Rules.Core;

// Levels and class progression (summed across classes, as multiclassing requires).
public partial class Character
{
    public int TotalLevel => Classes.Sum(c => c.Level);

    public int BaseAttackBonus =>
        Classes.Sum(c => c.Class.BaseAttackBonusAt(c.Level));

    public int BaseFortitude =>
        Classes.Sum(c => ClassDefinition.SaveAt(c.Class.Fortitude, c.Level));

    public int BaseReflex =>
        Classes.Sum(c => ClassDefinition.SaveAt(c.Class.Reflex, c.Level));

    public int BaseWill =>
        Classes.Sum(c => ClassDefinition.SaveAt(c.Class.Will, c.Level));

    public IEnumerable<ClassFeatureDefinition> ClassFeatures =>
        Classes.SelectMany(c => c.Class.Features.Where(f => f.Level <= c.Level));

    // Highest caster level among the character's spellcasting classes.
    public int CasterLevel => Classes
        .Where(c => c.Class.Spellcasting is not null)
        .Select(c => c.Level)
        .DefaultIfEmpty(0)
        .Max();
}