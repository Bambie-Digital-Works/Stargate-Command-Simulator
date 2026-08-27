# Repository guidance for Codex

## Project and toolchain

- This is a Windows-first Godot 4 C#/.NET game. Use the versions pinned by `global.json`, `WormholeWorldsSimulator.csproj`, and `project.godot`; do not upgrade them independently.
- Run repository commands from the repository root with PowerShell.
- Prefer the scripts in `tools/` over duplicating build, validation, Godot-installation, or packaging logic.
- Always rebuild managed assemblies before trusting a Godot run; stale `.godot` assemblies can prevent C# scenes from loading.

## Architecture

- Preserve the dependency direction `Presentation -> Application -> Core`, with Infrastructure implementing inward-facing ports and Bootstrap as the only composition root.
- Keep simulation rules deterministic and independent of Godot nodes, scenes, wall clocks, random generators, storage, and presentation state.
- Scenes own layout, visual state, focus order, and signal wiring. They may call application use cases but must not mutate domain state directly.
- Namespace segments mirror folders under `src/`, rooted at `WormholeWorlds`. Use one public C# type per matching file, nullable reference types, C# 12, and warning-free builds.
- Record a new architectural exception in `docs/decisions/` and update the relevant architecture or rules document in the same change.

## Product, terminology, and assets

- Maintain the project's original science-fiction identity. Use approved production terminology from `docs/TERMINOLOGY.md`; do not introduce franchise names, symbols, lore, characters, art, audio, screenshots, or extracted assets.
- Follow `docs/ASSET_POLICY.md`. Every retained visual or audio asset needs acceptable provenance and a valid licence-ledger entry before distribution.
- Preserve accessibility requirements: scalable UI, captions for audio-only information, remappable controls, colour-safe alerts, pause support, and adjustable time pressure.
- Treat the repository as proprietary. Do not incorporate unsolicited third-party code or creative material.

## Verification

- For focused C# changes, run the affected tests and a warning-free build.
- For content or asset changes, also run the relevant validators.
- Before declaring a material implementation complete, run the local CI-equivalent gate:

```powershell
$godot = ./tools/Install-Godot.ps1 -Destination ./.ci-tools/godot
./tools/Verify.ps1 -GodotPath $godot -SkipExport
```

- Packaging is not part of routine Codex verification. Install export templates and omit `-SkipExport` only when the task explicitly includes a Windows package.
