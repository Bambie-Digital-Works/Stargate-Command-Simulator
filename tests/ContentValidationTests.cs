using System.Text.Json;
using WormholeWorlds.Tools.ContentValidator;

namespace WormholeWorlds.Tests;

public sealed class ContentValidationTests
{
    [Fact]
    public void CurrentContentPassesCrossCatalogValidation()
    {
        IReadOnlyList<string> errors = ContentValidation.Validate(FindRepositoryRoot());

        Assert.Empty(errors);
    }

    [Fact]
    public void MissionWithUnknownDestinationIsRejected()
    {
        using TestDirectory directory = new();
        string sourceRoot = FindRepositoryRoot();
        string contentPath = Path.Combine(directory.Path, "content");
        Directory.CreateDirectory(contentPath);

        foreach (string file in Directory.GetFiles(Path.Combine(sourceRoot, "content"), "*.json"))
        {
            File.Copy(file, Path.Combine(contentPath, Path.GetFileName(file)));
        }

        string missionsPath = Path.Combine(contentPath, "missions.v1.json");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(missionsPath));
        using MemoryStream output = new();
        using (Utf8JsonWriter writer = new(output, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", 1);
            writer.WriteStartArray("missions");
            foreach (JsonElement mission in document.RootElement.GetProperty("missions").EnumerateArray())
            {
                writer.WriteStartObject();
                foreach (JsonProperty property in mission.EnumerateObject())
                {
                    if (property.NameEquals("destinationId"))
                    {
                        writer.WriteString(property.Name, "unknown_destination");
                    }
                    else
                    {
                        property.WriteTo(writer);
                    }
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        File.WriteAllBytes(missionsPath, output.ToArray());
        IReadOnlyList<string> errors = ContentValidation.Validate(directory.Path);

        Assert.Contains(errors, error => error.Contains("unknown destination", StringComparison.OrdinalIgnoreCase));
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "project.godot")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }
}
