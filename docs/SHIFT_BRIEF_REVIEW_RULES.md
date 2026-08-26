# Shift Brief and Review rules

Shift Brief (`shift_brief`) and Shift Review (`shift_review`) frame one duty shift. Presentation binds Application read models only; scoring and consequences come from recorded debrief facts plus live facility state.

## Lifecycle

1. **Briefing** — operator acknowledges `content/shift_brief.v1.json` plus prior-shift carryover lines.
2. **InProgress** — existing consoles and vertical-slice incidents run.
3. **Review** — after all five incidents resolve or fail, End shift builds a chronological timeline, transparent score, and campaign consequences.

`ShiftLifecycleService` owns the phase. Begin next shift resets the incident script, reapplies carryover effects, and returns to Briefing.

## Scoring

`ShiftScoring` counts debrief fact codes ending in `_success` vs `_failure`:

| Category | Rule |
|----------|------|
| Nominal | Zero failure facts |
| Contested | At least one failure fact, and failures fewer than successes |
| Compromised | Failure facts greater than or equal to success facts |

No hidden judgments — the category rule summary is shown on Shift Review.

## Persistence

`CampaignStateStore` writes `user://campaign_state.json` with atomic temp validation, `.bak` retention, forward migration from schema 1, and quarantine of corrupt/newer files. Recovery guidance surfaces on the Shift Brief carryover lines. See [SAVE_SCHEMA_RULES.md](SAVE_SCHEMA_RULES.md).
