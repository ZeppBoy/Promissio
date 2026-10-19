# ADR-0009: Current loan-state projection and event compatibility

**Status:** Accepted for the first Phase 3 read model on 2026-10-19.

## Context

ADR-0007 makes the Marten event stream authoritative and leaves projection consistency, rebuilds, and event-schema evolution to the ticket that introduces a projection. The first query needs the current state of one loan without loading its entire stream. Callers need to see a successfully saved state change immediately.

## Decision

Project one `LoanStateDocument` per loan stream, keyed by `LoanId`. It contains only the current `LoanState` and nullable `DaysPastDue`. Update it inline in the same Marten transaction that appends events. The Application query reads this derived document. Commands and conflict responses continue to use authoritative stream state, never the projection.

The projection is disposable query data. Rebuild the named `LoanState` projection from the complete loan event history with Marten's projection daemon; do not edit projected rows to change loan state. A rebuild must reproduce the current view, including cures and repeated past-due aging. Rebuilds are an explicit maintenance operation, not part of ordinary command handling. Existing installations must complete a rebuild before serving this query for pre-existing loan streams.

Persisted event types and their required fields remain readable. Do not rename, remove, or reinterpret a field in an existing stored event. An incompatible change requires a new versioned event type and projection/replay handling for both old and new types, with PostgreSQL rebuild tests before deployment. Historical events are not rewritten in place. This rule sets compatibility expectations; no event schema changes are made by this ticket.

## Consequences

Successful writes have immediate read-after-write visibility for this query. Inline projection work adds cost to each append and may reject an append if the projection fails, keeping the stream and view in sync. Rebuilds need operational coordination with writers and readers when the service is deployed. The current query reports stream-order state; business-effective historical queries remain a separate Phase 3 task.

The projection does not contain a balance, schedule, customer data, or portfolio totals. Other read models can be added later with their own query contracts and consistency decisions.
