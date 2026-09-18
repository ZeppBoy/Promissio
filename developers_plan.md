# Promissio development plan

The authoritative [roadmap](docs/plan/README.md) contains product context, phase acceptance criteria and future scope. Check [current status](docs/status.md) for delivery evidence, and [current architecture](docs/architecture/current-state.md) for the existing solution.

Next: select the next bounded Phase 3 ticket after persisted `ApplyAging`. PostgreSQL creation/replay, activation and aging now pass real Testcontainers verification with optimistic concurrency. The logical next candidate is a first read-model projection, but projection lifecycle and event-schema evolution decisions require owner approval before implementation. Phase 2 is complete: implementation and whole-Domain quality gates pass, and owner financial-contract sign-off was recorded on 2026-09-18.

The [original monolithic plan](docs/archive/developers-plan-2026-05-17.md) is preserved for historical context. Update canonical documents under docs rather than this forwarding overview.
