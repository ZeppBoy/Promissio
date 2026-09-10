# Loan State Machine

> Status: Accepted.
> Last updated: 2026-09-10.
> Owner: Domain owner.
> Related: AGENTS.md §8, ADR-0006 (Proposed), `docs/plan/phase-3-contract-review.md`.

## States

| State | Description | Terminal? |
|---|---|---|
| `Disbursed` | Loan has been disbursed; not yet in the active servicing cycle. | No |
| `Active` | Loan is active and performing normally. | No |
| `InGrace` | Loan is within the grace period (days past due ≥ 0, < past-due threshold). | No |
| `PastDue` | Loan is past due (days past due ≥ past-due threshold, default 1, < default threshold). | No |
| `Defaulted` | Loan is in default (days past due ≥ default threshold, default 90). | No |
| `WrittenOff` | Loan has been written off. | **Yes** |
| `Restructured` | Loan has been restructured. | **Yes** |
| `Recovered` | Loan has been recovered. | **Yes** |

## Transition Table

| # | From | Command / Trigger | Guard | To | Event | Effective Date |
|---|---|---|---|---|---|---|
| 1 | *(creation)* | Disbursement confirmed | Principal > 0; first payment date ≥ disbursement date | `Disbursed` | `LoanCreated` | Disbursement date |
| 2 | `Disbursed` | Activate | First business day after disbursement | `Active` | `LoanActivated` | Activation date |
| 3 | `Active` | ApplyAging(daysPastDue = 0) | — | `Active` | *(none — no-op)* | — |
| 4 | `Active` | ApplyAging(0 < days < pastDueThreshold) | pastDueThreshold default 1 | `InGrace` | `LoanEnteredGracePeriod` | Aging date |
| 5 | `Active` | ApplyAging(days ≥ pastDueThreshold, < defaultThreshold) | pastDueThreshold default 1; defaultThreshold default 90 | `PastDue` | `LoanBecamePastDue` | Aging date |
| 6 | `Active` | ApplyAging(days ≥ defaultThreshold) | defaultThreshold default 90 | `Defaulted` | `LoanDefaulted` | Aging date |
| 7 | `InGrace` | ApplyAging(days = 0) | Payment cleared the delinquency | `Active` | `LoanBecameActive` | Aging date |
| 8 | `InGrace` | ApplyAging(days ≥ pastDueThreshold, < defaultThreshold) | — | `PastDue` | `LoanBecamePastDue` | Aging date |
| 9 | `InGrace` | ApplyAging(days ≥ defaultThreshold) | — | `Defaulted` | `LoanDefaulted` | Aging date |
| 10 | `PastDue` | ApplyAging(days = 0) | Payment cleared the delinquency | `Active` | `LoanBecameActive` | Aging date |
| 11 | `PastDue` | ApplyAging(days ≥ defaultThreshold) | — | `Defaulted` | `LoanDefaulted` | Aging date |
| 12 | `PastDue` | ApplyAging(pastDueThreshold ≤ days < defaultThreshold) | — | `PastDue` | `LoanBecamePastDue` | Aging date |
| 13 | `Defaulted` | WriteOff | Reason provided | `WrittenOff` | `LoanWrittenOff` | Write-off date |
| 14 | `Defaulted` | Restructure | Plan provided | `Restructured` | `LoanRestructured` | Restructuring date |
| 15 | `Defaulted` | Recover | Method provided | `Recovered` | `LoanRecovered` | Recovery date |
| 16 | `Disbursed`, `Active`, `InGrace`, `PastDue` | RecordPayment(amount > 0) | amount ≤ remaining balance | *(state unchanged)* | `PaymentReceived` | Payment date |
| 17 | `Disbursed`, `Active`, `InGrace`, `PastDue` | RecordPayment(amount > balance) | — | *(state unchanged)* | *(none — Result failure)* | — |

## Invalid Transitions (throw `InvalidStateTransitionException`)

| Command | Prohibited From States | Reason |
|---|---|---|
| Activate | `Active`, `InGrace`, `PastDue`, `Defaulted`, `WrittenOff`, `Restructured`, `Recovered` | Can only activate from Disbursed |
| RecordPayment | `WrittenOff`, `Restructured`, `Recovered` | Terminal state — no payments accepted |
| ApplyAging | `Disbursed`, `WrittenOff`, `Restructured`, `Recovered` | Cannot age a loan that is not yet active or is terminal |
| WriteOff | `Disbursed`, `Active`, `InGrace`, `PastDue`, `WrittenOff`, `Restructured`, `Recovered` | Can only write off from Defaulted |
| Restructure | `Disbursed`, `Active`, `InGrace`, `PastDue`, `WrittenOff`, `Restructured`, `Recovered` | Can only restructure from Defaulted |
| Recover | `Disbursed`, `Active`, `InGrace`, `PastDue`, `WrittenOff`, `Restructured`, `Recovered` | Can only recover from Defaulted |

## Business Validation Failures (return `Result<T>`)

| Command | Condition | Error |
|---|---|---|
| RecordPayment | amount ≤ 0 | "Payment amount must be positive." |
| RecordPayment | amount > remaining balance | "Payment amount (…) exceeds remaining balance (…)." |

## Failure Boundary (Owner Decision E-5, 2026-09-10)

- **Business validation failures** (e.g., payment amount ≤ 0, payment exceeds balance) return `Result<T>` with a typed error.
- **Prohibited state transitions** (e.g., paying a written-off loan, aging a disbursed loan) throw `InvalidStateTransitionException` with current state, attempted command, and reason.
- Both failure paths leave aggregate state unchanged and emit no events.
- The classification is defined explicitly per command in the tables above.

## Event Schema

All events extend `LoanEvent` and include:

| Field | Type | Description |
|---|---|---|
| `LoanId` | `LoanId` | The loan identity |
| `EffectiveDate` | `LocalDate` | Business-effective date |
| `RecordedAt` | `Instant` | Recorded (persisted) time |
| `CorrelationId` | `Guid` | Correlation to the originating command |

Domain payload fields per event are defined in the event record itself.

## Historical State (Owner Decision E-7, 2026-09-10)

Historical state as of date D = all events with **effective date ≤ D**. This may
require reordering relative to stream order (a payment recorded on 2026-09-10
with effective date 2026-09-01 counts as of 2026-09-01).
