# Delivery status

Working-tree assessment: 2026-10-19, including the current-state projection. [Phase 2 completion evidence](verification/2026-09-18-phase-2-completion.md) records its checks and sign-off; [Phase 3 aging evidence](verification/2026-09-18-phase-3-aging.md) and [projection evidence](verification/2026-10-19-phase-3-loan-state-projection.md) record persistence verification.

Statuses are separate: **planned** means intended scope, **implemented** means code exists, **verified** requires recorded checks, and **reviewed** requires human review. Passing unit tests does not establish complete phase acceptance or financial compliance.

| Area | Implementation | Verification / review |
|---|---|---|
| Phase 0 foundation | Projects, dependency management, CI and runnable host shells present | Full solution build restored with zero warnings and errors |
| Phase 1 interest engine | Domain primitives, day counts and rate types present | Included in the passing whole-Domain coverage and mutation gates |
| Phase 2 schedules / APRC | **Complete** | 528 Domain tests pass; whole-Domain line coverage 96.48% and mutation score 85.58%; Phase 2 line coverage 98.51%; owner financial-contract sign-off recorded 2026-09-18 |
| Phase 3 loan lifecycle | Aggregate/state machine, idempotent creation/replay, persisted activation and aging with optimistic concurrency; inline current-state projection and query | Sixteen PostgreSQL Testcontainers scenarios pass; terminal command workflows, active/overdue/portfolio projections and historical queries remain |
| Phase 4 APIs | Host and application scaffolding | No endpoint or real PostgreSQL workflow verification |
| Phase 5 batch | Runnable generic host only | Registration and lifecycle smoke tests; no idempotency evidence |
| Phases 6–8 MCP / AI / evaluations | Host scaffolding and plans | No tools, agents, golden datasets or evaluation thresholds exercised |
| Phases 9–10 polish / launch | Planned; benchmark harnesses exist | No current performance baseline or launch readiness claim |

The Application test project covers creation, activation, aging and current-state query handlers. Integration contains passing real PostgreSQL scenarios for creation, replay, duplicate handoff, changed-payload conflict, concurrent handoffs, persisted activation, grace, past due, cure, default, the Active no-op, stale-writer rejection, immediate current-state reads, terminal event projection and projection rebuild. On 2026-10-19 the solution test run passed 566 tests across five test assemblies; AI evaluations remain empty.

Owner sequencing decision, 2026-09-10: Phase 3 preparation proceeded while Phase 2 assurance was deferred. That deferred Phase 2 implementation work is now complete without changing ADR-0004. The unfiltered Domain suite now clears both repository gates: 96.48% line coverage and 85.58% mutation score.

Phase 2 has no remaining implementation or acceptance work. The owner's sign-off is product acceptance, not legal or regulatory certification. APRC disclosure extensions remain outside the approved calculator scope. Existing tests and thresholds remain in force.

The first read-model projection is implemented under [ADR-0009](adr/0009-loan-state-projection.md). Further common queries, terminal command workflows and historical queries remain Phase 3 work. [Authorization](adr/0008-single-tenant-authorization.md) still requires explicit security review before exposing operations. See the [roadmap](plan/README.md) for acceptance criteria.
