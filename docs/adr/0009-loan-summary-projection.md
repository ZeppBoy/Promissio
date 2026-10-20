# ADR-0009: Loan summary consistency, rebuilds and event compatibility

**Status:** Accepted on 2026-10-20 by owner approval of the first read-model slice and its proposed policies. Documentation requires human editing before merge.

## Context

[ADR-0007](0007-persistence-and-concurrency.md) establishes event streams as the loan authority and defers projection lifecycle and schema evolution. Creation, activation and aging now persist events. Queries need a derived view without replaying every stream. Existing streams must also receive summaries when the projection is introduced.

## Decision

Implement one `LoanSummary` per loan stream, with identity, originating application, accepted terms version, original principal, disbursement and first-payment dates, current state, last recorded days past due and stream version. Original principal is not an outstanding balance. Retain the Domain's existing null and terminal-state aging semantics. Consume all existing loan events, including terminal events and the reserved payment event; a payment changes only the summary version because balances are outside this view.

Run a Marten single-stream projection **inline**, in the same PostgreSQL transaction as the append and any handoff receipt. Successful writes are immediately visible to subsequent fresh query sessions. A failed append must roll back its summary update. Application owns the query contract and read port; Infrastructure owns projection, document reads and maintenance. Commands continue loading authoritative streams.

The summary follows **stream version order**, matching current aggregate replay. Business-effective historical queries remain a separate capability under the owner's effective-date decision. Never sort current-summary events by effective date or present this summary as an as-of-date result.

Rebuild through Marten's projection daemon during an **offline maintenance window**. Stop readers, writers, batch jobs and other projection maintenance first. Replace only derived `LoanSummary` documents; preserve streams, original event payloads and handoff receipts. Backfill pre-existing streams before enabling summary queries. Resume service only after a successful rebuild and verification. Failed or cancelled rebuilds keep service in maintenance; retry the complete rebuild. This slice makes no zero-downtime rebuild guarantee and installs no background daemon or public maintenance endpoint.

Treat existing stored event names, CLR mappings and payloads as the baseline schema. Preserve them in this slice. Additive fields require explicit backward-compatible defaults and old-payload replay tests. Breaking changes require a new explicit event version and a reviewed upcaster/migration plan, with legacy deserialization, aggregate replay and projection-rebuild tests before deployment. Never rewrite historical payloads or reinterpret an existing field silently. No upcaster is needed until such a change is introduced.

## Consequences

Writes pay the small projection cost and share its failure boundary. Reads avoid aggregate replay. First deployment and subsequent projection-shape changes require a maintenance rebuild. Active/overdue lists, portfolio totals, historical queries, payment allocation, terminal command handlers and HTTP/MCP exposure remain later slices. Authorization review under ADR-0008 is still required before exposing any business operation.

## Maintenance usage

From a trusted maintenance process using `InfrastructureService.ConfigureMarten` and the intended database configuration:

```csharp
await using AsyncServiceScope scope = services.CreateAsyncScope();
LoanSummaryRebuilder rebuilder = scope.ServiceProvider.GetRequiredService<LoanSummaryRebuilder>();
await rebuilder.RebuildAsync(cancellationToken);
```

Apply configured schema changes through Marten before rebuilding. After rebuilding, compare summary state and stream version with authoritative replay for representative loans, check the original-principal/date fields, and confirm creation retries still return their original loan identities. The PostgreSQL integration suite verifies backfill, repeated rebuild equivalence, receipt preservation and resumed inline updates.

The implementation uses Marten's documented [explicit aggregation](https://martendb.io/events/projections/explicit) and [projection rebuilding](https://martendb.io/events/projections/rebuilding) APIs.
