# Core layer

Deterministic simulation state, commands, events, value objects, and rules live here. Code in this layer must remain independent of Godot and every outer project layer so it can run in fast, headless tests.

The Transit Array aggregate accepts explicit commands and simulation timestamps, emits immutable events, and follows the authoritative transition table in `docs/TRANSIT_ARRAY_STATE_MACHINE.md`.

