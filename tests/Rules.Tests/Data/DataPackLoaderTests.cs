using System.Text.Json;
using Rules.Core.Data;

namespace Rules.Tests;

public class DataPackLoaderTests
{
    [Fact]
    public void AnUnknownField_FailsLoudlyInsteadOfBeingIgnored()
    {
        string path = Path.Combine(Path.GetTempPath(), $"typo-{Guid.NewGuid()}.json");
        File.WriteAllText(path, """{ "id": "test.class", "name": "Test", "hitDice": 6 }""");

        try
        {
            var error = Assert.Throws<JsonException>(() => DataPackLoader.LoadClass(path));
            Assert.Contains("hitDice", error.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }
}