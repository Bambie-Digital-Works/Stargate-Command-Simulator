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
dotnet restore WormholeWorldsSimulator.csproj
dotnet build WormholeWorldsSimulator.csproj --no-restore
```

5. Open `project.godot` in the .NET editor (or run headless import below), then Play (F5). The boot scene should show the **0.8.0-beta.1 operator shell** with the Shift Brief, six operator consoles, accessibility controls, and preview updates, and print `Boot complete: operator shell is ready.` without errors.

Always rebuild before trusting a run; stale `.godot` assemblies can fail to instantiate new C# panels.

### Codex desktop environment

The repository includes a shared Codex local environment in `.codex/environments/environment.toml`. On Windows, a new Codex worktree runs `tools/Setup-Codex.ps1`, which validates the user-wide pinned .NET SDK, installs the checksum-verified Godot editor without export templates, restores locked dependencies, and performs an initial Debug build.

The Codex toolbar exposes **Build**, **Test**, **Verify**, and **Run** actions. **Verify** is the normal non-packaging completion gate; **Run** rebuilds before launching the pinned Godot .NET editor.

Install the x64 .NET SDK version from `global.json` for the current Windows user before creating a Codex worktree. Restart Codex after installation so new terminals inherit the updated user `PATH`.

### Non-interactive smoke check

```powershell
dotnet restore WormholeWorldsSimulator.csproj
dotnet build WormholeWorldsSimulator.csproj --no-restore
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

The toolchain installers verify the pinned Godot and Inno Setup SHA-256 values before use. Verification performs locked restore, formatting checks, warning-free builds, automated tests, asset validation, Godot import, headless boot, and (unless skipped) an unsigned Windows x64 export with a build manifest and hashes. `tools/Build-Release.ps1` additionally compiles the per-user installer, portable archive, update manifest, release notes, and release-level hashes.

```powershell
$iscc = ./tools/Install-InnoSetup.ps1 -Destination ./.ci-tools/inno-setup
./tools/Build-Release.ps1 -GodotPath $godot -DotnetPath (Get-Command dotnet).Source -IsccPath $iscc
```

`WormholeWorldsSimulator.sln` is the engine-facing solution required by Godot's .NET export plugin. Auxiliary test and validation projects remain outside that solution and are invoked explicitly by the verification script.

## Foundation CI

[`.github/workflows/ci.yml`](../.github/workflows/ci.yml) runs the verification gate on pull requests, packages development artifacts on `main`, and builds a public prerelease from immutable version tags.

If GitHub returns **Actions has been disabled for this user** (HTTP 422) or the organization only allows `selected` / `local_only` actions, workflows will not start even when the repository Actions toggle shows enabled. In that case:

1. Treat `./tools/Verify.ps1` as the required merge gate.
2. Ask an organization owner to lift the account/org Actions restriction (or contact GitHub Support if the account is platform-blocked).
3. Keep `github_owned_allowed` (and any needed `actions/*` patterns) under repository Actions → General so `actions/checkout`, `actions/setup-dotnet`, `actions/cache`, and `actions/upload-artifact` remain permitted.

## Local configuration and diagnostics

`config/defaults.json` contains the required, versioned defaults. A developer may create `user://settings.json` with schema version 1 and partial `logging`, `diagnostics`, or `simulation` overrides. Unknown fields and invalid ranges stop startup with a field-level message so configuration mistakes cannot silently change behaviour.

The same configuration document tracks prototype simulation timeouts and facility power/cooling capacity. User overrides may provide a partial `simulation` object; all durations and capacity values are range-checked before startup.

Local structured logs use JSON Lines under `user://logs`. Diagnostics expose build and runtime identity without showing usernames, credentials, or sensitive paths. Logging and diagnostics are local only; the project sends no telemetry.

Input bindings are rebuilt from semantic actions at startup and stored atomically in `user://input_bindings.v1.json`. Invalid or future-schema files are preserved with a `.corrupt-<UTC timestamp>.json` suffix while the game safely restores defaults. The in-app Input settings panel supports keyboard/mouse and controller rebinding, clearing optional bindings, and restoring defaults.

Accessibility preferences are stored atomically in `user://accessibility_settings.v1.json`. Preview update checks store only their last successful UTC check time, query the public GitHub Releases API at most once per 24 hours, and send no telemetry.
