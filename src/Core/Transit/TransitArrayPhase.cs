namespace WormholeWorlds.Core.Transit;

public enum TransitArrayPhase
{
    Standby,
    OutgoingPreparation,
    IncomingDetected,
    Sequencing,
    Stabilizing,
    LinkOpen,
    Closing,
    Cooldown,
    Recovering,
    Faulted,
}
