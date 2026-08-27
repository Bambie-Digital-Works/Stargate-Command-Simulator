using System.Reflection;
using System.Runtime.InteropServices;
using Godot;
using WormholeWorlds.Application.Diagnostics;

namespace WormholeWorlds.Infrastructure.Diagnostics;

public sealed class AssemblyBuildMetadataProvider : IBuildMetadataProvider
{
    private readonly Assembly _assembly;
    private readonly RuntimeBuildContext? _runtimeContext;

    public AssemblyBuildMetadataProvider(Assembly? assembly = null, RuntimeBuildContext? runtimeContext = null)
    {
        _assembly = assembly ?? typeof(AssemblyBuildMetadataProvider).Assembly;
        _runtimeContext = runtimeContext;
    }

    public BuildMetadata Get()
    {
        IReadOnlyDictionary<string, string> values = _assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(attribute => !string.IsNullOrWhiteSpace(attribute.Key))
            .ToDictionary(attribute => attribute.Key, attribute => attribute.Value ?? string.Empty, StringComparer.Ordinal);

        string productVersion = _assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion.Split('+')[0] ?? "0.0.0-dev";

        RuntimeBuildContext runtime = _runtimeContext ?? new RuntimeBuildContext(
            Engine.GetVersionInfo()["string"].AsString(),
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.OSDescription,
            RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant());

        return new BuildMetadata(
            productVersion,
            Required(values, "BuildChannel"),
            Required(values, "BuildId"),
            Required(values, "CommitSha"),
            ParsePositiveInteger(values, "ContentSchemaVersion"),
            ParsePositiveInteger(values, "SaveSchemaVersion"),
            ParseUtc(values, "BuildUtc"),
            runtime.EngineVersion,
            runtime.RuntimeVersion,
            runtime.OperatingSystem,
            runtime.Architecture);
    }

    private static string Required(IReadOnlyDictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Build metadata field '{key}' is missing.");
        }

        return value;
    }

    private static int ParsePositiveInteger(IReadOnlyDictionary<string, string> values, string key)
    {
        string value = Required(values, key);
        if (!int.TryParse(value, out int parsed) || parsed < 1)
        {
            throw new InvalidOperationException($"Build metadata field '{key}' must be a positive integer.");
        }

        return parsed;
    }

    private static DateTimeOffset ParseUtc(IReadOnlyDictionary<string, string> values, string key)
    {
        string value = Required(values, key);
        if (!DateTimeOffset.TryParse(value, out DateTimeOffset parsed) || parsed.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException($"Build metadata field '{key}' must be a UTC timestamp.");
        }

        return parsed;
    }
}

public sealed record RuntimeBuildContext(
    string EngineVersion,
    string RuntimeVersion,
    string OperatingSystem,
    string Architecture);
