# Phase 3 — Loan Aggregate and State Machine (Weeks 7–8)

> **Companion:** всегда подгружать вместе с `00-core.md`.
> **Weeks:** 7, 8
> **Goal:** Model the loan lifecycle with rigorous state transitions and event sourcing.

---

## Week 7 — Loan Aggregate

### Tasks

1. Implement `Loan` aggregate root with invariants enforced at construction and on every command.
2. Define explicit commands: `CreateLoan`, `ApproveLoan`, `DisburseLoan`, `ApplyPayment`, `MoveToPastDue`, `RestructureLoan`, `WriteOff`.
3. Emit domain events for every state transition.
4. Implement state machine validation: rejected transitions throw explicit exceptions with diagnostic context.

### Acceptance criteria

- All state transitions are tested.
- Invalid transitions are rejected with informative exceptions.
- Domain events carry sufficient information for downstream consumers (no need to query the aggregate).

---

## Week 8 — Marten Event Sourcing Integration

### Tasks

1. Configure Marten with PostgreSQL.
2. Persist all domain events.
3. Build read-model projections for common queries (active loans, overdue loans, portfolio summary).
4. Implement time-travel queries: retrieve loan state as of any past date.

### Acceptance criteria

- Integration tests verify event persistence and projection rebuilds.
- Time-travel query returns correct historical state for at least 10 scenarios.

---

## AI delegation notes

Marten configuration and projection scaffolding are well-suited to AI assistance. Domain event design (which events exist, what data they carry) should be the author's decision — these shape the entire system's auditability story.
