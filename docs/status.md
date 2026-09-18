# Delivery status

Mainline assessment: 2026-09-18 after synchronizing with `origin/main` at commit `075840d`. [Current verification evidence](verification/2026-09-18-current-status.md) records the checks and their limits; the earlier [Phase 3 T2 evidence](verification/2026-09-16-phase-3-t2.md) records the implementation-specific scenarios.

Statuses are separate: **planned** means intended scope, **implemented** means code exists, **verified** requires recorded checks, and **reviewed** requires human review. Passing unit tests does not establish complete phase acceptance or financial compliance.

| Area | Implementation | Verification / review |
|---|---|---|
| Phase 0 foundation | Projects, dependency management, CI and runnable host shells present | Current mainline build fails with two schedule-generator interface errors; earlier baseline checks remain recorded |
| Phase 1 interest engine | Domain primitives, day counts and rate types present | Domain tests included; coverage and mutation targets pending |
| Phase 2 schedules / APRC | **Partially implemented; remaining assurance work deferred by owner** | Current mainline does not compile because `BulletScheduleGenerator` and `CustomScheduleGenerator` do not implement the updated interface; prior mutation score was 72.70% (<80% target) |
| Phase 3 loan lifecycle | Aggregate/state machine plus T2 creation, replay and Marten persistence are committed on `main` | At the last green baseline (`2fe5ded`), 477 Domain, 2 Application and 3 Infrastructure tests passed; current mainline cannot be reverified until the solution compiles |
| Phase 4 APIs | Host and application scaffolding | No endpoint or real PostgreSQL workflow verification |
| Phase 5 batch | Runnable generic host only | Registration and lifecycle smoke tests; no idempotency evidence |
| Phases 6–8 MCP / AI / evaluations | Host scaffolding and plans | No tools, agents, golden datasets or evaluation thresholds exercised |
| Phases 9–10 polish / launch | Planned; benchmark harnesses exist | No current performance baseline or launch readiness claim |

The Application test project covers the creation handler. Integration contains real PostgreSQL scenarios for creation, replay, duplicate handoff, changed-payload conflict and concurrent handoffs; they require Docker. The current host does not have the Docker CLI installed. AI evaluations remain empty. The current build break was introduced by the newer remote merge and is not caused by the status-documentation change.

Owner sequencing decision, 2026-09-10: proceed to Phase 3 preparation while Phase 2 remains partially implemented. This defers the remaining Phase 2 work; it does not waive its acceptance criteria or turn a failed mutation threshold into a pass. The mutation run completed, but its quality gate failed. Its aggregate results were checked against the saved JSON; its proposed survivor fixes contain contract errors and must not be used as implementation instructions without correction.

Deferred Phase 2 work: correct survivor classification using actual mutant IDs, address uncovered and genuinely surviving behavior, measure line coverage, retain repeatable independent reference evidence and complete financial review. Preserve existing tests and thresholds. APRC disclosure extensions remain outside the approved calculator scope.

The immediate gate is restoring a clean solution build by reconciling the two schedule generators with `IScheduleGenerator`. After that, execute the Phase 3 PostgreSQL Testcontainers suite on a Docker-capable host, then continue with persisted lifecycle transitions and projections. [Authorization](adr/0008-single-tenant-authorization.md) still requires explicit security review before exposing operations. See the [roadmap](plan/README.md) for acceptance criteria.
