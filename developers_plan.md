# Promissio development plan

The authoritative [roadmap](docs/plan/README.md) contains product context, phase acceptance criteria and future scope. Check [current status](docs/status.md) for delivery evidence, and [current architecture](docs/architecture/current-state.md) for the existing solution.

The first loan-summary projection slice was approved on 2026-10-20 and is implemented with inline consistency, query-by-id and offline rebuild support; see ADR-0009 and current status for verification. Next: select a bounded Phase 3 slice for state-filtered queries or a persisted terminal transition. Portfolio summaries and business-effective historical queries remain. Phase 2 is complete: implementation and whole-Domain quality gates pass, and owner financial-contract sign-off was recorded on 2026-09-18.

The [original monolithic plan](docs/archive/developers-plan-2026-05-17.md) is preserved for historical context. Update canonical documents under docs rather than this forwarding overview.
