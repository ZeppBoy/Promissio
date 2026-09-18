# Delivery status

Working-tree assessment: 2026-09-18, based on `main` commit `c0096ac`. [Phase 2 completion evidence](verification/2026-09-18-phase-2-completion.md) records the current checks and their limits; [Phase 3 T2 evidence](verification/2026-09-16-phase-3-t2.md) records the Marten implementation scenarios.

Statuses are separate: **planned** means intended scope, **implemented** means code exists, **verified** requires recorded checks, and **reviewed** requires human review. Passing unit tests does not establish complete phase acceptance or financial compliance.

| Area | Implementation | Verification / review |
|---|---|---|
| Phase 0 foundation | Projects, dependency management, CI and runnable host shells present | Full solution build restored with zero warnings and errors |
| Phase 1 interest engine | Domain primitives, day counts and rate types present | Included in the passing whole-Domain coverage and mutation gates |
| Phase 2 schedules / APRC | **Complete** | 528 Domain tests pass; whole-Domain line coverage 96.48% and mutation score 85.58%; Phase 2 line coverage 98.51%; owner financial-contract sign-off recorded 2026-09-18 |
| Phase 3 loan lifecycle | Aggregate/state machine plus T2 creation, replay and Marten persistence are committed on `main` | Build restored; PostgreSQL Testcontainers execution still requires Docker |
| Phase 4 APIs | Host and application scaffolding | No endpoint or real PostgreSQL workflow verification |
| Phase 5 batch | Runnable generic host only | Registration and lifecycle smoke tests; no idempotency evidence |
| Phases 6–8 MCP / AI / evaluations | Host scaffolding and plans | No tools, agents, golden datasets or evaluation thresholds exercised |
| Phases 9–10 polish / launch | Planned; benchmark harnesses exist | No current performance baseline or launch readiness claim |

The Application test project covers the creation handler. Integration contains real PostgreSQL scenarios for creation, replay, duplicate handoff, changed-payload conflict and concurrent handoffs; they require Docker. The current host does not have the Docker CLI installed. AI evaluations remain empty.

Owner sequencing decision, 2026-09-10: Phase 3 preparation proceeded while Phase 2 assurance was deferred. That deferred Phase 2 implementation work is now complete without changing ADR-0004. The unfiltered Domain suite now clears both repository gates: 96.48% line coverage and 85.58% mutation score.

Phase 2 has no remaining implementation or acceptance work. The owner's sign-off is product acceptance, not legal or regulatory certification. APRC disclosure extensions remain outside the approved calculator scope. Existing tests and thresholds remain in force.

The next implementation gate is executing the Phase 3 PostgreSQL Testcontainers suite on a Docker-capable host, then continuing with persisted lifecycle transitions and projections. [Authorization](adr/0008-single-tenant-authorization.md) still requires explicit security review before exposing operations. See the [roadmap](plan/README.md) for acceptance criteria.
