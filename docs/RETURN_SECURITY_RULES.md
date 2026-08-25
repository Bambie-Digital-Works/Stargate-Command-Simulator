# Return security and containment rules

Return security is deterministic Core state. It consumes simulation timestamps and explicit credential evidence; it never reads wall-clock time, platform identity, or free-form operator data.

## Credential assessment

A Return Credential is authorized only when its reported state is `Verified`, its challenge response exactly matches its recorded proof, it has not expired in simulation time, and its stable credential ID has not already been accepted. Each attempt emits an ordered immutable audit event.

| State | Outcome | Containment effect |
|---|---|---|
| Verified | Authorized | May open while a stable Transit Link is active |
| Missing, damaged, expired, duplicate | Withheld | Remains closed; operator receives a corrective action |
| Duress | Security alert | Remains closed; begins the duress response |
| Spoof suspected | Security alert | Remains closed; escalates identity challenge |

An accepted credential ID is single-use for the verifier lifetime. Reuse is deterministically classified as duplicate. This is a simulation rule, not production cryptography.

## Containment Shutter interlocks

The shutter progresses through `Closed`, `Opening`, `Open`, `Closing`, and `Faulted`. Opening requires both an active `LinkOpen` Transit Array snapshot and a currently authorized credential assessment. Missing authorization and every rejected/security-alert outcome are fail-secure rejections.

A reported shutter fault enters `Faulted`, which is treated as secured. No opening command is accepted until an explicit safe reset returns it to `Closed`. Commands and results contain stable reason codes and corrective text so presentation code can explain every rejection without reproducing Core rules.
