# Automated tests

Tests mirror the layers beneath `src/`. Domain, application, and testable adapter checks run as ordinary headless xUnit tests. Scene integration is covered by the pinned Godot .NET import and boot smoke test. Run `dotnet test tests/WormholeWorlds.Tests.csproj` for the unit suite or `tools/Verify.ps1` for the complete CI-equivalent gate.

