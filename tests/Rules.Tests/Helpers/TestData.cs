using Rules.Core.Data;

namespace Rules.Tests;

// Provides test data for unit tests, including predefined classes, races, and skills.
public static class TestData
{
    // Constructs the full path to a data file within the test data directory.
    private static string DataPath(params string[] parts) =>
        Path.Combine(new[] { AppContext.BaseDirectory, "data", "core" }.Concat(parts).ToArray());

    
    // Returns the predefined Fighter class definition.
    public static ClassDefinition Fighter() =>
        DataPackLoader.LoadClass(DataPath("classes", "fighter.json"));

    // Returns the predefined Wizard class definition.
    public static ClassDefinition Wizard() =>
        DataPackLoader.LoadClass(DataPath("classes", "wizard.json"));

    // Returns the predefined Elf race definition.
    public static RaceDefinition Elf() =>
        DataPackLoader.LoadRace(DataPath("races", "elf.json"));
    
    // Returns the predefined Dwarf race definition.
    public static RaceDefinition Dwarf() =>
        DataPackLoader.LoadRace(DataPath("races", "dwarf.json"));

    // Returns the list of all predefined skill definitions.
    public static List<SkillDefinition> Skills() =>
        DataPackLoader.LoadSkills(DataPath("skills.json"));

    // Returns the skill definition with the specified name.
    public static SkillDefinition Skill(string name) =>
        Skills().Single(s => s.Name == name);
    
    // Returns the list of all predefined feat definitions.
    public static List<FeatDefinition> Feats() =>
        DataPackLoader.LoadFeatsFromFolder(DataPath("feats"));

    // Returns the feat definition with the specified name.
    public static FeatDefinition Feat(string name) =>
        Feats().Single(f => f.Name == name);
}