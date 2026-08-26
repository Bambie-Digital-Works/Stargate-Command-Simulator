# Incident script rules

Vertical-slice incidents are deterministic Core scripts authored in `content/incidents.v1.json`. Application evaluates live console state and records debrief facts; Presentation only shows progress and does not invent outcomes.

## Sequence

The five slice incidents run in fixed `sequenceOrder`:

1. `routine_reconnaissance`
2. `credential_damage`
3. `spoofed_incoming`
4. `survey_contradiction`
5. `medical_cooling_fault`

Only one incident is active at a time. Success or failure advances to the next script. Collected debrief facts remain available for Shift Review (#27).

## Events

Each definition declares a `successEvent` and `failureEvent`. `ShiftIncidentService.Evaluate` maps operator outcomes (dispatch, damaged credential, spoof alert, contradictory risk decision, cooling-fault clear) onto those events. Headless tests may call `Observe` directly.

## Side effects

When `medical_cooling_fault` becomes active, Application marks an Expedition Unit member injured and applies a cooling-capacity fault hold on `FacilityResourcePool`. Clearing the fault (Operations Board) completes the incident when injured staff are present.

## Seed

Prototype progression is sequential rather than randomly selected. `SeededIncidentSelector` remains available for future branch picks; identical operator event sequences remain reproducible under the shared simulation clock.
