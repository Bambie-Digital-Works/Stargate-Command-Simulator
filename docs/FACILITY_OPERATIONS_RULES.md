# Facility operations rules

The Command Facility is managed as a set of independently observable systems,
secured zones, intelligence records, faction relationships, and concurrent
incidents. The application layer owns these workflows; Godot only presents
their read models.

## Engineering

Subsystem faults reduce health and enter a repair queue. A queued repair
advances in deterministic steps and restores the subsystem only when the queue
reaches zero. Faults and completions remain in the operator alert history.

## Security

Zones can be locked down or reopened with an operator reason. Lockdown state is
visible in the facility read model and is intended to gate future transit,
quarantine, and response actions.

## Intelligence and diplomacy

Destination records store confidence, the latest report, and deduplicated
signal observations. Faction trust is bounded from -100 to 100 and every
contact updates the latest relationship summary.

## Concurrent incidents

The incident director accepts scheduled incidents with activation times,
deadlines, locations, and priorities. Active incidents are ordered by priority
and deadline; overdue incidents are removed and recorded so the Shift Review
can explain missed work.
