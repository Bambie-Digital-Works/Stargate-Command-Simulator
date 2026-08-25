# Project architecture

## Decision

The game uses a layered, UI-first architecture. Simulation rules are ordinary C# and do not depend on Godot nodes, scenes, clocks, random-number generators, storage, or presentation state. Godot is the composition and presentation host.

Dependency direction is inward:

```text
Presentation ──> Application ──> Core
      │                ▲
      └──> Infrastructure ┘

Bootstrap composes all layers and starts the first scene.
```

`Core` must not reference another project layer or Godot. `Application` coordinates use cases through interfaces owned by the inner layer. `Infrastructure` implements persistence, clocks, randomness, platform services, and other external concerns. `Presentation` translates input and view state; it does not own simulation rules. `Bootstrap` is the only composition root.

## Repository layout

| Path | Responsibility |
|---|---|
| `src/Core/` | Deterministic domain state, commands, events, and rules |
| `src/Application/` | Use-case orchestration and ports required from external systems |
| `src/Infrastructure/` | Godot/platform adapters, storage, logging, and configuration |
| `src/Presentation/` | Screen controllers, view models, and reusable UI behaviour |
| `src/Bootstrap/` | Startup, dependency composition, and top-level navigation |
| `scenes/` | Godot scenes, grouped by feature or shared UI role |
| `content/` | Versioned authored game data; no executable code |
| `tests/` | Automated tests mirroring the source-layer structure |
| `tools/` | Developer and CI utilities that are not shipped with the game |
| `assets/reference/` | Research-only material excluded from distributable builds |

## Scene ownership

`project.godot` starts `scenes/boot/Boot.tscn`. Its `Boot` script is the composition root and instantiates the operator shell. Future screens are children of a navigation host within the shell; they communicate with application services instead of locating one another through the scene tree.

Scenes own layout, visual state, focus order, and signal wiring. A scene may call an application use case, but it must not mutate domain state directly. Domain events return through a presentation-facing adapter so that the same simulation can run headlessly in tests.

## Conventions

- Namespace segments mirror folders beneath `src/`, rooted at `FacilityCommand`.
- One public C# type per file; the filename matches the type name.
- Godot node scripts use `partial` classes as required by the engine.
- Content uses stable identifiers rather than scene paths or display names.
- Reference assets never appear beneath production `content/` or future distributable `assets/` paths.
- `assets/reference/.gdignore` keeps research-only material outside Godot's import and export pipeline.
- `tools/.gdignore` and `tests/.gdignore` keep auxiliary C# projects outside Godot's resource and export pipeline.
- New architectural exceptions require a short decision record in `docs/decisions/`.

The deterministic Transit Array phase and safety rules are specified in [the state-machine document](TRANSIT_ARRAY_STATE_MACHINE.md). Core transitions receive explicit simulation timestamps and never read wall-clock time.

Destination validation, sequential locks, and atomic power/cooling behavior are specified in [the outgoing-connection rules](OUTGOING_CONNECTION_RULES.md).

Clock, seeded randomness, and privacy-safe replay contracts are specified in [the determinism and replay document](DETERMINISM_AND_REPLAYS.md).
