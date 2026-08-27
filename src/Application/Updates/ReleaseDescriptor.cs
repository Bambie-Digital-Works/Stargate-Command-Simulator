namespace WormholeWorlds.Application.Updates;

public sealed record ReleaseDescriptor(
    string Version,
    bool IsPrerelease,
    string ReleaseNotes,
    Uri ReleasePageUri,
    Uri InstallerUri,
    Uri ChecksumsUri,
    long InstallerSizeBytes,
    string InstallerFileName);
