# Development setup

## Pinned toolchain

- Godot `4.7.2-stable`, .NET-enabled Windows x86-64 editor and matching .NET export templates
- .NET SDK `8.0.424` x86-64
- C# `12`

The .NET SDK selection is recorded in `global.json`; the Godot SDK is pinned in the project file. Patch upgrades should change this document, `global.json`, the project SDK, and `project.godot` in one reviewed change.

## First run

1. Install the pinned x86-64 .NET SDK (`8.0.424`).
2. From the repository root, install the checksum-verified Godot .NET editor:

```powershell
$godot = ./tools/Install-Godot.ps1 -Destination ./.ci-tools/godot
```

   Add `-IncludeExportTemplates` when you also need Windows packaging.

3. Confirm `dotnet --version` prints `8.0.424` and `& $godot --version` reports `4.7.2.stable.mono`.
4. Restore and rebuild managed assemblies after every pull or C# change:

```powershell
dotnet restore FacilityCommand.csproj
dotnet build FacilityCommand.csproj --no-restore
```

5. Open `project.godot` in the .NET editor (or run headless import below), then Play (F5). The boot scene should show the **0.1.0 operator shell** (Operations Board with Transit Control, Return Control, and Expedition Roster) and print `Boot complete: operator shell is ready.` without errors.

Always rebuild before trusting a run; stale `.godot` assemblies can fail to instantiate new C# panels.

### Non-interactive smoke check

```powershell
dotnet restore FacilityCommand.csproj
dotnet build FacilityCommand.csproj --no-restore
& $godot --headless --path . --editor --quit
& $godot --headless --path . --quit-after 2
```

Godot imports project resources and builds the managed assembly during the editor check. Its `.godot/` cache and all `bin/` and `obj/` output remain local and are ignored by Git.

### Local CI-equivalent gate

```powershell
$godot = ./tools/Install-Godot.ps1 -Destination ./.ci-tools/godot -IncludeExportTemplates
./tools/Verify.ps1 -GodotPath $godot
```

Skip packaging with `./tools/Verify.ps1 -GodotPath $godot -SkipExport`.

The installer verifies the pinned editor and export-template SHA-256 values before extraction. Verification performs locked restore, formatting checks, warning-free builds, automated tests, asset validation, Godot import, headless boot, and (unless skipped) an unsigned Windows x64 export with a build manifest and hashes.

`FacilityCommand.sln` is the engine-facing solution required by Godot's .NET export plugin. Auxiliary test and validation projects remain outside that solution and are invoked explicitly by the verification script.

## Foundation CI

[`.github/workflows/ci.yml`](../.github/workflows/ci.yml) runs the same `Install-Godot.ps1` + `Verify.ps1` gate on pull requests (`-SkipExport`) and packages on `main`.

If GitHub returns **Actions has been disabled for this user** (HTTP 422) or the organization only allows `selected` / `local_only` actions, workflows will not start even when the repository Actions toggle shows enabled. In that case:

1. Treat `./tools/Verify.ps1` as the required merge gate.
2. Ask an organization owner to lift the account/org Actions restriction (or contact GitHub Support if the account is platform-blocked).
3. Keep `github_owned_allowed` (and any needed `actions/*` patterns) under repository Actions → General so `actions/checkout`, `actions/setup-dotnet`, `actions/cache`, and `actions/upload-artifact` remain permitted.

## Local configuration and diagnostics

`config/defaults.json` contains the required, versioned defaults. A developer may create `user://settings.json` with schema version 1 and partial `logging`, `diagnostics`, or `simulation` overrides. Unknown fields and invalid ranges stop startup with a field-level message so configuration mistakes cannot silently change behaviour.

The same configuration document tracks prototype simulation timeouts and facility power/cooling capacity. User overrides may provide a partial `simulation` object; all durations and capacity values are range-checked before startup.

Local structured logs use JSON Lines under `user://logs`. Diagnostics expose build and runtime identity without showing usernames, credentials, or sensitive paths. Logging and diagnostics are local only; the project sends no telemetry.

Input bindings are rebuilt from semantic actions at startup and stored atomically in `user://input_bindings.v1.json`. Invalid or future-schema files are preserved with a `.corrupt-<UTC timestamp>.json` suffix while the game safely restores defaults. The in-app Input settings panel supports keyboard/mouse and controller rebinding, clearing optional bindings, and restoring defaults.
