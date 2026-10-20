# Delivery status

Working-tree assessment: 2026-10-20, based on `main` commit `10fd369` plus the approved loan-summary slice. [Phase 2 completion evidence](verification/2026-09-18-phase-2-completion.md) records its checks and sign-off; [loan-summary evidence](verification/2026-10-20-loan-summary.md) records the latest persistence and query verification.

Statuses are separate: **planned** means intended scope, **implemented** means code exists, **verified** requires recorded checks, and **reviewed** requires human review. Passing unit tests does not establish complete phase acceptance or financial compliance.

| Area | Implementation | Verification / review |
|---|---|---|
| Phase 0 foundation | Projects, dependency management, CI and runnable host shells present | Full solution build restored with zero warnings and errors |
| Phase 1 interest engine | Domain primitives, day counts and rate types present | Included in the passing whole-Domain coverage and mutation gates |
| Phase 2 schedules / APRC | **Complete** | 528 Domain tests pass; whole-Domain line coverage 96.48% and mutation score 85.58%; Phase 2 line coverage 98.51%; owner financial-contract sign-off recorded 2026-09-18 |
| Phase 3 loan lifecycle | Aggregate/state machine, idempotent creation/replay, persisted activation/aging, inline loan summaries, query-by-id and offline rebuilds | 18 PostgreSQL Testcontainers cases pass; terminal Application workflows, state-filtered lists, portfolio summaries and historical queries remain |
| Phase 4 APIs | Host scaffolding; Application creation, activation, aging and summary query implemented | Application workflows have PostgreSQL verification; no HTTP business endpoint verification |
| Phase 5 batch | Runnable generic host only | Registration and lifecycle smoke tests; no idempotency evidence |
| Phases 6–8 MCP / AI / evaluations | Host scaffolding and plans | No tools, agents, golden datasets or evaluation thresholds exercised |
| Phases 9–10 polish / launch | Planned; benchmark harnesses exist | No current performance baseline or launch readiness claim |

The Application test project covers creation, activation, aging and summary queries; all 17 tests pass and measured Application line coverage is 82.91% (131/158). Integration adds immediate summary reads, reserved payment-event compatibility, terminal-event projection, backdated stream-order behavior, stale-writer rollback and repeated rebuild/backfill with receipt preservation to the existing persistence cases. Docker was unavailable at the start of this assessment; restarting the hung Desktop frontend restored the engine, and all 18 PostgreSQL cases now pass. AI evaluations remain empty.

Owner sequencing decision, 2026-09-10: Phase 3 preparation proceeded while Phase 2 assurance was deferred. That deferred Phase 2 implementation work is now complete without changing ADR-0004. The unfiltered Domain suite now clears both repository gates: 96.48% line coverage and 85.58% mutation score.

Phase 2 has no remaining implementation or acceptance work. The owner's sign-off is product acceptance, not legal or regulatory certification. APRC disclosure extensions remain outside the approved calculator scope. Existing tests and thresholds remain in force.

The owner approved the first loan-summary projection slice and its consistency, rebuild and event compatibility policies on 2026-10-20; [ADR-0009](adr/0009-loan-summary-projection.md) records the decision. This completes only the first bounded projection slice, not all Phase 3 T6 query scope. Next select state-filtered queries or one persisted terminal transition. [Authorization](adr/0008-single-tenant-authorization.md) still requires explicit security review before exposing operations. See the [roadmap](plan/README.md) for acceptance criteria. New generated documentation requires a human edit before merge.
