# Save schema rules

Player progress for the vertical slice is the campaign consequence document written after Shift Review. Save schema is independent of product SemVer and is recorded as assembly `SaveSchemaVersion`.

## Current document (save schema 2)

Primary path: `user://campaign_state.json`

Fields:

- `saveSchemaVersion` (required) — integer; current value `2`
- `writtenAtUtc` — ISO-8601 timestamp added in schema 2
- `consequences` — list of `{ code, kind, summary, relatedEntityId? }`
- `lastOutcomeCategory` / `lastCategoryRuleSummary` — prior Shift Review outcome

Legacy schema 1 used `schemaVersion` instead of `saveSchemaVersion` and omitted `writtenAtUtc`.

## Validation and atomic write

Save documents reject unknown fields, invalid schema versions, malformed consequence entries, and invalid `writtenAtUtc` values before they can replace the primary file.

1. Validate serialized document in memory
2. Write `campaign_state.json.tmp`
3. Re-read and validate the temporary file
4. Copy existing primary to `campaign_state.json.bak` when present
5. Atomically replace primary via `File.Move`

An interrupted write before step 5 leaves the last valid primary intact.

## Load, migrate, reject

| Condition | Behavior |
|-----------|----------|
| Missing file | Empty campaign (defaults) |
| Corrupt / invalid | Quarantine as `.corrupt-<utc>.json`, empty campaign, recovery guidance on Shift Brief |
| Schema newer than build | Quarantine as `.newer-<utc>.json`, empty campaign, guidance to update client or restore backup |
| Schema older than build | Copy `.pre-migrate-v{n}.json`, forward-migrate, validate, rewrite current schema |

Forward-only migrations. This build does not write older formats.

## Scope

Campaign carryover is covered here. A full mid-shift simulation envelope (transit, survey, incidents, roster pool) remains future work. Settings and input bindings keep their existing stores.
