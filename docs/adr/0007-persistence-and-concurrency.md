# ADR-0007: Persistence authority and concurrency

**Status:** Proposed. Architecture review required before implementing persistence.

## Context

Marten and EF Core are locked stack choices, but their data ownership and transaction boundary are unresolved. The old API plan's row-version requirement did not distinguish event streams from relational records.

## Proposed decision

Use Marten event streams as the authority for event-sourced aggregate state. Treat projections as derived, rebuildable query data. Use EF Core only for independently owned relational data outside those streams; do not update a projection as an alternate source of loan state.

Application defines persistence ports and workflow transaction requirements. Infrastructure implements them. Protect aggregate writes with expected stream versions; use concurrency tokens for independently owned relational records where applicable.

Persist idempotency outcomes atomically with the authoritative state change they protect. Avoid an uncoordinated EF write plus Marten append. Where one transaction cannot cover the workflow, review an outbox/inbox design before implementation.

## Questions to resolve

- Which aggregates are event sourced, and which facts are relational?
- Which projections must support immediate read-after-write, and which may lag?
- What is the transaction scope of payment recording and origination handoff?
- Where are idempotency keys, request fingerprints and replayed responses stored?
- How are retries, projection rebuilds and event-schema changes tested?

## Consequences

A single authority per fact prevents conflicting balances. The actual schema, isolation level, projection lifecycle and failure semantics remain unapproved until the questions above are resolved. This record does not introduce repositories or migrations.
