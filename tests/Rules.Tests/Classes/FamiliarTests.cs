using Rules.Core;
using Rules.Core.Data;

namespace Rules.Tests;

public class FamiliarTests
{
    private static ClassDefinition LoadWizard() => DataPackLoader.LoadClass(
        Path.Combine(AppContext.BaseDirectory, "data", "core", "classes", "wizard.json"));

    private static (Character Character, ClassLevel Wizard) WizardOfLevel(int level, int intelligence)
    {
        var wizard = new ClassLevel(LoadWizard(), level);
        var character = new Character();
        character.Classes.Add(wizard);
        character.BaseAbilityScores[Ability.Intelligence] = intelligence;
        return (character, wizard);
    }

    // Tests that a wizard with no familiar has no familiar bonuses.
    [Fact]
    public void NoFamiliar_HasNoFamiliarBonuses()
    {
        var (character, _) = WizardOfLevel(1, 10);

        Assert.Null(character.FamiliarType);
        Assert.Equal(0, character.Total("Stealth"));
        Assert.Equal(0, character.Total("Fortitude"));
        Assert.Equal(0, character.Total("HP"));
        Assert.Equal(0, character.Total("Perception"));
    }

    // Tests that a cat familiar grants +3 Stealth to the master.
    [Fact]
    public void CatFamiliar_GrantsStealthBonus()
    {
        var (character, _) = WizardOfLevel(1, 10);
        character.FamiliarType = "Cat";

        Assert.Equal(3, character.Total("Stealth"));
    }

    // Tests that a rat familiar grants +2 Fortitude to the master.
    [Fact]
    public void RatFamiliar_GrantsFortitudeBonus()
    {
        var (character, _) = WizardOfLevel(1, 10);
        character.FamiliarType = "Rat";

        Assert.Equal(2, character.Total("Fortitude"));
    }

    // Tests that a toad familiar grants +3 HP to the master.
    [Fact]
    public void ToadFamiliar_GrantsHPBonus()
    {
        var (character, _) = WizardOfLevel(1, 10);
        character.FamiliarType = "Toad";

        Assert.Equal(3, character.Total("HP"));
    }

    // Tests that a bat familiar grants +3 Fly to the master.
    [Fact]
    public void BatFamiliar_GrantsFlyBonus()
    {
        var (character, _) = WizardOfLevel(1, 10);
        character.FamiliarType = "Bat";

        Assert.Equal(3, character.Total("Fly"));
    }

    // Tests that a monkey familiar grants +3 Acrobatics to the master.
    [Fact]
    public void MonkeyFamiliar_GrantsAcrobaticsBonus()
    {
        var (character, _) = WizardOfLevel(1, 10);
        character.FamiliarType = "Monkey";

        Assert.Equal(3, character.Total("Acrobatics"));
    }

    // Tests that a lizard familiar grants +3 Climb to the master.
    [Fact]
    public void LizardFamiliar_GrantsClimbBonus()
    {
        var (character, _) = WizardOfLevel(1, 10);
        character.FamiliarType = "Lizard";

        Assert.Equal(3, character.Total("Climb"));
    }

    // Tests that an owl familiar grants +3 Perception plus +2 from Alertness at 1st level.
    [Fact]
    public void OwlFamiliar_GrantsPerceptionBonus()
    {
        var (character, _) = WizardOfLevel(1, 10);
        character.FamiliarType = "Owl";

        Assert.Equal(5, character.Total("Perception")); // +3 familiar type, +2 Alertness
    }

    // Tests that a hawk familiar grants +3 Perception plus +2 from Alertness at 1st level.
    [Fact]
    public void HawkFamiliar_GrantsPerceptionBonus()
    {
        var (character, _) = WizardOfLevel(1, 10);
        character.FamiliarType = "Hawk";

        Assert.Equal(5, character.Total("Perception")); // +3 familiar type, +2 Alertness
    }

    // Tests that a raven familiar grants +3 Appraise to the master.
    [Fact]
    public void RavenFamiliar_GrantsAppraiseBonus()
    {
        var (character, _) = WizardOfLevel(1, 10);
        character.FamiliarType = "Raven";

        Assert.Equal(3, character.Total("Appraise"));
    }

    // Tests that a viper familiar grants +3 Bluff to the master.
    [Fact]
    public void ViperFamiliar_GrantsBluffBonus()
    {
        var (character, _) = WizardOfLevel(1, 10);
        character.FamiliarType = "Viper";

        Assert.Equal(3, character.Total("Bluff"));
    }

    // Tests that a weasel familiar grants +2 Reflex to the master.
    [Fact]
    public void WeaselFamiliar_GrantsReflexBonus()
    {
        var (character, _) = WizardOfLevel(1, 10);
        character.FamiliarType = "Weasel";

        Assert.Equal(2, character.Total("Reflex"));
    }

    // Tests that the familiar's Alertness ability grants +2 Perception and +2 Sense Motive at 1st level.
    [Fact]
    public void Familiar_AlertnessAtFirstLevel()
    {
        var (character, _) = WizardOfLevel(1, 10);
        character.FamiliarType = "Cat";

        Assert.Equal(2, character.Total("Perception"));
        Assert.Equal(2, character.Total("Sense Motive"));
    }

    // Tests that familiar level-based Alertness still applies at higher levels.
    [Fact]
    public void Familiar_AlertnessAtHigherLevel()
    {
        var (character, _) = WizardOfLevel(5, 10);
        character.FamiliarType = "Cat";

        Assert.Equal(2, character.Total("Perception"));
        Assert.Equal(2, character.Total("Sense Motive"));
    }

    // Tests that familiar bonuses from type and level stack correctly.
    [Fact]
    public void Familiar_TypeAndLevelBonusesStack()
    {
        var (character, _) = WizardOfLevel(1, 10);
        character.FamiliarType = "Cat";

        // Cat gives +3 Stealth; familiar level gives +2 Perception and +2 Sense Motive.
        Assert.Equal(3, character.Total("Stealth"));
        Assert.Equal(2, character.Total("Perception"));
        Assert.Equal(2, character.Total("Sense Motive"));
    }

    // Tests that changing familiar type changes the bonus.
    [Fact]
    public void ChangeFamiliarType_ChangesBonus()
    {
        var (character, _) = WizardOfLevel(1, 10);
        character.FamiliarType = "Cat";
        Assert.Equal(3, character.Total("Stealth"));

        character.FamiliarType = "Rat";
        Assert.Equal(0, character.Total("Stealth"));
        Assert.Equal(2, character.Total("Fortitude"));
    }
}
