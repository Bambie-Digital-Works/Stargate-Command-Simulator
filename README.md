# Wormhole Worlds Simulator

> Wormhole Worlds Simulator is an original, independent game project. It is not affiliated with, endorsed by, sponsored by, or licensed by Amazon, Metro-Goldwyn-Mayer, or any other owner of the Stargate franchise.

Wormhole Worlds Simulator is a Windows command-centre simulation game. The player operates an underground interstellar transit facility: scheduling Expedition Units, establishing Transit Links, reviewing Survey Drone telemetry, authenticating returnees, controlling the Containment Shutter, and responding to overlapping technical, medical, and security incidents.

The project is at **v0.8.0-beta.1**. This first beta contains the focused, UI-first vertical slice rather than an explorable 3D base.

## Core gameplay loop

1. Review the shift briefing, team readiness, facility status, and known destinations.
2. Select and verify a Destination Vector, allocate power, and establish a Transit Link.
3. Examine Survey Drone telemetry before committing personnel.
4. Authenticate incoming travellers and make time-critical containment decisions.
5. Resolve equipment failures, security alerts, injuries, and incomplete intelligence.
6. Debrief the shift and carry its discoveries and consequences into later missions.

## Product pillars

- **Information under pressure:** important decisions must be made from incomplete, sometimes conflicting data.
- **Systems with consequences:** power, cooling, authentication, containment, personnel, and mission systems affect one another.
- **Readable operator interfaces:** the console is the play space, with strong hierarchy and restrained alarm design.
- **Persistent command:** destinations, staff condition, discoveries, repairs, and prior decisions survive between shifts.
- **Original science-fiction identity:** final terminology, symbols, lore, interfaces, characters, art, and audio will be original.
- **Accessible by design:** scalable UI, captions, remappable controls, colour-safe alerts, pause, and adjustable time pressure are baseline requirements.

## Technology and targets

- Godot 4 with C#/.NET
- Windows-first desktop release
- Keyboard and mouse, with controller support
- Target shift length: 20–35 minutes
- Semantic product versions with immutable CI build identifiers

## Roadmap

| Version | Target |
|---|---|
| `0.1.0` | Core Transit Array state model and operator-console prototype |
| `0.3.0` | Polished vertical slice with five incidents |
| `0.8.0-beta.1` | Current Windows beta, installer, preview updates, saves, and accessibility baseline |
| `1.0.0` | Stable original-IP commercial release target |

See the [project overview](docs/PROJECT_OVERVIEW.md) for the vertical-slice definition, the [production terminology guide](docs/TERMINOLOGY.md) for approved names, and the [research and production brief](docs/RESEARCH.md) for design references, comparable games, visual-source guidance, and Windows release practices.

Developers should start with the [architecture](docs/ARCHITECTURE.md) and [development setup](docs/DEVELOPMENT.md) documents. The repository pins the Godot .NET and .NET SDK versions needed to open and build the project.

All retained visual and audio material follows the [asset policy](docs/ASSET_POLICY.md) and must pass the version-controlled licence-ledger validation before distribution.

## Ownership and contributions

Copyright © 2026 `bambiejomurphy-ux`. Original project materials are proprietary and all rights are reserved. Public visibility of this repository does not make its contents open source and does not grant permission to copy, modify, redistribute, sublicense, or commercially exploit them. See [LICENSE](LICENSE) and [NOTICE](NOTICE.md) for the precise scope and third-party exclusions.

Issue feedback is welcome, but unsolicited source code, pull requests, art, audio, writing, designs, and other creative contributions are not accepted. See [CONTRIBUTING.md](CONTRIBUTING.md).

## Status

The **v0.8.0-beta.1** build includes the complete five-incident vertical slice, Systems Board, accessibility settings, preview update checks, and Windows installer/release tooling. Download published builds from [GitHub Releases](https://github.com/Bambie-Digital-Works/Stargate-Command-Simulator/releases), or follow the [development setup](docs/DEVELOPMENT.md) to build and verify locally.

The beta is currently unsigned, so Windows SmartScreen may warn. Verify the installer against the published `SHA256SUMS.txt` before running it.
