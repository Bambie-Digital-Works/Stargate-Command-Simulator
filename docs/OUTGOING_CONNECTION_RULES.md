# Outgoing connection rules

Outgoing connections use schema-versioned Destination Registry records from `content/destinations.v1.json`. Each record has a stable ID, an ordered Destination Vector containing four to eight unique `snake_case` elements, and positive power/cooling requirements.

Preparation validates the destination and the complete vector before atomically reserving both resources. A Link Sequence accepts only the next expected Vector Lock. Stabilization is unavailable until every element is locked, and a stable link requires the reservation to remain intact.

Unscheduled incoming detection is a separate Standby action: it reserves a fixed prototype budget, skips Vector Locks, and stabilizes from `IncomingDetected`. See the [Transit Array state machine](TRANSIT_ARRAY_STATE_MACHINE.md) unscheduled-incoming section.

Invalid destinations, vectors, locks, and commands do not mutate phase or resources. Abort, timeout, fault, completed closure, and safe recovery release the connection's reservation exactly once. Normal closure retains resources until the link has physically closed, then releases them before cooldown.

Prototype timing and capacity values live in strict tracked configuration. They are deterministic engineering defaults rather than final balance.
