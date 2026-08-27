# Project overview

## Vision

Create an original Windows operations-room game in which the player commands a secret underground Command Facility built around an experimental Transit Array. The interface is the game: players interpret telemetry, follow procedures, coordinate personnel, and choose when incomplete information justifies taking a risk.

Wormhole Worlds Simulator is the approved beta title. Production content does not use protected franchise terminology, symbols, lore, characters, production designs, audio, or other distinctive assets.

## Audience and format

- Players who enjoy systems-driven workplace, command, document, and crisis-management simulators.
- Single-player Windows desktop game built with Godot 4 and C#.
- UI-first presentation rather than a freely explorable 3D facility.
- Individual shifts targeted at 20–35 minutes, with persistent campaign consequences.

## Vertical slice

The `0.8.0-beta.1` vertical slice contains one complete shift using the approved names in the [production terminology guide](TERMINOLOGY.md):

- an Operations Board;
- Transit Control;
- Return Control and Containment Shutter controls;
- Survey Telemetry;
- an Expedition Roster and dispatch workflow;
- a Systems Board; and
- Shift Brief and chronological Shift Review screens.

The slice demonstrates five escalating incidents: routine reconnaissance, a friendly return with a damaged Return Credential, an unscheduled incoming Transit Link with spoofed credentials, contradictory Survey Drone telemetry, and a combined medical emergency and cooling-system fault.

## Core systems

- A deterministic Transit Array state machine with verified transitions and safe aborts.
- Destination Registry records, Destination Vector validation, Vector Locks, stability, power, and cooling.
- Challenge-response authentication with expiry, duress, corruption, and spoofing cases.
- Barrier and containment controls whose timing has irreversible consequences.
- Survey Drone telemetry for atmosphere, radiation, biology, terrain, video, and signal quality.
- Personnel specialties, fatigue, equipment, injuries, mission clocks, and availability.
- Seeded incidents and an event log that can reproduce a shift for testing and support.
- Versioned, atomic saves with backups and forward migrations.

## Presentation and accessibility baseline

The visual direction combines an industrial underground facility with an original, legible command interface. Colour conveys priority but is never the only carrier of meaning. Red is reserved for immediate danger. Final art and audio must be original, commissioned with clear rights, or used under a verified compatible licence recorded in the asset ledger.

The baseline includes scalable UI, high-contrast and colour-vision-safe states, captions for radio and alarm information, independent volume controls, remappable keyboard/mouse/controller input, pause, reduced flashing, and adjustable time pressure.

## Version targets

| Version | Completion definition |
|---|---|
| `0.1.0` Prototype | Transit Array state model, Operations Board, Transit Control, Return Control, Containment Shutter, and Expedition Roster are interactable. |
| `0.3.0` Vertical Slice | One polished shift, five incidents, Survey Telemetry, persistence, Shift Review, original placeholder identity, and accessibility baseline are playable. |
| `0.8.0` Beta Foundation | Content and save formats are stabilized; hash-verified Windows release candidates, preview/stable channels, migration tests, rollback procedures, and optional signing support exist. |
| `1.0.0` Stable Release | Original title and branding are cleared; release content, compatibility QA, packaging, support, and legal/asset reviews are complete. |

Product versions follow Semantic Versioning. Windows packages use a compatible four-part numeric mapping, while every build also records its CI build number, Git commit, content schema, save schema, UTC build time, and release channel.

## Explicit boundaries

- No multiplayer, explorable 3D base, mod SDK, or licensed-franchise content is required for the vertical slice.
- No outside code or creative contributions are accepted under the current ownership policy.
- Research images are references and are not assumed to be production-ready assets.
