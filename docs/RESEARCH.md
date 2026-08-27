# Wormhole Worlds — research and production brief

> Wormhole Worlds is the original-IP beta title. Television and fan-game references in this document are research sources only, not production assets or a claim of affiliation or licence.

Research terms on this page are reference-only. Production specifications, code, content, and UI must use the approved [production terminology](TERMINOLOGY.md).

Research date: 2026-08-25. This is a design reference, not legal advice.

## Product thesis

Build a tense Windows operations-room game in which the player runs an underground interstellar command. The fantasy is not simply “watch a ring spin”; it is making fast, imperfect decisions while several systems and people compete for attention.

A strong loop is:

1. Review the shift briefing, teams, known destinations, maintenance status, and threat level.
2. Schedule an expedition and dial a multi-symbol address.
3. Establish and stabilize a connection, verify telemetry, then send a probe.
4. Authenticate a returning team, control the barrier, and handle medical/security screening.
5. React to exceptions: an unscheduled activation, spoofed identification, power failure, hostile signal, injured team, contagion, or object lodged in transit.
6. Debrief, allocate research and repairs, update the destination database, and accept the consequences.

The television reference supports this structure. Stargate Command is depicted as a multi-department underground base; its control room monitors incoming and outgoing activity, while the Earth-built dialing system works without a DHD. Episode references also establish hundreds of feedback signals during dialing and an isolated dialing computer—excellent inspiration for diagnostics and security gameplay ([GateWorld: Stargate Command](https://gateworld.net/wiki/Stargate_Command), [“48 Hours”](https://www.gateworld.net/sg1/s5/48-hours/), [“Proving Ground”](https://www.gateworld.net/sg1/s5/proving-ground/)). MGM identifies Stargate as its franchise; assume the names, logos, symbols, characters, footage, sounds, and production designs are protected unless licensed ([MGM](https://www.mgm.com/franchise/stargate)).

## Reference study: SG-1 (reference only)

Use these as functional inspiration, then give the shipped game its own terminology and visual language unless an MGM/Amazon licence is secured.

| Fictional element | Useful game system | Player-facing tension |
|---|---|---|
| Gate room and blast window (reference only) | Live camera, room pressure, radiation and barrier state | Opening the room too early risks the base |
| Dialing computer (reference only) | Address entry, symbol lock sequence, calibration and feedback channels | Speed versus a safe, verified lock |
| Iris/barrier (reference only) | Closed/open/locked/damaged states | Authentication may arrive seconds before impact |
| GDO-style identifier | Challenge-response codes, expiry, duress codes | Friendly, stolen, duplicated, or garbled credentials |
| MALP/probe (reference only) | Camera, atmosphere, radiation, motion, sample and signal data | Spend time verifying or risk the team |
| SG teams (reference only) | Personnel, specialties, fatigue, equipment, injuries | The ideal team may be unavailable |
| Base zones | Lockdown doors, ventilation, quarantine, armoury and evacuation | Contain a threat without trapping staff |
| Command staff | Requests, overrides, political pressure and incomplete intelligence | Obey, challenge, or delay an order |
| Address database | Known worlds, aliases, drift correction, last contact and risk | Knowledge accumulates across a campaign |
| Self-destruct/failsafe | Two-person authorization and abort window | Rare, consequential final option |

### Screen set

- Operations overview: gate state, countdowns, alarms, active team, power and base readiness.
- Dialing console: symbol/address selection, lock progress, motor load, feedback and abort.
- Incoming activation: waveform, origin estimate, identification exchange, barrier controls and clock.
- Probe station: video plus atmospheric, biological, radiation and terrain telemetry.
- Team board: roster, specialties, fatigue, loadout, mission clock and extraction priority.
- Base security: zone map, doors, cameras, ventilation, containment and response squads.
- Intelligence archive: destinations, factions, languages, artefacts and confidence-rated reports.
- Engineering: generators, capacitors, cooling, gate mechanism and repair queues.
- Debrief/replay: an event timeline showing exactly why the shift succeeded or failed.

Accessibility should be designed in: scalable UI, high-contrast and colour-blind-safe alarm states, captions for every radio cue, independent volume controls, remappable keyboard/mouse/gamepad input, pause or adjustable time pressure, and reduced flash/camera shake.

## Comparable games and prototypes

Do not copy their interfaces; study the interaction patterns.

- **SGCSim**: a historical Flash/Windows fan simulator centred on dialing, iris decisions, probes, and known destinations. It validates the core fantasy but also shows the danger of relying on novelty and copyrighted presentation ([overview](https://sgcsim.software.informer.com/)).
- **AISNSim**: a PC interface simulator inspired by Stargate Atlantis. Useful evidence that fans enjoy “operating the OS” as the game itself ([developer page](https://ddobs.com/aisnsim/)).
- **Uplink**: demonstrates how mundane desktop actions become dramatic through timed missions, layered tools, audio, traces, and consequences.
- **DEFCON**: readable strategic abstraction, escalating readiness states, and emotionally cold presentation.
- **Not For Broadcast**: multiple live controls, interruptions, narrative consequences, and escalating workload.
- **Papers, Please**: document verification becomes a deep loop through exceptions, limited time, and human consequences.
- **911 Operator / Radio Commander**: map-mediated command, incomplete information, dispatch, and voice/radio atmosphere.
- **Duskers / Objects in Space**: sensor-led discovery makes missing information more interesting than full visibility.
- **Keep Talking and Nobody Explodes**: good model for an optional two-player mode—one operator at the console, one using a procedures manual.

The design opportunity is to combine a believable workplace simulator with campaign consequences. A “shift” of 20–35 minutes is a sensible initial target. Between shifts, unlock new protocols and equipment rather than merely larger numbers.

### Expanded source study

The following SG-1 references were reviewed on 2026-08-27 to turn the
television premise into original systems rather than copied fiction:

- **“Foothold”** supports identity uncertainty, compromised personnel, sensor
  countermeasures, security-room monitoring, lockdown, and the possibility that
  an infiltrator learns the facility layout. Use these as procedural security
  incidents with original factions and technologies.
- **“Proving Ground”** supports recurring training exercises, candidate
  assessment, access permissions, simulated failures, and the human cost of
  deciding whether to leave a team member behind. Use this for onboarding and
  expedition qualification.
- **“A Matter of Time”** supports a connection that will not disengage,
  capacitor depletion, time distortion, evacuation, and a two-person
  destructive failsafe. Use this as a rare high-severity facility crisis.
- **“Watergate”** supports competing gate operators, a remote facility,
  international technology exchange, a submerged destination, and missions
  affected by another active connection. Use this for diplomacy and campaign
  state, not for copied governments or episode plot.

The requested simulator references reinforce the same interaction pattern:
AISNSim treats the fictional operating system as the play space and unlocks
additional screens over time; TSACS shows the value of modular panels,
address-book data, diagnostics, logs, and external content configuration; and
SGCSim places the player in a technician role where probing, dispatching, and
barrier decisions form the core loop. The Stargate Network visual study
supports a dark navy/black atmosphere with restrained gold and cyan accents.
Wormhole Worlds uses these as high-level interaction and mood references only;
all shipped terminology, layouts, marks, art, audio, and lore remain original.

Sources: [“Foothold”](https://www.gateworld.net/sg1/s3/foothold/),
[“Proving Ground”](https://www.gateworld.net/sg1/s5/proving-ground/),
[“A Matter of Time”](https://www.gateworld.net/sg1/s2/a-matter-of-time/),
[“Watergate”](https://www.gateworld.net/sg1/s4/watergate/),
[AISNSim](https://ddobs.com/aisnsim/), [TSACS](https://www.tsacs.com/),
[SGCSim](https://sgcsim.software.informer.com/5.1/), and
[Stargate Network](https://stargate-network.com/).

## Content plan

### Vertical slice

One gate/control-room view, one dialing console, one team roster, one probe screen, and five authored incidents:

1. Routine outgoing reconnaissance.
2. Friendly return with a damaged identifier.
3. Unscheduled incoming connection and spoofed code.
4. Probe detects an environmental contradiction.
5. Simultaneous medical emergency and cooling fault.

Instrument the slice: completion/failure reason, time spent per screen, alarm acknowledgement latency, barrier decisions, tutorial retries, and quit point. Prefer local/opt-in telemetry and disclose it.

### Campaign progression

- Phase 1: training and local technical failures.
- Phase 2: unreliable contacts, diplomacy, and resource limits.
- Phase 3: infiltration, misinformation, and multi-system incidents.
- Phase 4: base-wide crises with persistent casualties and political consequences.

Use deterministic seeds plus an event log so bugs and player reports can be replayed. Save the content/schema version inside every save. Migrations must be forward-only, backed up before conversion, and tested against representative saves from every supported release.

## Art and image reference pack

The repository folder `assets/reference/public-domain/` contains three public-domain US government reference images and a machine-readable `SOURCES.md`. They are mood/layout references, not an endorsement by the Department of Defense. Verify the source page at final publication because DVIDS notes that individual items can carry restrictions.

Recommended visual recipe: late-1990s/early-2000s industrial command centre; powder-coated steel, concrete, thick glazing, recessed fluorescent light, CRT bloom mixed with upgraded LCD panels, physical key switches, restrained blue/green telemetry, amber warnings, red only for immediate danger. Make silhouettes, symbols, nomenclature, typefaces, console layouts and the portal device original.

Copyrighted show imagery should remain an internal reference board only. Useful pages include the [MGM franchise page](https://www.mgm.com/franchise/stargate), [GateWorld SGC reference](https://gateworld.net/wiki/Stargate_Command), and the [SGC standing-set discussion](https://movies.stackexchange.com/questions/123309/floor-plans-of-the-stargate-command-standing-set-sg-1). Do not ship screen captures, actor likenesses, the franchise logo, gate glyphs, sound effects, dialogue, or traced UI without permission.

Safer sources for production assets:

- Original commissioned/created art with a written work-for-hire or assignment agreement.
- Public-domain US government photography, checked item-by-item for third-party marks and personality/privacy issues.
- CC0 assets, or CC BY assets with Title/Author/Source/License attribution recorded. Creative Commons recommends the TASL pattern ([guide](https://wiki.creativecommons.org/images/4/4b/OpenBook_licensing.pdf)).
- Licensed commercial libraries whose licence explicitly permits use in games and redistribution only inside compiled builds.

Keep an asset ledger with: filename, creator, source URL, licence, download date, modifications, attribution text, and proof-of-licence snapshot.

## Windows versioning and release process

### Version identifiers

Use one public SemVer-style product version and one monotonically increasing build identifier:

- `0.1.0-alpha.1+build.42` — internal playable milestone.
- `0.4.0-beta.2+build.317` — external test.
- `1.0.0+build.812` — first stable release.
- `1.0.1+build.829` — compatible bug/security fix.
- `1.1.0+build.910` — backward-compatible content or feature release.
- `2.0.0` — only for a genuinely incompatible save/mod/API/platform break.

SemVer defines `MAJOR.MINOR.PATCH`, with pre-release and build metadata ([SemVer 2.0](https://semver.org/)). Before 1.0, treat minor releases as potentially save/mod breaking and say so in release notes.

Windows package versions have a separate numeric constraint. MSIX uses four numeric parts; for Store UWP packages the fourth part is reserved and should be `0`, with the first three values in the supported integer range ([Microsoft package requirements](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/app-package-requirements)). Map product `1.4.2` build 913 to display version `1.4.2` and package version `1.4.913.0`; never derive ordering from Git commit counts that can reset.

Embed these in the executable and diagnostics:

- Product version and channel (`stable`, `preview`, `nightly`).
- CI build number and full Git commit SHA.
- Content/data schema version and save schema version.
- Build UTC timestamp, engine/runtime version, and target architecture.

### Branches and channels

- `main`: always releasable; protected, reviewed, CI required.
- short-lived `feature/*` and `fix/*`; merge through pull requests.
- signed tags such as `v0.4.0-beta.2`; never move a release tag.
- `nightly`: automated and disposable.
- `preview`: opt-in testers; save folder separated from stable.
- `stable`: promoted only from the exact tested artifact—do not rebuild it.
- `legacy`: previous stable build for rollback/save recovery.

Steam supports public/private beta branches and manual promotion to the default branch, which fits preview/stable/legacy delivery ([Steam branches](https://partner.steamgames.com/doc/store/application/branches?l=english), [builds](https://partner.steamgames.com/doc/store/application/builds?l=english)). Steam Playtest can separate testing ownership from the main game ([testing](https://partner.steamgames.com/doc/store/testing?language=english)).

### CI/CD release gates

For every commit: format/lint, compile all supported architectures, unit tests, content validation, save migration tests, headless scenario smoke tests, and dependency/security scan.

For a release candidate:

1. Freeze content and version; generate release notes from curated change fragments.
2. Produce one reproducible artifact per architecture and calculate SHA-256 hashes.
3. Run clean-machine install, launch, update, rollback, uninstall, offline launch, non-admin user, unusual Windows username/path, DPI/scaling, multi-monitor, controller hot-plug, and antivirus/SmartScreen checks.
4. Test upgrade from every supported stable version and verify saves/settings survive; also test corrupted and future-version saves.
5. Code-sign and timestamp. Keep signing credentials in a protected CI secret or managed signing service, never in the repository.
6. Publish to preview, observe crash and save-corruption signals, then promote the identical artifact to stable.
7. Retain manifests, hashes, symbols, release notes, licence report, previous installer, and rollback instructions.

MSIX provides clean uninstall, differential downloads, and Store-managed updates; Store submission also handles signing. Directly distributed packages need an appropriate trusted signature, and Microsoft strongly recommends timestamping ([packaging guide](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/packaging/), [signing overview](https://learn.microsoft.com/en-us/windows/msix/package/signing-package-overview)). If a game engine/storefront works better with a portable folder or EXE installer, keep that option; the key requirements are deterministic packaging, signing, clean uninstall, and tested updates.

### Compatibility policy

- Support save migration from the last two stable minor versions at minimum.
- Never overwrite the only save during migration; write a new file, validate, then atomically replace and retain a backup.
- Separate player saves, settings, logs, caches, and downloaded/mod content.
- Mods declare supported game/API ranges; disable incompatible mods with a clear explanation.
- A newer client may read older saves after migration. An older client must refuse newer saves safely.
- Crash reports should include versions and hardware/OS facts but exclude usernames, file paths, save contents, and personal data by default.

## Suggested project milestones

| Version | Exit criterion |
|---|---|
| `0.1.0` prototype | Dial, establish connection, barrier, one scripted return |
| `0.2.0` core loop | Probe, roster, three incident types, save/load |
| `0.3.0` vertical slice | One polished shift, tutorial, original art/audio direction |
| `0.4.0-alpha` | Campaign skeleton, event tools, automated save migrations |
| `0.6.0-alpha` | Content-complete systems, accessibility baseline |
| `0.8.0-beta` | Feature freeze, external testing, signed Windows builds |
| `0.9.0-rc` | Release candidate, localization and compatibility QA |
| `1.0.0` | Stable launch with rollback and support process |

## Decisions established after research

1. Develop original science-fiction IP under the Wormhole Worlds beta title; retain professional trademark review before a commercial stable release.
2. Build a UI-first desktop simulation rather than an explorable 3D base for the initial release.
3. Use Godot 4 with C# and target Windows first; choose storefront packaging after the vertical slice.
4. Implement the five vertical-slice incidents as deterministic, testable event/state models before producing final art.
5. Establish `VERSION`, `CHANGELOG.md`, an asset ledger, save schema, CI build number, and preview/stable channels at project bootstrap.
