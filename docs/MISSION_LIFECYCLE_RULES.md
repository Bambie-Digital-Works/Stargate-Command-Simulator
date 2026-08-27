# Mission lifecycle rules

Mission Control turns a dispatched Expedition Unit into a persistent field
operation. Mission definitions are authored in
`content/missions.v1.json`; code stores only stable IDs and state.

## States

```text
Available -> InField -> AwaitingReturn -> Completed
                              │              │
                              ├──────────────┘
                              ├────────────> Failed
                              └────────────> Aborted
```

An operation can start only when its Expedition Unit is dispatched through a
stable Transit Link to the definition's destination. Field progress is
deterministic and is driven by the simulation clock. The operator must
authorize the return after field objectives reach 100 percent.

Completed operations create a discovery consequence; failed or aborted
operations create a command note. These consequences are included in the next
Shift Review and are eligible for campaign persistence.

The service intentionally does not depend on Godot nodes. Presentation calls
the application service and refreshes from `MissionReadModel`, allowing the
same mission transitions to run in headless tests and replay tooling.
