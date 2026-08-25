using System.Text.Json;
using FacilityCommand.Application.Replay;
using FacilityCommand.Application.Simulation;
using FacilityCommand.Core.Transit;
using FacilityCommand.Infrastructure.Replay;
using FacilityCommand.Infrastructure.Simulation;

namespace FacilityCommand.Tests;

public sealed class DeterminismAndReplayTests
{
    [Fact]
    public void ManualClockUsesSimulationTimeForPauseAndScaling()
    {
        ManualSimulationClock clock = new();

        clock.Advance(1000);
        clock.SetPaused(true);
        clock.Advance(5000);
        clock.SetPaused(false);
        clock.SetTimeScale(0.5m);
        clock.Advance(1000);

        Assert.Equal(1500, clock.Current.Milliseconds);
    }

    [Fact]
    public void IdenticalSeedsProduceIdenticalIncidentSequences()
    {
        string[] incidents = ["routine_survey", "cooling_fault", "credential_damage"];

        string[] first = SelectSequence(new XorShiftRandomSource(42), incidents);
        string[] second = SelectSequence(new XorShiftRandomSource(42), incidents);

        Assert.Equal(first, second);
    }

    [Fact]
    public void IdenticalCommandsProduceByteEquivalentReplayAndValidChecksum()
    {
        string first = RecordHappyPath(seed: 99);
        string second = RecordHappyPath(seed: 99);
        ReplayDocument document = JsonSerializer.Deserialize<ReplayDocument>(first, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        })!;

        Assert.Equal(first, second);
        Assert.True(ReplayRecorder.Verify(document));
        Assert.False(ReplayRecorder.Verify(document with { Sha256 = "tampered" }));
    }

    [Fact]
    public void ReplayRedactsUnsafeReasonAndWritesAtomically()
    {
        TransitArray transitArray = new();
        TransitArrayCommand command = new(TransitArrayCommandKind.ReportFault, SimulationInstant.Zero, @"token=C:\Users\Alice\secret");
        TransitTransitionResult result = transitArray.Execute(command);
        ReplayRecorder recorder = new(1, 7);
        recorder.Record(command, result);
        using TestDirectory directory = new();

        string path = new ReplayFileStore(directory.Path).Save("failed_run_1", recorder.Serialize());
        string json = File.ReadAllText(path);

        Assert.DoesNotContain("Alice", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("redacted", json, StringComparison.Ordinal);
        Assert.False(File.Exists(path + ".tmp"));
    }

    private static string[] SelectSequence(ISeededRandomSource random, IReadOnlyList<string> incidents)
    {
        SeededIncidentSelector selector = new(random);
        return Enumerable.Range(0, 10).Select(_ => selector.Select(incidents)).ToArray();
    }

    private static string RecordHappyPath(ulong seed)
    {
        ReplayRecorder recorder = new(1, seed);
        TransitArray transitArray = new();
        TransitArrayCommandKind[] kinds =
        [
            TransitArrayCommandKind.PrepareOutgoing,
            TransitArrayCommandKind.BeginSequence,
            TransitArrayCommandKind.BeginStabilization,
            TransitArrayCommandKind.ConfirmStable,
        ];

        for (int index = 0; index < kinds.Length; index++)
        {
            TransitArrayCommand command = new(kinds[index], new SimulationInstant(index + 1));
            recorder.Record(command, transitArray.Execute(command));
        }

        return recorder.Serialize();
    }
}
