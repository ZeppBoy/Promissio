# Delivery status

Working-tree assessment: 2026-09-16. [Verification evidence](verification/2026-09-10-structure-alignment.md) records the earlier baseline checks and their limits.

Statuses are separate: **planned** means intended scope, **implemented** means code exists, **verified** requires recorded checks, and **reviewed** requires human review. Passing unit tests does not establish complete phase acceptance or financial compliance.

| Area | Implementation | Verification / review |
|---|---|---|
| Phase 0 foundation | Projects, dependency management, CI and runnable host shells present | Build and baseline checks recorded; complete production foundation not claimed |
| Phase 1 interest engine | Domain primitives, day counts and rate types present | Domain tests included; coverage and mutation targets pending |
| Phase 2 schedules / APRC | **Partially implemented; remaining assurance work deferred by owner** | Core calculations and stabilization present; mutation score 72.70% (<80% target), line coverage unknown, financial review pending |
| Phase 3 loan lifecycle | Aggregate/state machine implemented; T2 creation, replay and Marten persistence implemented in the working tree | 477 Domain, 2 Application and 3 Infrastructure tests pass; PostgreSQL Testcontainers scenarios compile but await execution on a Docker-capable host |
| Phase 4 APIs | Host and application scaffolding | No endpoint or real PostgreSQL workflow verification |
| Phase 5 batch | Runnable generic host only | Registration and lifecycle smoke tests; no idempotency evidence |
| Phases 6–8 MCP / AI / evaluations | Host scaffolding and plans | No tools, agents, golden datasets or evaluation thresholds exercised |
| Phases 9–10 polish / launch | Planned; benchmark harnesses exist | No current performance baseline or launch readiness claim |

The Application test project now covers the creation handler. Integration contains real PostgreSQL scenarios for creation, replay, duplicate handoff, changed-payload conflict and concurrent handoffs; they require Docker. AI evaluations remain empty.

Owner sequencing decision, 2026-09-10: proceed to Phase 3 preparation while Phase 2 remains partially implemented. This defers the remaining Phase 2 work; it does not waive its acceptance criteria or turn a failed mutation threshold into a pass. The mutation run completed, but its quality gate failed. Its aggregate results were checked against the saved JSON; its proposed survivor fixes contain contract errors and must not be used as implementation instructions without correction.

Deferred Phase 2 work: correct survivor classification using actual mutant IDs, address uncovered and genuinely surviving behavior, measure line coverage, retain repeatable independent reference evidence and complete financial review. Preserve existing tests and thresholds. APRC disclosure extensions remain outside the approved calculator scope.

The next verification gate is executing the Phase 3 PostgreSQL Testcontainers suite on a Docker-capable host. After that, continue with persisted lifecycle transitions and projections. [Authorization](adr/0008-single-tenant-authorization.md) still requires explicit security review before exposing operations. See the [roadmap](plan/README.md) for acceptance criteria.
