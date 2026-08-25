using FacilityCommand.Core.Transit;

namespace FacilityCommand.Tests;

public sealed class TransitArrayStateMachineTests
{
    private static readonly IReadOnlyDictionary<(TransitArrayPhase Phase, TransitArrayCommandKind Command), TransitArrayPhase> ExpectedTransitions =
        new Dictionary<(TransitArrayPhase, TransitArrayCommandKind), TransitArrayPhase>
        {
            [(TransitArrayPhase.Standby, TransitArrayCommandKind.PrepareOutgoing)] = TransitArrayPhase.OutgoingPreparation,
            [(TransitArrayPhase.Standby, TransitArrayCommandKind.DetectIncoming)] = TransitArrayPhase.IncomingDetected,
            [(TransitArrayPhase.OutgoingPreparation, TransitArrayCommandKind.BeginSequence)] = TransitArrayPhase.Sequencing,
            [(TransitArrayPhase.IncomingDetected, TransitArrayCommandKind.BeginStabilization)] = TransitArrayPhase.Stabilizing,
            [(TransitArrayPhase.Sequencing, TransitArrayCommandKind.BeginStabilization)] = TransitArrayPhase.Stabilizing,
            [(TransitArrayPhase.Stabilizing, TransitArrayCommandKind.ConfirmStable)] = TransitArrayPhase.LinkOpen,
            [(TransitArrayPhase.LinkOpen, TransitArrayCommandKind.CloseLink)] = TransitArrayPhase.Closing,
            [(TransitArrayPhase.Closing, TransitArrayCommandKind.CompleteClosure)] = TransitArrayPhase.Cooldown,
            [(TransitArrayPhase.Cooldown, TransitArrayCommandKind.CompleteCooldown)] = TransitArrayPhase.Standby,
            [(TransitArrayPhase.Recovering, TransitArrayCommandKind.CompleteRecovery)] = TransitArrayPhase.Standby,
            [(TransitArrayPhase.Faulted, TransitArrayCommandKind.ResetFault)] = TransitArrayPhase.Recovering,
            [(TransitArrayPhase.OutgoingPreparation, TransitArrayCommandKind.Abort)] = TransitArrayPhase.Recovering,
            [(TransitArrayPhase.IncomingDetected, TransitArrayCommandKind.Abort)] = TransitArrayPhase.Recovering,
            [(TransitArrayPhase.Sequencing, TransitArrayCommandKind.Abort)] = TransitArrayPhase.Recovering,
            [(TransitArrayPhase.Stabilizing, TransitArrayCommandKind.Abort)] = TransitArrayPhase.Recovering,
            [(TransitArrayPhase.LinkOpen, TransitArrayCommandKind.Abort)] = TransitArrayPhase.Closing,
            [(TransitArrayPhase.OutgoingPreparation, TransitArrayCommandKind.Timeout)] = TransitArrayPhase.Recovering,
            [(TransitArrayPhase.IncomingDetected, TransitArrayCommandKind.Timeout)] = TransitArrayPhase.Recovering,
            [(TransitArrayPhase.Sequencing, TransitArrayCommandKind.Timeout)] = TransitArrayPhase.Recovering,
            [(TransitArrayPhase.Stabilizing, TransitArrayCommandKind.Timeout)] = TransitArrayPhase.Recovering,
            [(TransitArrayPhase.LinkOpen, TransitArrayCommandKind.Timeout)] = TransitArrayPhase.Closing,
            [(TransitArrayPhase.Closing, TransitArrayCommandKind.Timeout)] = TransitArrayPhase.Recovering,
            [(TransitArrayPhase.Cooldown, TransitArrayCommandKind.Timeout)] = TransitArrayPhase.Recovering,
            [(TransitArrayPhase.Recovering, TransitArrayCommandKind.Timeout)] = TransitArrayPhase.Faulted,
        };

    public static TheoryData<TransitArrayPhase, TransitArrayCommandKind, TransitArrayPhase?> TransitionCases
    {
        get
        {
            TheoryData<TransitArrayPhase, TransitArrayCommandKind, TransitArrayPhase?> cases = new();
            foreach (TransitArrayPhase phase in Enum.GetValues<TransitArrayPhase>())
            {
                foreach (TransitArrayCommandKind command in Enum.GetValues<TransitArrayCommandKind>())
                {
                    TransitArrayPhase? expectedPhase = ExpectedTransitions.TryGetValue((phase, command), out TransitArrayPhase nextPhase)
                        ? nextPhase
                        : command == TransitArrayCommandKind.ReportFault && phase != TransitArrayPhase.Faulted
                            ? TransitArrayPhase.Faulted
                            : null;
                    cases.Add(phase, command, expectedPhase);
                }
            }

            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(TransitionCases))]
    public void EveryStateCommandCombinationIsDefined(
        TransitArrayPhase phase,
        TransitArrayCommandKind commandKind,
        TransitArrayPhase? expectedPhase)
    {
        TransitArray transitArray = Reach(phase);
        TransitArraySnapshot before = transitArray.Snapshot;
        TransitArrayCommand command = new(
            commandKind,
            new SimulationInstant(before.EnteredAt.Milliseconds + 1),
            commandKind == TransitArrayCommandKind.ReportFault ? "test_fault" : null);

        TransitTransitionResult result = transitArray.Execute(command);

        Assert.Equal(expectedPhase is not null, result.IsAccepted);
        if (expectedPhase is not null)
        {
            Assert.NotNull(result.Event);
            Assert.Null(result.Rejection);
            Assert.Equal(expectedPhase.Value, result.Snapshot.Phase);
            Assert.Equal(before.LastEventSequence + 1, result.Event.Sequence);
        }
        else
        {
            Assert.Equal(before, result.Snapshot);
            Assert.Equal("invalid_transition", result.Rejection?.ReasonCode);
        }
    }

    [Fact]
    public void OutgoingHappyPathReturnsToStandbyWithOrderedEvents()
    {
        TransitArray transitArray = new();
        TransitArrayCommandKind[] commands =
        [
            TransitArrayCommandKind.PrepareOutgoing,
            TransitArrayCommandKind.BeginSequence,
            TransitArrayCommandKind.BeginStabilization,
            TransitArrayCommandKind.ConfirmStable,
            TransitArrayCommandKind.CloseLink,
            TransitArrayCommandKind.CompleteClosure,
            TransitArrayCommandKind.CompleteCooldown,
        ];

        List<TransitArrayEvent> events = [];
        for (int index = 0; index < commands.Length; index++)
        {
            TransitTransitionResult result = transitArray.Execute(
                new TransitArrayCommand(commands[index], new SimulationInstant(index + 1)));
            events.Add(Assert.IsType<TransitArrayEvent>(result.Event));
        }

        Assert.Equal(TransitArrayPhase.Standby, transitArray.Snapshot.Phase);
        Assert.Null(transitArray.Snapshot.Direction);
        Assert.Equal(Enumerable.Range(1, commands.Length).Select(value => (long)value), events.Select(item => item.Sequence));
    }

    [Theory]
    [InlineData(TransitArrayPhase.OutgoingPreparation)]
    [InlineData(TransitArrayPhase.IncomingDetected)]
    [InlineData(TransitArrayPhase.Sequencing)]
    [InlineData(TransitArrayPhase.Stabilizing)]
    public void AbortFromPreOpenPhaseRecoversToStandby(TransitArrayPhase phase)
    {
        TransitArray transitArray = Reach(phase);

        TransitTransitionResult abort = ExecuteNext(transitArray, TransitArrayCommandKind.Abort);
        TransitTransitionResult recovery = ExecuteNext(transitArray, TransitArrayCommandKind.CompleteRecovery);

        Assert.Equal(TransitArrayEventKind.AbortInitiated, abort.Event?.Kind);
        Assert.Equal(TransitArrayPhase.Recovering, abort.Snapshot.Phase);
        Assert.Equal(TransitArrayPhase.Standby, recovery.Snapshot.Phase);
        Assert.Null(recovery.Snapshot.Direction);
    }

    [Fact]
    public void RecoveryTimeoutRequiresExplicitFaultReset()
    {
        TransitArray transitArray = Reach(TransitArrayPhase.Recovering);

        TransitTransitionResult timeout = ExecuteNext(transitArray, TransitArrayCommandKind.Timeout);
        TransitTransitionResult reset = ExecuteNext(transitArray, TransitArrayCommandKind.ResetFault);
        TransitTransitionResult recovered = ExecuteNext(transitArray, TransitArrayCommandKind.CompleteRecovery);

        Assert.Equal(TransitArrayPhase.Faulted, timeout.Snapshot.Phase);
        Assert.Equal(TransitArrayPhase.Recovering, reset.Snapshot.Phase);
        Assert.Equal(TransitArrayPhase.Standby, recovered.Snapshot.Phase);
    }

    [Fact]
    public void NonMonotonicTimeAndMissingFaultReasonAreRejectedWithoutMutation()
    {
        TransitArray transitArray = new();
        TransitTransitionResult preparation = transitArray.Execute(
            new TransitArrayCommand(TransitArrayCommandKind.PrepareOutgoing, new SimulationInstant(10)));

        TransitTransitionResult timeRejection = transitArray.Execute(
            new TransitArrayCommand(TransitArrayCommandKind.BeginSequence, new SimulationInstant(9)));
        TransitTransitionResult reasonRejection = transitArray.Execute(
            new TransitArrayCommand(TransitArrayCommandKind.ReportFault, new SimulationInstant(10)));

        Assert.Equal("non_monotonic_time", timeRejection.Rejection?.ReasonCode);
        Assert.Equal("missing_fault_reason", reasonRejection.Rejection?.ReasonCode);
        Assert.Equal(preparation.Snapshot, transitArray.Snapshot);
    }

    private static TransitTransitionResult ExecuteNext(TransitArray transitArray, TransitArrayCommandKind commandKind) =>
        transitArray.Execute(new TransitArrayCommand(
            commandKind,
            new SimulationInstant(transitArray.Snapshot.EnteredAt.Milliseconds + 1),
            commandKind == TransitArrayCommandKind.ReportFault ? "test_fault" : null));

    private static TransitArray Reach(TransitArrayPhase phase)
    {
        TransitArray transitArray = new();
        TransitArrayCommandKind[] commands = phase switch
        {
            TransitArrayPhase.Standby => [],
            TransitArrayPhase.OutgoingPreparation => [TransitArrayCommandKind.PrepareOutgoing],
            TransitArrayPhase.IncomingDetected => [TransitArrayCommandKind.DetectIncoming],
            TransitArrayPhase.Sequencing => [TransitArrayCommandKind.PrepareOutgoing, TransitArrayCommandKind.BeginSequence],
            TransitArrayPhase.Stabilizing =>
            [
                TransitArrayCommandKind.PrepareOutgoing,
                TransitArrayCommandKind.BeginSequence,
                TransitArrayCommandKind.BeginStabilization,
            ],
            TransitArrayPhase.LinkOpen =>
            [
                TransitArrayCommandKind.PrepareOutgoing,
                TransitArrayCommandKind.BeginSequence,
                TransitArrayCommandKind.BeginStabilization,
                TransitArrayCommandKind.ConfirmStable,
            ],
            TransitArrayPhase.Closing =>
            [
                TransitArrayCommandKind.PrepareOutgoing,
                TransitArrayCommandKind.BeginSequence,
                TransitArrayCommandKind.BeginStabilization,
                TransitArrayCommandKind.ConfirmStable,
                TransitArrayCommandKind.CloseLink,
            ],
            TransitArrayPhase.Cooldown =>
            [
                TransitArrayCommandKind.PrepareOutgoing,
                TransitArrayCommandKind.BeginSequence,
                TransitArrayCommandKind.BeginStabilization,
                TransitArrayCommandKind.ConfirmStable,
                TransitArrayCommandKind.CloseLink,
                TransitArrayCommandKind.CompleteClosure,
            ],
            TransitArrayPhase.Recovering => [TransitArrayCommandKind.PrepareOutgoing, TransitArrayCommandKind.Abort],
            TransitArrayPhase.Faulted => [TransitArrayCommandKind.ReportFault],
            _ => throw new ArgumentOutOfRangeException(nameof(phase)),
        };

        foreach (TransitArrayCommandKind command in commands)
        {
            TransitTransitionResult result = ExecuteNext(transitArray, command);
            Assert.True(result.IsAccepted);
        }

        return transitArray;
    }
}
