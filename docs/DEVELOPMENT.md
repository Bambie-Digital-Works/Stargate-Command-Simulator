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
dotnet restore StargateCommandSimulator.csproj
dotnet build StargateCommandSimulator.csproj --no-restore
godot --headless --path . --editor --build-solutions --quit
godot --headless --path . --quit-after 2
```

Godot imports project resources and builds the managed assembly during the editor check. Its `.godot/` cache and all `bin/` and `obj/` output remain local and are ignored by Git.
