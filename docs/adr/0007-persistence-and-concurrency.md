# ADR-0007: Persistence authority and concurrency

**Status:** Accepted for Phase 3 T2 on 2026-09-16. Projection lifecycle and event-schema evolution require follow-up decisions before T6.

## Context

Marten and EF Core are locked stack choices, but their data ownership and transaction boundary are unresolved. The old API plan's row-version requirement did not distinguish event streams from relational records.

## Decision

Use Marten event streams as the authority for event-sourced aggregate state. Treat projections as derived, rebuildable query data. Use EF Core only for independently owned relational data outside those streams; do not update a projection as an alternate source of loan state.

Application defines persistence ports and workflow transaction requirements. Infrastructure implements them. Protect aggregate writes with expected stream versions; use concurrency tokens for independently owned relational records where applicable.

Persist idempotency outcomes atomically with the authoritative state change they protect. Avoid an uncoordinated EF write plus Marten append. Where one transaction cannot cover the workflow, review an outbox/inbox design before implementation.

For the T2 creation/replay slice:

- `Loan` is event sourced. Its Marten stream id is `LoanId`.
- A Marten document keyed by `LoanApplicationId` records the handoff result. It is inserted in the same Marten session and PostgreSQL transaction as `LoanCreated`.
- An identical retry returns the recorded loan identity. A retry whose immutable terms or disbursement details differ returns a typed conflict.
- Loan creation uses `StartStream`, which enforces the version-zero/non-existent-stream precondition. Subsequent aggregate writes must use an expected stream version.
- Command responses are produced from authoritative stream state. Query projections are derived and may lag unless a later workflow explicitly requires immediate read-after-write behavior.
- EF Core is not used in this transaction. Any future workflow that cannot remain inside one PostgreSQL transaction requires a reviewed outbox/inbox decision.

Projection rebuild policy, payment transaction scope, and event-schema evolution remain deferred to the tickets that introduce those capabilities. They do not change the accepted single-authority rule.

## Consequences

A single authority per fact prevents conflicting balances. T2 introduces the Application persistence port, Marten implementation, atomic handoff receipt and replay tests. It does not introduce query projections, EF Core ownership, payment persistence or event-schema migrations.
