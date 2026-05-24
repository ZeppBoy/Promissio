# Phase 5 — Daily Batch Processor (Weeks 11–12)

> **Companion:** всегда подгружать вместе с `00-core.md`.
> **Weeks:** 11, 12
> **Goal:** Implement end-of-day operations with the operational rigor expected in real loan servicing.

---

## Week 11 — Scheduler and Daily Run Logic

### Tasks

1. Implement a hosted service that tracks the last successful run date.
2. Design idempotent operations — running the batch twice for the same date must produce identical results.
3. Implement graceful shutdown handling.
4. Add structured logging with correlation IDs spanning the entire batch run.

### Acceptance criteria

- Re-running a completed batch produces no duplicate accruals or transitions.
- Batch can resume after interruption from the last completed step.

---

## Week 12 — Daily Operations

### Tasks

1. Implement daily interest accrual for all active loans.
2. Implement past-due detection and state transitions (Active → PastDue based on days past due).
3. Implement penalty rate activation logic.
4. Implement IFRS 9 stage transitions (Stage 1 → Stage 2 → Stage 3 based on days past due and credit deterioration signals).
5. Calculate provisioning per simplified IFRS 9 expected credit loss approach.

### Acceptance criteria

- A simulated portfolio of 1,000 loans processes correctly through a 90-day simulation.
- IFRS 9 stage transitions match expected behavior for canonical scenarios.

---

## AI delegation notes

Batch orchestration logic is well-suited to AI assistance. IFRS 9 staging rules require domain expertise — the author should specify rules precisely rather than asking the AI to infer them.
