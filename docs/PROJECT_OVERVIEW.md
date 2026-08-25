# Project overview

## Vision

Create an original Windows operations-room game in which the player commands a secret underground facility built around an experimental interstellar transit portal. The interface is the game: players interpret telemetry, follow procedures, coordinate personnel, and choose when incomplete information justifies taking a risk.

“Stargate Command Simulator” is a working title only. Production content will not use protected franchise terminology, symbols, lore, characters, production designs, audio, or other distinctive assets.

## Audience and format

- Players who enjoy systems-driven workplace, command, document, and crisis-management simulators.
- Single-player Windows desktop game built with Godot 4 and C#.
- UI-first presentation rather than a freely explorable 3D facility.
- Individual shifts targeted at 20–35 minutes, with persistent campaign consequences.

## Vertical slice

The `0.3.0` vertical slice contains one complete shift with:

- an operations overview;
- a portal/dialing console;
- incoming authentication and defensive-barrier controls;
- a remote-probe telemetry station;
- a team roster and dispatch workflow;
- a facility status and alarm view; and
- shift briefing and chronological debrief screens.

The slice demonstrates five escalating incidents: routine reconnaissance, a friendly return with damaged authentication, an unscheduled incoming connection with spoofed credentials, contradictory probe telemetry, and a combined medical emergency and cooling-system fault.

## Core systems

- A deterministic portal connection state machine with verified transitions and safe aborts.
- Destination records, address validation, lock sequencing, stability, power, and cooling.
- Challenge-response authentication with expiry, duress, corruption, and spoofing cases.
- Barrier and containment controls whose timing has irreversible consequences.
- Probe telemetry for atmosphere, radiation, biology, terrain, video, and signal quality.
- Personnel specialties, fatigue, equipment, injuries, mission clocks, and availability.
- Seeded incidents and an event log that can reproduce a shift for testing and support.
- Versioned, atomic saves with backups and forward migrations.

## Presentation and accessibility baseline

The visual direction combines an industrial underground facility with an original, legible command interface. Colour conveys priority but is never the only carrier of meaning. Red is reserved for immediate danger. Final art and audio must be original, commissioned with clear rights, or used under a verified compatible licence recorded in the asset ledger.

The baseline includes scalable UI, high-contrast and colour-vision-safe states, captions for radio and alarm information, independent volume controls, remappable keyboard/mouse/controller input, pause, reduced flashing, and adjustable time pressure.

## Version targets

| Version | Completion definition |
|---|---|
| `0.1.0` Prototype | Portal state model, overview, dialing, incoming authentication, barrier controls, and roster workflow are interactable. |
| `0.3.0` Vertical Slice | One polished shift, five incidents, probe station, persistence, debrief, original placeholder identity, and accessibility baseline are playable. |
| `0.8.0` Beta Foundation | Content and save formats are stabilized; signed Windows release candidates, preview/stable channels, migration tests, and rollback procedures exist. |
| `1.0.0` Stable Release | Original title and branding are cleared; release content, compatibility QA, packaging, support, and legal/asset reviews are complete. |

Product versions follow Semantic Versioning. Windows packages use a compatible four-part numeric mapping, while every build also records its CI build number, Git commit, content schema, save schema, UTC build time, and release channel.

## Explicit boundaries

- No multiplayer, explorable 3D base, mod SDK, or licensed-franchise content is required for the vertical slice.
- No outside code or creative contributions are accepted under the current ownership policy.
- Research images are references and are not assumed to be production-ready assets.
