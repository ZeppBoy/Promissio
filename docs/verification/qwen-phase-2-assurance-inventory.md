# Q01 — Phase 2 assurance inventory (Qwen IQ3_M)

**Date:** 2026-09-10
**Status:** Complete — baseline green; inventory only, no code changes.

## Baseline

- Branch: `main`
- Commit: `ef634bcefaafd1e9639f7336f13fa047b83ef2a7` (verified descendant of expected `8b3c314`; exactly one commit ahead: `docs(development): add Qwen IQ3_M implementation handoff`)
- Working tree: clean (`git status --porcelain` produced no output).
- No files were reset, committed, or pushed during this task.

## Commands executed and observed results

| # | Command | Result |
|---|---|---|
| 1 | `git branch --show-current` / `git rev-parse HEAD` / `git status --porcelain` | `main`, `ef634bc…`, clean |
| 2 | `git merge-base --is-ancestor 8b3c314 HEAD` | exit 0 (expected baseline is an ancestor) |
| 3 | `pwsh ./tools/verify.ps1` | **Passed.** Build OK (16 projects). Tests: **405 total, 405 passed, 0 failed, 0 skipped**. Formatting (Phase 2 paths, batch, benchmarks): OK. Documentation links: OK. Benchmark case discovery: OK (`Promissio.Domain.Benchmarks` 8 cases, `Promissio.Benchmarks` 8 cases). |
| 4 | (pending, step 8 of packet) | `pwsh ./tools/check-documentation.ps1` — see "Report status" below |

Empty test projects (reported as such by the test runner, not failures):
`Promissio.Application.Tests`, `Promissio.Integration.Tests`, `Promissio.AI.Evals`.
`Promissio.Infrastructure.Tests` and `Promissio.BatchProcessor.Tests` contain only host/registration smoke tests.

## Contract-to-test mapping (ADR-0004)

Sources consulted: `docs/adr/0004-phase-2-schedule-and-aprc-contracts.md`, `docs/domain/payment-schedules.md`, `tests/Promissio.Domain.Tests/**` (18 test files), `stryker-config.json`, `tools/verify.ps1`, `Directory.Packages.props`.

Evidence classes: **ext** = externally sourced reference (Commission/SWD example); **ind** = independently worked calculation in `docs/domain/payment-schedules.md`; **alg** = algebraic/property check; **self** = self-consistency/present-value residual (implementation-derived, weaker assurance).

| # | Approved contract (ADR-0004) | Test(s) | Evidence class | Reference source |
|---|---|---|---|---|
| C1 | Annuity sizing via cent-grid search on the same `IInterestCalculator`; smallest payment covering interest, retiring principal by maturity | `Phase2ContractTests.Annuity_MatchesShortWorkedCases` (1010; 507.51); `ThreePeriodSchedule_MatchesWorkedPrincipalAndInterest` (kind 0) | ind | worked table in `docs/domain/payment-schedules.md` (EUR 12% 30E/360) |
| C2 | Differentiated: equal principal portions; residual at maturity | `ThreePeriodSchedule_…` (kind 1: 400/400/400, 12/8/4) | ind | same table |
| C3 | Bullet: principal retained until maturity; final settles balance | `ThreePeriodSchedule_…` (kind 2: 0/0/1200, 12/12/12) | ind | same table |
| C4 | Custom flows preserve supplied amounts and dates; never recomputed; copied input | `Custom_PreservesIrregularCashFlowsAndCopiesInput` | ind (assertions are input echoes) | ADR-0004 decision text |
| C5 | Custom grace validated against supplied principal portions, never inserted | `Custom_ValidatesGraceWithoutRewritingAmounts`; rejection: `Custom_RejectsInvalidFlows("grace")` | ind | ADR-0004 decision text |
| C6 | Custom validation: balance/currency/negative/date/count rejections | `Custom_RejectsInvalidFlows` (theory: balance, currency, negative, date, count) | — (negative contract) | ADR-0004 decision text |
| C7 | Holiday calendar moves payable dates forward; accrual keeps contractual dates | `Calendar_MovesPaymentButNotAccrualOrLaterContractualDates` (all three generators) | ind (synthetic 2024-06-03 holiday, business-day shift asserted) | ADR-0004 decision text |
| C8 | Explicit custom dates preserved; conflicting calendar rejected | `Custom_RejectsInvalidFlows("holiday")` (Saturday 2024-01-06 with calendar) | — (negative contract) | ADR-0004 consequences |
| C9 | Optional first payment date; short/long first period; subsequent dates anchored to it | `ShortAndLongFirstPeriods_UseContractualAccrual` (15d → 5.60 EUR interest = 14/360; 61d → 24 EUR = 60/360) | ind | worked 30E/360 day fractions |
| C10 | Month-end anchoring without February drift (no repeated clamping) | `MonthEnd_DoesNotDriftAfterFebruary` (2024-01-31 → 02-29, 03-31, 04-30) | ind (calendar arithmetic) | ADR-0004 decision text |
| C11 | No overpayment: principal never exceeds original balance; early settlement / trailing zeros permitted | `TinyPrincipal_LongTerm_IsNeverOverpaid` (0.01 EUR, 360 months, non-negative portions, exact sum); `ScheduleBalancePropertyTests.*Randomized` (200 randomized draws each generator) | alg | — |
| C12 | Money currency invariants on `PaymentScheduleItem` | `PaymentItem_RejectsMixedCurrencies` | alg | `Money` contract |
| C13 | APRC solves PV equation using payment **dates** (SWD(2012)128 §4.1.1), not period labels | `AprcReferenceTests.PublishedCommissionExamples_MatchToFourDecimalPercentagePlaces` (examples 2–5, tolerance 0.0000005) | ext | COM(2005)483 final/2, Annex II, pp. 59–61 |
| C14 | APRC time intervals (monthly/annual/weekly units; leap-year denominators) | `AprcDatedTests.PublishedMonthlyIntervals_AreUsedInsteadOfPeriodNumbers` (10 dated cases incl. 2013 leap years), `PublishedAnnualIntervals_RespectSelectedFrequency` (3), `WeeklyFrequency_UsesEqualWeeks` | ext (interval values) + ind (inverted single-payment rates) | SWD(2012)128, pp. 23–26 |
| C15 | APRC invariance: moving dates changes APRC; relabelling periods does not | `MovingDates_ChangesAprcButRelabellingPeriodsDoesNot` | self | — |
| C16 | Upfront fee equivalent to net drawdown (never both) | `UpfrontFee_IsEquivalentToNetDrawdown` | alg | ADR-0004 consequences |
| C17 | `TryCalculate` returns failures instead of throwing; `Calculate` throws for compatibility | `InvalidOrUnsolvableInputs_ReturnFailureWithoutThrowing` (9 failure modes) | — (error contract) | ADR-0004 decision text |
| C18 | Zero-cost loan returns exact zero; large-rate bracket expansion | `ZeroCost_ReturnsExactZero`; `LargeAnnualRate_ExpandsBracket` | ind (closed-form) | — |
| C19 | Legacy loan configurations retained as PV-residual regressions (no sourced rate) | `OriginalLoanConfigurations_SatisfyPresentValueEquation` (3 configs, PV residual ≈ 1e-6) | self | none (deliberately unsourced, labelled in test) |
| C20 | Canonical 4 snapshots (USD 10,000 fixtures), independently checked with Python Decimal before acceptance | `ScheduleSnapshotTests.*CanonicalCase_MatchesSnapshot` (Annuity/Differentiated/Bullet/Custom) | ext (prior independent Decimal check recorded in `docs/domain/payment-schedules.md`) | docs record; snapshot files under test project |
| C21 | Annuity/differentiated/bullet generate balanced schedules (principal sums to original, grace principal = 0) | `ScheduleGenerationTests.*` | self | — |
| C22 | Day-count conventions underpinning C1–C3, C9 (30E/360, A/A, A/360, A/365) | `DayCountConventionTests` (`Actual360Tests`, `Actual365Tests`, `ActualActualTests`, `Thirty360Tests`, `Thirty360EuropeanTests` — ~23 vectors each) | **unknown provenance** — vectors are plausible but no ISDA/ECB citation is present in the test file header (AGENTS.md §8 requires cited reference vectors) | see gap G4 |
| C23 | Interest engine path used by all schedules (single calculator, banker's rounding) | `InterestCalculatorTests` | self (expected values computed in-test from the same formula, not from an external table) | — |

## Assurance evidence status for this checkout

- **Line coverage (target ≥90% for Domain, AGENTS.md §7):** not measured in this checkout. No coverage collector (e.g., Coverlet) is declared in `Directory.Packages.props`, and no coverage report is checked in. Status: **unknown** (not zero). The 405-passing-test result does not establish a score.
- **Mutation score (target ≥80% for Domain, Stryker):** configuration exists and is correct for the task: `stryker-config.json` selects `Promissio.Domain.csproj` by filename with break threshold 80; tool pinned at `dotnet-stryker` 4.14.2 in `dotnet-tools.json`; documented invocation from `tests/Promissio.Domain.Tests` in `docs/verification/README.md`. No mutation report for commit `ef634bc` exists in `docs/verification/` or `StrykerOutput/` (existing `StrykerOutput/` directories predate this checkout and are not cited by any current evidence file). Status: **unknown** for this checkout.
- **Financial reference evidence:** APRC has externally sourced examples (C13, C14) with 50-digit independent calculations recorded in docs. Schedule strategies rest on the independently worked table (C1–C3, C7, C9) plus snapshot cross-check (C20). Day-count vector provenance is not cited (C22).
- **Residual/self-consistency checks** (C15, C19, C21, C23) verify internal consistency only and must not be counted as independent financial evidence.

## Identified gaps

1. **G1 — No coverage measurement.** The ≥90% Domain line-coverage target is unmeasured; no collector declared, no artifact retained.
2. **G2 — No mutation score for this checkout.** Stryker is configured and pinned but has not been run against `ef634bc`; no survivor classification exists.
3. **G3 — Snapshot independence is historical.** The four canonical snapshots rely on a one-time Python Decimal cross-check recorded in docs; there is no repeatable, checked-in independent calculation to re-run on fixture changes.
4. **G4 — Day-count test vector provenance is uncited.** `DayCountConventionTests` contains ~115 date vectors across five conventions but no ISDA/ECB reference citation, which AGENTS.md §8 requires for day-count implementations.
5. **G5 — APRC legal-disclosure rounding and national-calculator alignment are out of scope by ADR-0004** and remain unverified; disclosure-formatting behavior is not tested (explicitly not inferred).

## Proposed follow-up tasks (ranked by financial risk, max five)

1. **Q02 — Measure Domain line coverage** (closes G1). Scope: `Directory.Packages.props` (add collector if architect approves version), one test project file (collector config), one verification script/document. Acceptance: deterministic command, retained report artifact, line numerator/denominator/percentage with generated-code treatment documented; financial code not excluded.
2. **Q03 — Run Stryker and classify survivors** (closes G2). Scope: `stryker-config.json` (read-only), `dotnet-tools.json` (read-only), new dated report under `docs/verification/`. Acceptance: source commit, tool version, command, killed/survived/timed-out counts as reported; every survivor addressed by a proposed test naming specific independent expected behavior; no inferred score from partial runs.
3. **Repeatable independent schedule cross-check** (closes G3). Scope: one checked-in script or test fixture generator under `tools/` (or `tests/`) implementing the Decimal-based row-by-row verification described in `docs/domain/payment-schedules.md`; the four canonical fixtures plus the worked-table cases. Acceptance: script output matches current snapshots/expectations at `ef634bc`; any mismatch blocks rather than updates snapshots; command recorded in `docs/verification/README.md`.
4. **Cite day-count reference vectors** (closes G4). Scope: `tests/Promissio.Domain.Tests/Calculations/DayCountConventionTests.cs` (header citations and vector corrections only — no implementation change) plus `docs/domain/day-count-conventions.md`. Acceptance: each of the five conventions has ≥20 vectors with an authoritative source cited in the test header and formula doc; any vector whose source value differs from the current expectation is reported, not silently changed (requires owner confirmation per AGENTS.md §8).
5. **APRC disclosure/rounding boundary documentation test** (bounds G5 explicitly, no behavior change). Scope: one test file asserting and documenting the *unsupported-by-design* behaviors (results not rounded for legal disclosure; no national calendar fabrication; negative-rate rejection) so future regressions are caught. Acceptance: each assertion maps to a named ADR-0004 sentence; no new public API.

## Report status

- This file was newly created (no pre-existing `qwen-phase-2-assurance-inventory.md` was found).
- `pwsh ./tools/check-documentation.ps1` is run after this report is written; result recorded in the final session report.
