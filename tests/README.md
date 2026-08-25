# Automated tests

Tests mirror the layers beneath `src/`. Domain and application tests should be ordinary headless .NET tests; scene and integration tests may use the pinned Godot .NET runtime. Test infrastructure and the CI entry point are introduced by their dedicated backlog tasks.

