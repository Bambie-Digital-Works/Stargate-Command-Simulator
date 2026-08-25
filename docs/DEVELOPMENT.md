# Development setup

## Pinned toolchain

- Godot `4.7.2-stable`, .NET-enabled Windows x86-64 editor and matching .NET export templates
- .NET SDK `8.0.424` x86-64
- C# `12`

The .NET SDK selection is recorded in `global.json`; the Godot SDK is pinned in the project file. Patch upgrades should change this document, `global.json`, the project SDK, and `project.godot` in one reviewed change.

## First run

1. Install the pinned x86-64 .NET SDK.
2. Download the pinned Godot .NET editor and matching .NET export templates.
3. Confirm `dotnet --version` prints `8.0.424` and `Godot_v4.7.2-stable_mono_win64.exe --version` reports `4.7.2.stable.mono`.
4. Open `project.godot` in the .NET editor and allow the initial NuGet restore.
5. Run the project. The boot scene should show the empty operator shell and print `Boot complete: operator shell is ready.` without errors.

From a developer shell where the Godot executable is available as `godot`, the non-interactive checks are:

```powershell
dotnet restore FacilityCommand.csproj
dotnet build FacilityCommand.csproj --no-restore
godot --headless --path . --editor --build-solutions --quit
godot --headless --path . --quit-after 2
```

Godot imports project resources and builds the managed assembly during the editor check. Its `.godot/` cache and all `bin/` and `obj/` output remain local and are ignored by Git.

The complete local CI-equivalent entry point is:

```powershell
$godot = ./tools/Install-Godot.ps1 -Destination ./.ci-tools/godot -IncludeExportTemplates
./tools/Verify.ps1 -GodotPath $godot
```

The installer verifies the pinned editor and export-template SHA-256 values before extraction. Verification performs locked restore, formatting checks, warning-free builds, automated tests, asset validation, Godot import, headless boot, and an unsigned Windows x64 export with a build manifest and hashes.

`FacilityCommand.sln` is the engine-facing solution required by Godot's .NET export plugin. Auxiliary test and validation projects remain outside that solution and are invoked explicitly by the verification script.

## Local configuration and diagnostics

`config/defaults.json` contains the required, versioned defaults. A developer may create `user://settings.json` with schema version 1 and partial `logging`, `diagnostics`, or `simulation` overrides. Unknown fields and invalid ranges stop startup with a field-level message so configuration mistakes cannot silently change behaviour.

The same configuration document tracks prototype simulation timeouts and facility power/cooling capacity. User overrides may provide a partial `simulation` object; all durations and capacity values are range-checked before startup.

Local structured logs use JSON Lines under `user://logs`. Diagnostics expose build and runtime identity without showing usernames, credentials, or sensitive paths. Logging and diagnostics are local only; the project sends no telemetry.

Input bindings are rebuilt from semantic actions at startup and stored atomically in `user://input_bindings.v1.json`. Invalid or future-schema files are preserved with a `.corrupt-<UTC timestamp>.json` suffix while the game safely restores defaults. The in-app Input settings panel supports keyboard/mouse and controller rebinding, clearing optional bindings, and restoring defaults.
