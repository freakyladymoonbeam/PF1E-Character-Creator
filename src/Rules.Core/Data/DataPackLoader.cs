using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rules.Core.Data;

// Provides methods for loading data packs, such as races, from JSON files.
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
    public static RaceDefinition LoadRace(string path)
    {
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<RaceDefinition>(stream, Options)
            ?? throw new InvalidDataException($"Could not read a race from '{path}'.");
    }
}