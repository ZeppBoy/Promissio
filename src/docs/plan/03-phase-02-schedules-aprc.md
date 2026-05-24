# Phase 2 — Payment Schedule Generator (Weeks 5–6)

> **Companion:** всегда подгружать вместе с `00-core.md`.
> **Weeks:** 5, 6
> **Goal:** Implement all four schedule types and the APRC calculator.

---

## Week 5 — Annuity and Differentiated Schedules

### Tasks

1. Define `IScheduleGenerator` interface.
2. Implement `AnnuityScheduleGenerator` using the standard annuity formula.
3. Implement `DifferentiatedScheduleGenerator` with equal principal portions.
4. Handle edge cases: short first period, long first period, grace period adjustments, holiday calendars.
5. Verify invariants: total payments equal principal plus total interest, no rounding drift.

### Acceptance criteria

- Generated schedules pass invariant checks for at least 100 random loan configurations.
- Snapshot tests via Verify.Xunit lock in expected schedules for canonical examples.

---

## Week 6 — Bullet, Custom, and APRC Calculation

### Tasks

1. Implement `BulletScheduleGenerator` with interest-only periods and balloon payment.
2. Implement `CustomScheduleGenerator` accepting predefined cash flows.
3. Implement `AprcCalculator` using the Newton-Raphson or bisection iterative method per EU Consumer Credit Directive 2008/48/EC.
4. Validate against official EU example cases.

### Acceptance criteria

- APRC values match EU reference examples to four decimal places.
- All schedule generators produce balanced schedules (sum of principal portions equals original principal).

---

## AI delegation notes

Schedule generation logic is well-suited to AI assistance once formulas are specified. The author should always manually validate three to five reference cases per schedule type, since LLMs may produce code that is plausible but incorrect for edge cases like grace periods or non-standard first periods.
