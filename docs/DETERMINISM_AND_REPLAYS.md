# Determinism and diagnostic replays

Authoritative systems receive simulation time through `ISimulationClock`; they never read wall-clock time. Pause freezes simulation time, and time scaling advances it deterministically. Seeded incident selection uses the repository-owned xorshift implementation so identical seeds and inputs produce the same choices across supported runtimes.

Replay schema version 1 records only content schema version, seed, ordered simulation timestamps, stable command/event identifiers, outcomes, and reason codes. The canonical payload is protected by SHA-256. Unsafe or free-form reason values are replaced with `redacted` before serialization. Replay loading rejects unknown schema versions, invalid checksums, malformed stable identifiers, and out-of-order records.

Replays contain no application logs, usernames, absolute paths, Return Credentials, save content, or remote telemetry. Local export validates and canonicalizes the document before using an atomic `.tmp` replacement under the caller-provided `user://` replay directory.
