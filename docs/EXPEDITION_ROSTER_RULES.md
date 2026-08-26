# Expedition Roster rules

Expedition Roster state is deterministic Core state. It tracks one Expedition Unit, assigned personnel, issued kit, and dispatch status. Presentation binds read models only; it never bypasses assemble, equip, or dispatch guards.

## Assemble

A roster is valid only when assigned staff cover commander, medic, engineer, and security specialties. Injured, unavailable, or high-fatigue (80+) staff are rejected with corrective text. Duplicate assignments are rejected.

## Equip and dispatch

Equipping requires the approved kit (`medkit`, `comm_pack`, `survey_kit`, `sidearm`). Dispatch requires an equipped unit, an active `LinkOpen` Transit Array snapshot, and a recorded Survey Telemetry risk decision (see [survey telemetry rules](SURVEY_TELEMETRY_RULES.md)). Recall returns a dispatched unit to Standby and clears assignments.

## Persistence stub

`ExpeditionUnitSnapshot` is the portable roster identity. Application code can serialize and restore that snapshot so Operations Board summaries stay consistent across screens and future save/load work.
