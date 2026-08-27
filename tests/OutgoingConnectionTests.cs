using WormholeWorlds.Core.Destinations;
using WormholeWorlds.Core.Transit;
using WormholeWorlds.Infrastructure.Content;

namespace WormholeWorlds.Tests;

public sealed class OutgoingConnectionTests
{
    private static readonly DestinationRecord Destination = new(
        "test_site",
        "Test Site",
        ["alpha", "bravo", "charlie", "delta"],
        40,
        25);

    [Fact]
    public void RegisteredVectorReachesStableLinkAndReleasesResourcesAfterClosure()
    {
        FacilityResourcePool resources = new(100, 100);
        OutgoingConnection connection = Create(resources);

        Assert.True(connection.Prepare(Destination.Id, Destination.Vector, At(1)).IsAccepted);
        Assert.True(connection.BeginSequence(At(2)).IsAccepted);
        foreach (string element in Destination.Vector)
        {
            Assert.True(connection.LockNext(element).IsAccepted);
        }

        Assert.True(connection.BeginStabilization(At(3)).IsAccepted);
        Assert.True(connection.ConfirmStable(At(4)).IsAccepted);
        Assert.Equal(TransitArrayPhase.LinkOpen, connection.Snapshot.TransitArray.Phase);
        Assert.Equal(40, resources.ReservedPower);
        Assert.True(connection.Close(At(5)).IsAccepted);
        Assert.True(connection.CompleteClosure(At(6)).IsAccepted);
        Assert.Equal(0, resources.ReservedPower);
        Assert.True(connection.CompleteCooldown(At(7)).IsAccepted);
        Assert.Null(connection.Snapshot.DestinationId);
    }

    [Fact]
    public void InvalidVectorAndInsufficientResourcesDoNotReserveAnything()
    {
        FacilityResourcePool resources = new(39, 24);
        OutgoingConnection connection = Create(resources);

        OutgoingOperationResult invalid = connection.Prepare(Destination.Id, ["wrong"], At(1));
        OutgoingOperationResult unavailable = connection.Prepare(Destination.Id, Destination.Vector, At(2));

        Assert.Equal("destination_vector_invalid", invalid.Rejection?.ReasonCode);
        Assert.Equal("resources_unavailable", unavailable.Rejection?.ReasonCode);
        Assert.Equal(0, resources.ReservedPower);
        Assert.Equal(0, resources.ReservedCooling);
        Assert.Equal(TransitArrayPhase.Standby, connection.Snapshot.TransitArray.Phase);
    }

    [Fact]
    public void PartialSequenceRejectsOutOfOrderLockAndAbortReleasesReservation()
    {
        FacilityResourcePool resources = new(100, 100);
        OutgoingConnection connection = Create(resources);
        connection.Prepare(Destination.Id, Destination.Vector, At(1));
        connection.BeginSequence(At(2));

        OutgoingOperationResult wrong = connection.LockNext("bravo");
        connection.LockNext("alpha");
        OutgoingOperationResult incomplete = connection.BeginStabilization(At(3));
        OutgoingOperationResult aborted = connection.Abort(At(4));

        Assert.Equal("vector_lock_out_of_order", wrong.Rejection?.ReasonCode);
        Assert.Equal("sequence_incomplete", incomplete.Rejection?.ReasonCode);
        Assert.Equal(TransitArrayPhase.Recovering, aborted.Snapshot.TransitArray.Phase);
        Assert.Equal(0, resources.ReservedPower);
        Assert.Equal(0, resources.ReservedCooling);
    }

    [Fact]
    public void UnscheduledIncomingSkipsVectorLocksAndReachesStableLink()
    {
        FacilityResourcePool resources = new(100, 100);
        OutgoingConnection connection = Create(resources);

        Assert.True(connection.DetectIncoming(At(1)).IsAccepted);
        Assert.Equal(TransitArrayPhase.IncomingDetected, connection.Snapshot.TransitArray.Phase);
        Assert.Equal(TransitLinkDirection.Incoming, connection.Snapshot.TransitArray.Direction);
        Assert.Equal(OutgoingConnection.UnscheduledIncoming.RequiredPowerUnits, resources.ReservedPower);
        Assert.True(connection.BeginStabilization(At(2)).IsAccepted);
        Assert.True(connection.ConfirmStable(At(3)).IsAccepted);
        Assert.Equal(TransitArrayPhase.LinkOpen, connection.Snapshot.TransitArray.Phase);
        Assert.Equal(OutgoingConnection.UnscheduledIncoming.Id, connection.Snapshot.DestinationId);
    }

    [Fact]
    public void DetectIncomingRejectedWhenResourcesUnavailableOrNotStandby()
    {
        FacilityResourcePool starved = new(20, 20);
        OutgoingConnection starvedConnection = Create(starved);
        OutgoingOperationResult unavailable = starvedConnection.DetectIncoming(At(1));
        Assert.Equal("resources_unavailable", unavailable.Rejection?.ReasonCode);
        Assert.Equal(0, starved.ReservedPower);

        FacilityResourcePool resources = new(100, 100);
        OutgoingConnection connection = Create(resources);
        Assert.True(connection.Prepare(Destination.Id, Destination.Vector, At(1)).IsAccepted);
        OutgoingOperationResult notStandby = connection.DetectIncoming(At(2));
        Assert.False(notStandby.IsAccepted);
        Assert.Equal(40, resources.ReservedPower);
    }

    [Fact]
    public void CurrentDestinationContentLoadsStrictly()
    {
        string root = FindRepositoryRoot();
        string json = File.ReadAllText(Path.Combine(root, "content", "destinations.v1.json"));

        DestinationRegistry registry = new DestinationRegistryLoader().Load(json);

        Assert.Equal(2, registry.Records.Count);
        Assert.Throws<InvalidDataException>(() => new DestinationRegistryLoader().Load(json.Replace("\"schemaVersion\"", "\"unknown\"")));
    }

    private static OutgoingConnection Create(FacilityResourcePool resources) =>
        new(new DestinationRegistry([Destination]), resources);

    private static SimulationInstant At(long milliseconds) => new(milliseconds);

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
