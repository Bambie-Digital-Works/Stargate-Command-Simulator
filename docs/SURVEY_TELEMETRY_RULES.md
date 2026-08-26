# Survey Telemetry rules

Survey Telemetry is deterministic Core state backed by versioned destination profiles in `content/survey_profiles.v1.json`. Presentation binds read models only; deployment, delayed resolution, and risk decisions never bypass Core guards.

## Deployment

Deploying the Survey Drone requires:

- an active `LinkOpen` Transit Array;
- a Destination Registry id with a matching Survey Telemetry profile; and
- `survey_kit` already issued on an Equipped (or Dispatched) Expedition Unit.

Unscheduled incoming links without a destination profile cannot deploy.

## Telemetry qualities

Profiles may mark individual channels as clear, delayed, missing, noisy, or contradictory. Contradictory readings carry corrective text and raise an Operations Board warning until a risk decision is recorded. Delayed profiles start in `Deployed` until the simulation clock reaches `deployDelayMs`, then become `TelemetryReady`.

## Risk decision and dispatch interlock

Recording Acceptable, Elevated, or Unacceptable risk is available only after telemetry is ready. Expedition Unit dispatch requires that recorded decision in addition to an Equipped roster and a stable Transit Link. Closing or leaving `LinkOpen` resets Survey Drone state.
