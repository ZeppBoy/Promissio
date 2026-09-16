# Phase 3 contract review — Loan aggregate and state machine

> Status: Historical preparation packet. T1 was merged on 2026-09-11; ADR-0006 and the T2 subset of ADR-0007 were accepted on 2026-09-16. See `docs/status.md` for current implementation state.
> Prepared: 2026-09-10.
> Baseline: branch `main`, commit `ef634bc`, working tree dirty (see dirty paths below).
> Scope: documentation-only review packet that makes Phase 3 domain decisions concrete and reviewable. No source, tests, snapshots, configuration, ADR-status, dependencies or Phase 2 assurance edits.
>
> This file does **not** approve ADR-0006 or ADR-0007. It records their open questions, the evidence available in the working tree, and the minimum decisions required before the first coding ticket can be unblocked.

## Owner Decisions Recorded (2026-09-10)

The following decisions were made by the owner on 2026-09-10 and are recorded here for the coding phase:

| # | Decision | Owner Ruling |
|---|----------|-------------|
| E-1 | Loan creation milestone | **Disbursement.** Servicing Loan is created only at confirmed disbursement. |
| E-2 | Approved terms immutability | Approved terms are immutable per version. A change before disbursement creates a new terms version on the same LoanApplication and requires fresh approval and borrower acceptance before disbursement. Preserve previous versions and their approval history; do not overwrite them or require a new application automatically. Create the servicing Loan only at confirmed disbursement, using the exact approved and accepted terms version. |
| E-3 | Loan identity and handoff idempotency | Strongly-typed `LoanId` wrapping a `Guid`. Allow exactly one servicing Loan per `LoanApplicationId`, using `LoanApplicationId` as the handoff idempotency key. An identical retry returns the existing `LoanId` without creating another loan or event. A retry with different terms version or disbursement details returns a conflict. Enforce uniqueness and record the handoff outcome atomically with loan creation. |
| E-4 | Loan state names | Accept AGENTS.md §8 names as-is: `Disbursed`, `Active`, `InGrace`, `PastDue`, `Defaulted`, `WrittenOff`, `Restructured`, `Recovered`. |
| E-5 | Failure boundary | Commands rejected by an approved business validation rule return `Result<T>` with a typed error. Transitions prohibited by the approved state-transition table throw `InvalidStateTransitionException` containing the current state, attempted transition, and reason. Both failure paths leave aggregate state unchanged and emit no events. Define the classification explicitly for each command; do not infer it from "structural" versus "conditional" wording. |
| E-6 | Event versioning policy | *(UNRESOLVED — deferred to T6.)* |
| E-7 | Historical state time | **Business-effective time.** Historical state as of D = all events with effective date ≤ D. This may require reordering relative to stream order. |
| E-8 | Event schema worksheet | *(UNRESOLVED — deferred to T3.)* |
| E-9 | Existing LoanApplication record | **Transient DTO.** The existing `LoanApplication(decimal Amount, int TermInMonths)` record in `src/Promissio.Application/Validators.cs` is a transient DTO. A new Domain aggregate is needed; the record should be renamed or removed. |

## Baseline

- Branch: `main`
- Commit: `ef634bc`
- Dirty paths at preparation time:
  - `README.md`, `developers_plan.md`
  - `docs/plan/03-phase-02-schedules-aprc.md`, `docs/plan/04-phase-03-loan-aggregate.md`
  - `docs/plan/qwen-local/README.md`, `docs/status.md`
  - Untracked: `Prompts/qwen-phase-3-contracts.md`, `docs/verification/2026-09-10-stryker-mutation-run.md`, `docs/verification/qwen-phase-2-assurance-inventory.md`
- This review packet adds a new untracked file: `docs/plan/phase-3-contract-review.md`. No other file is modified by this task.

## Task summary (six bullets)

- Prepare a review packet that turns the open Phase 3 questions in ADR-0006 (origination handoff) and ADR-0007 (persistence and concurrency) into explicit ACCEPTED / PROPOSED / UNRESOLVED items, so the owner can approve the first coding ticket.
- Do **not** invent state names, identifiers, event schemas, business milestones, rates, dates or financial values. Unknown entries are marked `UNRESOLVED`.
- Do **not** approve ADR-0006, ADR-0007, ADR-0008, or any future ADR. Do **not** change ADR statuses.
- Do **not** run Stryker, build, or test in this documentation-only task. No coverage or mutation claims are made.
- The first coding ticket (T1) is defined but stays `BLOCKED ON CONTRACT APPROVAL` until the required owner decisions in §E are accepted.
- This packet is the single output file for the task; it is not a plan to implement Phase 3 end-to-end.

## Evidence table

| # | Fact | Exact repository file / symbol | Accepted rule or unresolved decision |
|---|---|---|---|
| 1 | No `Loan` aggregate exists in `Promissio.Domain`. | `src/Promissio.Domain/` (27 files; no `Loan.cs` or `Loan` class). Grep for `Loan` across `src/**/*.cs` shows only `LoanTerm`, `LoanApplication` (Application), `LoanApplicationValidator` (Application), and the phrase "loan" in doc comments. | UNRESOLVED — `Loan` aggregate is a Phase 3 deliverable, not a pre-existing type. |
| 2 | No `LoanApplication` aggregate exists in Domain. The only `LoanApplication` is a 2-field record in Application. | `src/Promissio.Application/Validators.cs` line 19: `public record LoanApplication(decimal Amount, int TermInMonths);` and line 5 `LoanApplicationValidator`. | UNRESOLVED — Phase 3 must decide whether the existing Application-layer `LoanApplication` record is the underwriting aggregate root or a transient DTO, and where the underwriting decision lives (ADR-0006 open question). |
| 3 | No domain event types exist. | Grep for `Event`, `EventStream`, `DomainEvent` across `src/**/*.cs` returns no aggregate events. | UNRESOLVED — event names, schemas, and versioning are Phase 3 decisions. |
| 4 | No state machine, no state enum, no transition table exists in code or docs. | Grep for `State` across `src/**/*.cs` returns no loan-state symbols. `docs/domain/` contains no loan state machine document. | UNRESOLVED — state names and transitions are Phase 3 decisions; AGENTS.md §8 forbids inventing them. |
| 5 | Existing value objects that a `Loan` aggregate would reuse. | `src/Promissio.Domain/ValueObjects/Money.cs`, `Percentage.cs`, `LoanTerm.cs`, `InterestRate.cs` (abstract) with `FixedRate`, `FloatingRate`, `TieredRate`, `HolidayCalendar.cs`. | ACCEPTED — value objects are sealed records with constructor validation; reuse is expected, not new. |
| 6 | Schedule generation is implemented and has a stable `PaymentScheduleItem` contract. | `src/Promissio.Domain/ScheduleGeneration/IScheduleGenerator.cs` — `IEnumerable<PaymentScheduleItem> Generate(Money principal, InterestRate, int termMonths, LocalDate startDate, int gracePeriodMonths, HolidayCalendar?, LocalDate? firstPaymentDate)` and `PaymentScheduleItem` record. | ACCEPTED (ADR-0004) — schedule generation is a Phase 2 contract; Phase 3 may consume it, must not change it without a new ADR. |
| 7 | Interest calculation is gated behind `IInterestCalculator`. | `src/Promissio.Domain/Calculations/IInterestCalculator.cs` — `Money Calculate(Money principal, InterestRate rate, LocalDate startDate, LocalDate endDate)` and `IReadOnlyList<Money> CalculateForPeriods(...)`. | ACCEPTED — AGENTS.md §8 mandates this as the only path to compute interest. |
| 8 | Day-count conventions exist as a sealed set. | `src/Promissio.Domain/Calculations/DayCounts/` — `Actual360`, `Actual365`, `ActualActual`, `Thirty360`, `Thirty360European`, `DayCountConvention` base, `DayCountConventions` factory. | ACCEPTED (ADR-0002, ADR-0003) — rate owns its convention. |
| 9 | Marten is referenced in Infrastructure but no streams, projections, or repositories exist. | `src/Promissio.Infrastructure/InfrastructureService.cs` — `ConfigureMarten(IServiceCollection)` sets `options.DatabaseSchemaName = "promissio";` only. No `IEventStore`, no projections, no `DocumentStore` configuration beyond schema name. | PROPOSED (ADR-0007) — Marten event streams as authority, projections as derived; not yet accepted. |
| 10 | Application layer has MediatR registration and one placeholder request/response. | `src/Promissio.Application/ApplicationService.cs` — `ConfigureMediatR(IServiceCollection)`, plus `public record SomeRequest();` and `public record SomeResponse();`. | PROPOSED — no loan workflows exist yet. |
| 11 | Project references today: Domain references NodaTime only; Application references Domain; Infrastructure has no project references; APIs do not yet reference Infrastructure. | `docs/architecture/current-state.md` and verified by reading project files. | ACCEPTED — Domain purity is enforced by build; Phase 3 must preserve it. |
| 12 | No `Loan` state machine documentation exists. | `docs/domain/` directory does not contain `loan-state-machine.md`. AGENTS.md §8 references it as the required location. | UNRESOLVED — documentation is a Phase 3 deliverable. |
| 13 | ADR-0004 (Phase 2 schedule/APRC contracts) is Accepted. | `docs/adr/0004-phase-2-schedule-and-aprc-contracts.md` header. | ACCEPTED — Phase 3 must not change Phase 2 schedule/APRC contracts without a new ADR. |
| 14 | ADR-0006 (origination handoff) and ADR-0007 (persistence) are Proposed. | `docs/adr/0006-origination-servicing-handoff.md`, `docs/adr/0007-persistence-and-concurrency.md`. | PROPOSED — owner review required before Phase 3 implementation. |
| 15 | ADR-0008 (authorization) is Proposed and out of scope for this packet. | `docs/adr/0008-single-tenant-authorization.md`. | OUT OF SCOPE — authorization is a later task; not a Phase 3 contract. |
| 16 | `DomainService` exists in Domain for clock/time-zone access. | `src/Promissio.Domain/DomainService.cs` — `GetCurrentDate()` using `IClock` and explicit `DateTimeZone`. | ACCEPTED — Phase 3 aggregate methods should not assume UTC; business time zone is explicit. |

## A. Ownership and handoff

### A.1 Responsibilities by layer (per accepted guidance)

| Responsibility | Owner | Source |
|---|---|---|
| Pure invariants, value objects, interest calculation, schedule generation, day-count conventions | `Promissio.Domain` | `docs/architecture/target-state.md`; AGENTS.md §3, §8 |
| Commands, queries, workflow orchestration, validation at API boundary, cross-aggregate coordination, transaction requirements | `Promissio.Application` | `docs/architecture/target-state.md`; AGENTS.md §3 |
| Marten event store, projections, EF Core relational data, external services, persistence ports | `Promissio.Infrastructure` | `docs/architecture/target-state.md`; ADR-0007 (Proposed) |
| Transport (HTTP, MCP, batch) | API hosts, MCP host, BatchProcessor | `docs/architecture/target-state.md` |
| Underwriting decision (approve / reject application) | **UNRESOLVED** — ADR-0006 proposes `LoanApplication` owns underwriting, but this is not accepted. | ADR-0006 §Proposed decision |
| Servicing loan lifecycle (states, transitions, events) | **UNRESOLVED** — ADR-0006 proposes a `Loan` aggregate created/activated from approved terms by application orchestration; not accepted. | ADR-0006 §Proposed decision |

### A.2 Milestone at which the servicing `Loan` begins to exist

ADR-0006 lists three candidates and explicitly asks the owner to decide. **Do not silently choose one.**

| Candidate | Implication | Owner decision required? |
|---|---|---|
| **Approval** | `Loan` is created the moment underwriting approves. The loan exists before any contract is signed and before any money moves. Risk: the servicing loan can be mutated before the borrower has accepted terms. | **OWNER DECISION REQUIRED** |
| **Contract acceptance** | `Loan` is created (or activated) when the borrower accepts the contract. Approval produces an offer; acceptance creates the servicing loan. | **OWNER DECISION REQUIRED** |
| **Disbursement** | `Loan` is created only when funds are disbursed. Before that, no servicing loan exists. | **OWNER DECISION REQUIRED** |

Consequences of each choice (recorded for the owner, not as a recommendation):

- **Approval**: simplest mapping from underwriting to servicing, but the servicing loan must be immutable (or effectively read-only) until disbursement to honor "approved terms are immutable" (AGENTS.md §6, ADR-0006 open question 3).
- **Contract acceptance**: introduces an intermediate state (e.g., `Offered` / `Accepted`) between approval and disbursement; more faithful to a two-party contract but adds a state the owner must name and a transition the owner must approve.
- **Disbursement**: the servicing loan only exists when there is a live obligation; approval and acceptance are pure application-layer facts on `LoanApplication`. Smallest `Loan` surface, but the servicing API cannot show "approved but not yet disbursed" loans.

### A.3 Immutability of approved terms — **RESOLVED 2026-09-10**

ADR-0006 open question 3: *"What happens when approved terms change before disbursement?"*

**Owner decision (verbatim):** Approved terms are immutable per version. A change before disbursement creates a new terms version on the same `LoanApplication` and requires fresh approval and borrower acceptance before disbursement. Preserve previous versions and their approval history; do not overwrite them or require a new application automatically. Create the servicing `Loan` only at confirmed disbursement, using the exact approved and accepted terms version.

Consequence for the `Loan` aggregate: the approved-terms block (principal, rate, term, currency, day-count convention, grace, holiday calendar, first payment date, schedule) is **immutable after creation**. The `Loan` is created once, at disbursement, and its terms cannot change.

### A.4 Identity and uniqueness for duplicate handoff prevention — **RESOLVED 2026-09-10**

ADR-0006 open question 4: *"What identifier and uniqueness rule make the handoff idempotent?"*

**Owner decision:** Strongly-typed `LoanId` wrapping a `Guid`. Allow exactly one servicing `Loan` per `LoanApplicationId`, using `LoanApplicationId` as the handoff idempotency key. An identical retry returns the existing `LoanId` without creating another loan or event. A retry with different terms version or disbursement details returns a conflict. Enforce uniqueness and record the handoff outcome atomically with loan creation.

### A.5 The existing `LoanApplication` record in Application — **RESOLVED 2026-09-10**

`src/Promissio.Application/Validators.cs` line 19 defines `public record LoanApplication(decimal Amount, int TermInMonths);`.

**Owner decision:** This is a **transient DTO**. A new Domain aggregate is needed; the record should be renamed or removed when the Domain aggregate is implemented.

## B. State and event contract worksheet

### B.1 Supported transitions (only those explicitly cited in accepted guidance)

No state names are approved. The following transitions are the **only** ones for which there is explicit guidance in the repository. All other entries are `UNRESOLVED`.

| # | Current state | Command | Guard | Resulting state | Event | Error | Effective date | Recorded time | Reference scenario | Source |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | UNRESOLVED (initial) | UNRESOLVED (creation) | UNRESOLVED | UNRESOLVED | UNRESOLVED | UNRESOLVED | UNRESOLVED | UNRESOLVED | Servicing loan created at the owner-chosen milestone (§A.2) | ADR-0006 open question 1; AGENTS.md §8 |
| 2 | UNRESOLVED (active performing) | UNRESOLVED (payment received) | UNRESOLVED (payment covers at least accrued interest) | UNRESOLVED (possibly back to performing if it was delinquent) | UNRESOLVED (e.g., payment-received event) | UNRESOLVED | UNRESOLVED (business date of payment) | UNRESOLVED (Instant of persistence) | Normal amortizing payment | AGENTS.md §8 §"State transitions" (common transitions list) |
| 3 | UNRESOLVED (active) | UNRESOLVED (aging crosses grace threshold) | UNRESOLVED (days past due ≥ grace threshold, default 1 per AGENTS.md) | UNRESOLVED (in-grace / past-due) | UNRESOLVED | UNRESOLVED | UNRESOLVED (date threshold crossed) | UNRESOLVED | Loan becomes past due | AGENTS.md §8: "Active / InGrace → PastDue based on days past due threshold (configurable, default 1)" |
| 4 | UNRESOLVED (past due) | UNRESOLVED (aging crosses default threshold) | UNRESOLVED (days past due ≥ default threshold, default 90 per AGENTS.md) | UNRESOLVED (defaulted) | UNRESOLVED | UNRESOLVED | UNRESOLVED (date threshold crossed) | UNRESOLVED | Loan becomes defaulted | AGENTS.md §8: "PastDue → Defaulted based on days past due threshold (configurable, default 90)" |
| 5 | UNRESOLVED (defaulted) | UNRESOLVED (recovery / write-off / restructure) | UNRESOLVED | UNRESOLVED (written-off / restructured / recovered) | UNRESOLVED | UNRESOLVED | UNRESOLVED | UNRESOLVED | Exit from default | AGENTS.md §8: "Defaulted → WrittenOff / Restructured / Recovered" |
| 6 | UNRESOLVED (any non-terminal) | UNRESOLVED (disbursement, if creation is at approval) | UNRESOLVED | UNRESOLVED (disbursed / active) | UNRESOLVED | UNRESOLVED | UNRESOLVED (disbursement date) | UNRESOLVED | Funds leave the lender | AGENTS.md §8: "Disbursed → Active on first business day after disbursement" |

**Note:** The AGENTS.md §8 transition list uses the names `Disbursed`, `Active`, `InGrace`, `PastDue`, `Defaulted`, `WrittenOff`, `Restructured`, `Recovered`. These names appear in the operating manual, but the manual explicitly says "State machines are documented in `/docs/domain/state-machines.md` with diagrams that must stay in sync with code" and that file does not exist. The names are therefore **not** approved as the final state names; they are the only names with a textual source. The owner must confirm or rename them.

**Owner decision (2026-09-10):** Accept AGENTS.md §8 names as-is: `Disbursed`, `Active`, `InGrace`, `PastDue`, `Defaulted`, `WrittenOff`, `Restructured`, `Recovered`. These are the final state names.

### B.2 Invalid transitions

AGENTS.md §6: *"Invalid transitions throw `InvalidStateTransitionException` with full context (current state, attempted transition, reason)."*

- **PROPOSED** (from AGENTS.md, which is authoritative) — the exception type name and the required diagnostic context are settled by AGENTS.md.
- **UNRESOLVED** — which specific (state, command) pairs are invalid; this follows from the transition table above once the owner approves the states and commands.

### B.3 Distinguishing expected business failures from invalid-state-transition exceptions — **RESOLVED 2026-09-10**

- **Owner decision (verbatim):** Commands rejected by an approved business validation rule return `Result<T>` with a typed error. Transitions prohibited by the approved state-transition table throw `InvalidStateTransitionException` containing the current state, attempted transition, and reason. Both failure paths leave aggregate state unchanged and emit no events. Define the classification explicitly for each command; do not infer it from "structural" versus "conditional" wording.

### B.4 Required event-field categories and versioning

AGENTS.md §6: *"Events carry enough data that consumers don't need to query the aggregate."* and *"Event schema changes are versioned. Never break existing event consumers without a migration plan."*

Required categories (no final field names, no C# records):

- **Identity:** which aggregate the event belongs to, and the aggregate's version at the time of the event (for concurrency).
- **Temporal:** business-effective date (`LocalDate`) and recorded time (`Instant`). These are distinct (see §B.5).
- **Causation:** which command produced the event, and the correlation/transaction id of the workflow (for audit).
- **Domain payload:** the data that changed (e.g., the new state, the amount, the rate, the date), sufficient for a consumer to update a projection without querying the aggregate.
- **Versioning:** an explicit schema version on each event type (or on the event envelope) so that a reader can detect an incompatible change.

Versioning questions (UNRESOLVED):

- Is versioning per event type (each event type has its own version counter) or per stream (a global version)?
- What is the migration policy for a breaking change (dual-write, read-time adapter, or rebuild)?
- Are events immutable in storage (append-only) or can they be corrected by a compensating event?

### B.5 Historical state: business-effective time vs. recorded event time — **RESOLVED 2026-09-10**

- **Owner decision:** **Business-effective time.** The state of the loan as of date D = all events with effective date ≤ D. This may require reordering relative to stream order.

Consequences of each interpretation:

- **Business-effective:** time-travel queries must be able to reorder events by effective date, which may differ from stream order. This is more faithful to accounting (a payment is "received" on its value date) but complicates the event store because stream order ≠ effective-date order.
- **Recorded event time:** time-travel queries are a simple prefix of the event stream; no reordering. This is simpler and matches what Marten provides natively, but a late-recorded payment does not appear in historical views until it is recorded.

ADR-0007 does not choose. AGENTS.md does not choose. **OWNER DECISION REQUIRED.**

### B.6 Event schema worksheet (structure only, no names)

For each event the owner approves, the worksheet below must be filled in before the event is implemented. No event names are proposed here.

| Field | Category | Type (NodaTime / value object / primitive) | Required | Notes |
|---|---|---|---|---|
| Aggregate identity | Identity | Owner-chosen id type | Yes | Must match the aggregate root id |
| Stream version | Identity | long | Yes | Marten event version at write time |
| Effective date | Temporal | `LocalDate` | Yes | Business date the event takes effect |
| Recorded time | Temporal | `Instant` | Yes | When the event was persisted |
| Command correlation | Causation | Owner-chosen correlation id | Yes | Links event to the command that caused it |
| Schema version | Versioning | int | Yes | Per-event-type version |
| (domain-specific) | Domain payload | Per-event | Yes | Defined when the event is approved |

## C. Persistence and concurrency

### C.1 ADR-0007 summary (Proposed, not accepted)

ADR-0007 proposes:

- Marten event streams are the authority for event-sourced aggregate state.
- Projections are derived, rebuildable query data.
- EF Core is used only for independently owned relational data outside those streams; a projection is not an alternate source of loan state.
- Application defines persistence ports; Infrastructure implements them.
- Aggregate writes are protected by expected stream versions.
- Idempotency outcomes are persisted atomically with the authoritative state change.
- If one transaction cannot cover a workflow, an outbox/inbox design must be reviewed before implementation.

**Status: PROPOSED.** The owner has not accepted this. The questions below are still open.

### C.2 Open persistence and concurrency questions (from ADR-0007 and derived)

| # | Question | Status | Notes |
|---|---|---|---|
| C-1 | Which aggregates are event-sourced, and which facts are relational? | **UNRESOLVED** | At minimum, the servicing `Loan` is a candidate for event sourcing. The owner must confirm whether `LoanApplication` (underwriting) is also event-sourced or is a simpler workflow. |
| C-2 | Which projections must support immediate read-after-write, and which may lag? | **UNRESOLVED** | Affects whether the servicing API can return a loan immediately after a payment is recorded, or whether it can tolerate a short lag. |
| C-3 | What is the transaction scope of payment recording and origination handoff? | **UNRESOLVED** | A payment recording may need to update the loan stream, a balance projection, and a relational payment log in one transaction. The handoff may need to create a loan stream and update an application record. |
| C-4 | Where are idempotency keys, request fingerprints, and replayed responses stored? | **UNRESOLVED** | ADR-0007 says "atomically with the authoritative state change" but does not say whether that is in Marten, EF Core, or a side table. |
| C-5 | What is the expected-version behavior on a concurrent write? | **UNRESOLVED** | Does the application retry, return a conflict error, or both? What is the retry policy? |
| C-6 | How are duplicate requests (same idempotency key, same payload) handled? | **UNRESOLVED** | Return the original response, or re-execute? |
| C-7 | How are changed-payload replays (same idempotency key, different payload) handled? | **UNRESOLVED** | Reject, or treat as a new request? |
| C-8 | What are the crash/retry outcomes? | **UNRESOLVED** | If the process crashes between appending the event and updating the projection, what is the recovery path? |
| C-9 | What is the projection rebuild expectation? | **UNRESOLVED** | Can projections be rebuilt from the event store at will? Is a rebuild a supported operational task or an emergency-only task? |
| C-10 | What is the event-schema change policy? | **UNRESOLVED** | See §B.4 versioning questions. |

### C.3 Marten event state vs. independently owned EF data

ADR-0007 (Proposed) draws a line:

- **Marten event state:** the authoritative state of the aggregate (e.g., the loan's current state, balance, and history).
- **EF Core relational data:** independently owned facts that are not part of the aggregate (e.g., a payment log table owned by a different team or concern, audit logs, reference data).

**Constraint:** a projection must not be written as an alternate authoritative path. If a balance is in the event stream, it must not also be updated by a separate EF Core write that could diverge.

**UNRESOLVED** — which specific facts fall on each side of the line. The owner must name them.

### C.4 Application ports and Infrastructure responsibilities (conceptual)

ADR-0007: "Application defines persistence ports and workflow transaction requirements. Infrastructure implements them."

- **Application (conceptual, no signatures yet):**
  - A port for appending events to an aggregate stream with an expected version.
  - A port for reading the current state of an aggregate (replay from the stream).
  - A port for reading projections (active loans, overdue loans, portfolio summary).
  - A port for reading historical state (time-travel).
  - A port for idempotency (check-and-store an idempotency key atomically with the state change).
  - Transaction requirements: which operations must be in one transaction, which may be eventual.

- **Infrastructure (conceptual):**
  - Marten configuration (connection string, schema, event store options).
  - Projection definitions and registration.
  - Idempotency storage (Marten, EF Core, or a side table — owner decision).
  - Concurrency handling (expected-version enforcement, retry policy).
  - Projection rebuild tooling.

**Exact signatures are pending the approved contracts.** Do not invent them.

### C.5 Immediate vs. eventual read requirements

- **UNRESOLVED** — the owner must decide, for each projection (active loans, overdue loans, portfolio summary, historical state), whether reads must be immediately consistent with the last write or may lag.
- If a projection may lag, the API must either (a) accept the lag and document it, or (b) read the aggregate directly for immediate consistency at the cost of performance.

## D. Conditional implementation tickets

The following tickets are **conditional on owner approval** of the decisions in §E. They are not authorized to start until the first coding ticket's prerequisites are met.

### Ticket T1 — Domain contract and its tests

**Goal:** Implement the `Loan` aggregate root (or the first slice of it) with invariants enforced at construction and on every command, plus the full unit test suite for the approved transition table.

**Prerequisites (all resolved and accepted on 2026-09-10):**

- §A.2: ✅ **ACCEPTED** — Disbursement.
- §A.3: ✅ **ACCEPTED** — Immutable per version; new terms version on change.
- §A.4: ✅ **ACCEPTED** — `LoanId` (Guid); `LoanApplicationId` as idempotency key.
- §B.1: ✅ **ACCEPTED** — AGENTS.md §8 state names as-is.
- §B.2: ✅ **ACCEPTED** — Invalid transitions throw `InvalidStateTransitionException`.
- §B.3: ✅ **ACCEPTED** — Business validation → `Result<T>`; prohibited transition → throw.
- §B.4: ⏳ **DEFERRED to T6** — Event-field categories listed; versioning policy deferred.
- §B.5: ✅ **ACCEPTED** — Business-effective time.
- §B.6: ⏳ **DEFERRED to T3** — Event schema worksheet structure given; domain-payload fields to be filled at implementation time.

**Allowed files (proposed; exact paths to be confirmed when the ticket is unblocked):**

- `src/Promissio.Domain/Loan/Loan.cs` (proposed) — the aggregate root.
- `src/Promissio.Domain/Loan/LoanState.cs` (proposed) — the state enum (name owner-approved).
- `src/Promissio.Domain/Loan/Events/` (proposed) — event records (names owner-approved).
- `src/Promissio.Domain/Loan/InvalidStateTransitionException.cs` (proposed) — the exception type (name fixed by AGENTS.md §6).
- `tests/Promissio.Domain.Tests/Loan/LoanTests.cs` (proposed) — unit tests.
- `docs/domain/loan-state-machine.md` (proposed) — the state machine diagram and transition table, kept in sync with code (AGENTS.md §8).

**Success cases (from the approved transition table):**

- Each approved transition produces the expected resulting state and emits the expected event with the expected payload.
- The aggregate's invariants hold after every command.

**Failure cases:**

- Each invalid (state, command) pair throws `InvalidStateTransitionException` with the current state, attempted transition, and a reason.
- Each expected business failure returns `Result<T>` with the expected error (per the owner-approved boundary in §B.3).
- Construction with invalid arguments throws the appropriate exception (per the existing value-object conventions).

**Verification commands:**

- `dotnet build` (must succeed).
- `dotnet test tests/Promissio.Domain.Tests` (must pass).
- `dotnet format --verify` on the changed files (or `dotnet format` to fix, then verify).
- `pwsh ./tools/check-documentation.ps1` (if `docs/domain/loan-state-machine.md` is added).

**Reviewer:** Owner (domain owner for the transition table; architecture reviewer for the event schemas).

**Status: UNBLOCKED as of 2026-09-10.** All E-1 through E-9 prerequisites are resolved (E-6 and E-8 are deferred to later tickets T6 and T3 respectively, but do not block T1). The owner has accepted all blocking decisions on 2026-09-10.

### Ticket T2 — Creation / replay foundation

**Goal:** Implement the ability to create a `Loan` from approved terms (the handoff) and to replay a stream of events to reconstruct the current state.

**Prerequisites:** T1 accepted and merged; §C-1 (which aggregates are event-sourced) resolved; §C-3 (transaction scope of the handoff) resolved; ADR-0007 accepted (or the relevant subset accepted).

**Allowed files (proposed):**

- `src/Promissio.Domain/Loan/LoanFactory.cs` or equivalent (proposed) — a static or instance factory that creates a `Loan` from approved terms and returns the creation event.
- `src/Promissio.Application/LoanCreation/` (proposed) — the command handler that orchestrates the handoff.
- `src/Promissio.Infrastructure/LoanPersistence/` (proposed) — the Marten stream configuration and the replay logic.
- `tests/Promissio.Application.Tests/LoanCreationTests.cs` (proposed).
- `tests/Promissio.Integration.Tests/LoanCreationTests.cs` (proposed) — real PostgreSQL via Testcontainers.

**Success cases:**

- Creating a `Loan` from approved terms appends the creation event to the stream.
- Replaying the stream reconstructs the same state.
- A second creation with the same idempotency key does not append a second event (returns the original).

**Failure cases:**

- A creation with a conflicting expected version (the stream already exists) returns a conflict error.
- A creation with an invalid idempotency-key/payload pair is rejected.

**Verification commands:**

- `dotnet build`
- `dotnet test tests/Promissio.Application.Tests`
- `dotnet test tests/Promissio.Integration.Tests` (requires Testcontainers with a real PostgreSQL)
- `pwsh ./tools/check-documentation.ps1`

**Reviewer:** Architecture reviewer (for the persistence design); domain owner (for the creation contract).

### Ticket T3 — One transition per ticket (repeat)

**Goal:** For each row in the approved transition table (rows 2–6 in §B.1), implement the command handler, the state change, the event emission, and the tests.

**Prerequisites:** T2 merged; the specific row's contract (command name, guard, event, effective date, recorded time) approved.

**Allowed files (proposed, per transition):**

- `src/Promissio.Domain/Loan/Loan.cs` — add the command method.
- `src/Promissio.Domain/Loan/Events/` — add the event record.
- `src/Promissio.Application/<TransitionName>/` — the command handler.
- `tests/Promissio.Domain.Tests/Loan/Loan<Transition>Tests.cs` — unit tests.
- `tests/Promissio.Integration.Tests/Loan<Transition>Tests.cs` — integration test with real PostgreSQL.

**Success / failure cases:** per the approved row.

**Verification commands:** same as T2.

**Reviewer:** Domain owner.

### Ticket T4 — Application workflow (end-to-end)

**Goal:** Wire one full workflow (e.g., "record a payment") from the command to the persisted state and the projection, with idempotency.

**Prerequisites:** T3 merged for the transitions the workflow uses; §C-3, §C-4, §C-5 resolved.

**Allowed files (proposed):**

- `src/Promissio.Application/<Workflow>/` — the workflow handler.
- `src/Promissio.Infrastructure/<Workflow>/` — the persistence implementation.
- `tests/Promissio.Integration.Tests/<Workflow>Tests.cs` — integration test.

**Verification commands:** same as T2.

**Reviewer:** Architecture reviewer.

### Ticket T5 — Idempotency

**Goal:** Implement the idempotency contract (check-and-store atomically with the state change) and the duplicate / changed-payload replay behavior.

**Prerequisites:** §C-4, §C-6, §C-7 resolved.

**Verification commands:** same as T2.

**Reviewer:** Architecture reviewer.

### Ticket T6 — Projections

**Goal:** Implement the approved projections (active loans, overdue loans, portfolio summary) as Marten projections, with rebuild tests.

**Prerequisites:** §C-1, §C-2, §C-9 resolved; ADR-0007 accepted.

**Verification commands:** same as T2, plus a projection-rebuild test.

**Reviewer:** Architecture reviewer.

### Ticket T7 — Historical queries (time-travel)

**Goal:** Implement time-travel queries (loan state as of any past date) per the owner's choice in §B.5.

**Prerequisites:** §B.5 resolved; T6 merged.

**Verification commands:** same as T2, plus at least 10 historical-scenario tests (per the Phase 3 acceptance criteria in `docs/plan/04-phase-03-loan-aggregate.md`).

**Reviewer:** Architecture reviewer.

### Out of scope for all tickets

- HTTP authorization (ADR-0008 is a later task).
- Batch processing.
- MCP tools.
- AI agents.
- Any change to Phase 2 schedule / APRC contracts (ADR-0004 is accepted and must not be changed without a new ADR).

## E. Decision checklist

### E.1 Minimum decisions needed to unblock T1 (the first coding ticket)

These are the only decisions that must be resolved before T1 can start. The owner does **not** need to decide the later tickets (T2–T7) yet.

| # | Decision | Section | Status |
|---|---|---|---|
| E-1 | Milestone at which the servicing `Loan` begins to exist. | §A.2 | **ACCEPTED 2026-09-10** — Disbursement. |
| E-2 | Whether approved terms are immutable and how changes before disbursement are handled. | §A.3 | **ACCEPTED 2026-09-10** — Immutable per version; new terms version on change. |
| E-3 | The identity of a `Loan` and the idempotency key for the handoff. | §A.4 | **ACCEPTED 2026-09-10** — `LoanId` (Guid); `LoanApplicationId` as idempotency key. |
| E-4 | The state names and the transition table (rows 1–6 in §B.1, filled in). | §B.1 | **ACCEPTED 2026-09-10** — AGENTS.md §8 names as-is. |
| E-5 | The boundary between expected business failures (`Result<T>`) and invalid-state-transition exceptions. | §B.3 | **ACCEPTED 2026-09-10** — Business validation → `Result<T>`; prohibited transition → throw. |
| E-6 | The event-field categories and the versioning policy. | §B.4 | **DEFERRED to T6** — Categories listed; versioning policy deferred. |
| E-7 | Whether historical state means business-effective time or recorded event time. | §B.5 | **ACCEPTED 2026-09-10** — Business-effective time. |
| E-8 | The event schema worksheet filled in for each event in the transition table. | §B.6 | **DEFERRED to T3** — Structure given; domain-payload fields per event to be filled in at implementation time. |
| E-9 | Confirmation that the existing `LoanApplication` record in Application is a DTO, an aggregate root that must move to Domain, or a mistake. | §A.5 | **ACCEPTED 2026-09-10** — Transient DTO. |

### E.2 Decisions needed later (T2–T7)

These are **not** required to unblock T1. They are listed so the owner can see the full decision surface.

| # | Decision | Section | Needed for |
|---|---|---|---|
| E-10 | Which aggregates are event-sourced, and which facts are relational. | §C-1 | T2 |
| E-11 | Which projections must support immediate read-after-write, and which may lag. | §C-2 | T4, T6 |
| E-12 | The transaction scope of payment recording and origination handoff. | §C-3 | T2, T4 |
| E-13 | Where idempotency keys, request fingerprints, and replayed responses are stored. | §C-4 | T5 |
| E-13a | Expected-version behavior on a concurrent write. | §C-5 | T5 |
| E-13b | Duplicate-request handling. | §C-6 | T5 |
| E-13c | Changed-payload replay handling. | §C-7 | T5 |
| E-13d | Crash / retry outcomes. | §C-8 | T5 |
| E-13e | Projection rebuild expectation. | §C-9 | T6 |
| E-13f | Event-schema change policy. | §C-10 | T6, T7 |

## F. Checks run for this documentation-only task

| Check | Command | Result |
|---|---|---|
| Branch and commit | `git branch --show-current; git rev-parse --short HEAD` | `main`, `ef634bc` |
| Working tree | `git status --short` | Dirty paths listed in §Baseline |
| Loan aggregate exists? | Grep `Loan` across `src/**/*.cs` | No `Loan` aggregate in Domain (evidence row 1) |
| Domain events exist? | Grep `Event` across `src/**/*.cs` | No aggregate events (evidence row 3) |
| State machine in code or docs? | Grep `State` across `src/**/*.cs`; `docs/domain/` listing | No loan state machine (evidence row 4) |
| ADR-0006 / ADR-0007 status | Read the two ADR files | Both **Proposed** (evidence rows 14) |
| ADR-0004 status | Read the ADR file | **Accepted** (evidence row 13) |
| `docs/plan/phase-3-contract-review.md` exists before this task? | `file_search` for the path | No (this file is new) |
| Documentation link check | `pwsh ./tools/check-documentation.ps1` | Exit code 0 — "Maintained documentation file links passed". |
| Whitespace / diff check | `git diff --check` | Exit code 0 — only pre-existing CRLF warnings on unrelated dirty files; no errors on this file. |
| New-file link check (manual) | PowerShell one-liner scanning local markdown links in this file | All local links resolve. (This file is untracked, so `check-documentation.ps1` does not scan it; the manual check covers the gap.) |

## G. Completion summary

- **Output file:** `docs/plan/phase-3-contract-review.md` (this file).
- **Checks actually run:** see §F. The `check-documentation.ps1` and `git diff --check` results are appended after execution.
- **Decisions resolved for the first coding ticket (T1):** E-1, E-2, E-3, E-4, E-5, E-7, E-9 ACCEPTED on 2026-09-10. E-6 deferred to T6. E-8 deferred to T3. **T1 is UNBLOCKED.**
- **Next proposed ticket after T1:** T2 (creation / replay foundation), which is blocked on E-10, E-11, E-12 and ADR-0007 acceptance.
- **This task does not start coding.** T1 is unblocked but has not been started.
