# Phase 3 — Loan Aggregate and State Machine (Weeks 7–8)

> Canonical phase plan. Scope is planned unless [current status](../status.md) records verification. Week numbers are historical effort estimates, not deadlines.

**Goal:** Model the loan lifecycle with rigorous state transitions and event sourcing.

**Next active task:** execute the T2 PostgreSQL integration tests on a Docker-capable host, then add persisted lifecycle transitions one approved command at a time. Phase 2 implementation and automated assurance, including the whole-Domain quality gates, are complete; human financial review remains separate.

## Week 7 — Loan Aggregate

**Tasks:**

1. Review [origination handoff](../adr/0006-origination-servicing-handoff.md) and [persistence ownership](../adr/0007-persistence-and-concurrency.md) before implementing `Loan` aggregate root with invariants enforced at construction and on every command.
2. Define commands after the proposed application-to-loan handoff is reviewed. Approval ownership must be settled before the command and event schemas are implemented.
3. Emit domain events for every state transition.
4. Implement state machine validation: rejected transitions throw explicit exceptions with diagnostic context.

**Acceptance criteria:**

- All state transitions are tested.
- Invalid transitions are rejected with informative exceptions.
- Domain events carry sufficient information for downstream consumers (no need to query the aggregate).

## Week 8 — Marten Event Sourcing Integration

**Tasks:**

1. Configure Marten with PostgreSQL.
2. Persist all domain events.
3. Build read-model projections for common queries (active loans, overdue loans, portfolio summary).
4. Implement time-travel queries: retrieve loan state as of any past date.

**Acceptance criteria:**

- Integration tests verify event persistence and projection rebuilds.
- Time-travel query returns correct historical state for at least 10 scenarios.

**AI delegation notes:** Marten configuration and projection scaffolding are well-suited to AI assistance. Domain event design (which events exist, what data they carry) should be the author's decision — these shape the entire system's auditability story.

---
