using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rules.Core.Data;

// Provides methods for loading data packs, such as races and classes, from JSON files.
public static class DataPackLoader
{
    // JSON serializer options for reading data packs.
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new JsonStringEnumConverter() }
    };

    // Loads a race definition from a JSON file at the specified path.
    public static RaceDefinition LoadRace(string path) => Load<RaceDefinition>(path);

    // Loads a class definition from a JSON file at the specified path.
    public static ClassDefinition LoadClass(string path) => Load<ClassDefinition>(path);

    // Loads a skill definition list from a JSON file at the specified path.    
    public static List<SkillDefinition> LoadSkills(string path) => Load<List<SkillDefinition>>(path);

    // Loads a generic data pack from a JSON file at the specified path.
    private static T Load<T>(string path)
    {
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<T>(stream, Options)
            ?? throw new InvalidDataException($"Could not read '{path}'.");
    }
}