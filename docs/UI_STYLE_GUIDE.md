# Operator console UI and audio kit

The vertical slice uses an original, text-forward command-console language.
Scenes own layout and focus order, while each board exposes a stable status
summary and action state to the operator shell.

## Shared tokens

| Token | Value | Use |
| --- | --- | --- |
| Console background | `#05090C` | Full-window background |
| Primary signal | `#49C0D3` | Navigation, headings, neutral telemetry |
| Operator text | `#E2EBED` | Body and action labels |
| Secondary text | `#95A8AD` | Hints and supporting information |
| Caution | `#FACA60` | Warnings and time pressure |
| Critical | `#F47D72` | Active alarms and failed actions |
| Confirmed | `#80D89B` | Accepted actions and successful facts |

Colour is never the only state cue. Alerts include a severity word, an
identifier or message, a dedicated status placement, and an alarm cue.

## Alert hierarchy

| Level | Text and shape cue | Sound | Placement |
| --- | --- | --- | --- |
| Information | `Information:` prefix and neutral label | None or interface tone | Board status |
| Warning | `Warning:` prefix and caution label | Low alarm tone | Shell status and board |
| Critical | `[Critical]` severity plus alarm code | Alarm tone | Active alarms panel and shell status |
| Confirmed | `Accepted.` prefix and confirmed label | Interface tone | Action result status |

The operations board renders active alarms as `[Severity] Code: Message`.
Return-control warnings and shell announcements use explicit prefixes, so
players can distinguish states without colour vision.

## Audio and captions

The beta intentionally retains no third-party audio files. Interface and alarm
cues are generated from short original tones at runtime, avoiding an
unverifiable binary asset. Every alarm cue also emits a caption beginning with
`[Alarm tone]` and the complete text announcement. Interface volume and alarm
volume are independently adjustable, and captions can be disabled or enabled
in Accessibility.

## Screen coverage and accessibility

Operations, Transit, Survey, Return, Expedition, Systems, Shift Brief, Shift
Review, Updates, Diagnostics, Input, and Accessibility are separate reusable
scenes. The shell provides keyboard, mouse, and controller focus traversal,
pause, UI scaling, high contrast, reduced flashing, remapping, captions, and
adjustable time pressure. Accessibility preferences are validated, persisted,
and safely reset through the Accessibility panel.

Production and reference assets remain governed by `assets/asset-ledger.json`
and `docs/ASSET_POLICY.md`. The validator must pass before a distributable
artifact is created.
