# Production terminology guide

Status: **Approved**  
Approved by: project owner  
Approval date: 2026-08-25

This guide is the source of truth for player-facing and production-facing names. New code, content, interface copy, and specifications must use the approved term. Reference-only language may appear in clearly labelled research or legal context, but never as shipped fiction or interface vocabulary.

## Approved vocabulary

| Concept | Approved player-facing term | Stable code/content identifier | Definition |
|---|---|---|---|
| Interstellar transit device | **Transit Array** | `transit_array` | The facility machine that creates a link between two sites. |
| Active traversable connection | **Transit Link** | `transit_link` | A temporary connection established by the Transit Array. |
| Connection-establishment procedure | **Link Sequence** | `link_sequence` | The ordered verification, power, lock, and stabilization process. |
| Destination coordinate sequence | **Destination Vector** | `destination_vector` | The ordered symbols or coordinates identifying a remote site. |
| Accepted sequence element | **Vector Lock** | `vector_lock` | Confirmation that one element of a Destination Vector is valid and mechanically aligned. |
| Facility operations centre | **Command Facility** | `command_facility` | The complete underground operational site. |
| Secured room containing the device | **Transit Chamber** | `transit_chamber` | The controlled physical space surrounding the Transit Array. |
| Physical defensive closure | **Containment Shutter** | `containment_shutter` | The protective barrier that isolates the Transit Link from the facility. |
| Remote reconnaissance vehicle | **Survey Drone** | `survey_drone` | The unmanned platform used to inspect a destination before dispatch. |
| Deployable personnel group | **Expedition Unit** | `expedition_unit` | A staffed operational team prepared for transit and field work. |
| Incoming identity proof | **Return Credential** | `return_credential` | The expiring challenge-response evidence used to authenticate returnees. |
| Known-site database | **Destination Registry** | `destination_registry` | The persistent record of sites, vectors, aliases, corrections, contact, and risk. |
| Main status screen | **Operations Board** | `operations_board` | The overview of active work, alarms, facility readiness, and current link state. |
| Outgoing-connection screen | **Transit Control** | `transit_control` | The console for selecting a vector, allocating power, running a Link Sequence, and aborting safely. |
| Incoming-connection screen | **Return Control** | `return_control` | The console for origin assessment, Return Credential checks, and Containment Shutter decisions. |
| Remote-data screen | **Survey Telemetry** | `survey_telemetry` | The display for drone video, atmosphere, radiation, biology, terrain, and signal confidence. |
| Personnel screen | **Expedition Roster** | `expedition_roster` | The workflow for readiness, specialties, fatigue, equipment, dispatch, and recall. |
| Engineering/status screen | **Systems Board** | `systems_board` | The view of power, cooling, alarms, faults, and repair state. |
| Pre-shift information | **Shift Brief** | `shift_brief` | The operational objectives, known risks, readiness, and constraints shown before play. |
| Post-shift report | **Shift Review** | `shift_review` | The chronological outcome, scoring, consequences, and persistent changes shown after play. |

Generic words such as `destination`, `incident`, `authentication`, `connection`, `facility`, `team member`, and `telemetry` remain acceptable when they describe ordinary concepts rather than replace an approved proper system term.

## Reference-only language

The following terms identify franchise research and must not appear as production fiction, interface labels, content identifiers, audio, or marketing claims:

- `Stargate`, `Stargate Command`, and `SGC`;
- `DHD` and franchise-specific “dialing” language;
- `iris` when used for the defensive barrier;
- `MALP` when used for the reconnaissance vehicle;
- `chevron` when used for a sequence lock; and
- franchise team designations, glyphs, symbols, names, lore, or quotations.

The repository name and “Stargate Command Simulator” remain a temporary, clearly disclaimed working title until the controlled rename in issue #12. That exception does not authorize the title for shipped product metadata.

## Usage rules

- Use the exact capitalization above in player-facing prose.
- Use the stable `snake_case` identifier in serialized content and analytics-safe local events.
- Prefer plain descriptions in tutorials, then introduce the approved proper term.
- Do not create abbreviations until the full term has appeared in the same workflow.
- Add proposed terms here before introducing them in code or content; owner approval is required to change an approved entry.

