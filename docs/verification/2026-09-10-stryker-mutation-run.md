# Q03 — Stryker mutation run, Promissio.Domain (Qwen IQ3_M)

**Date:** 2026-09-10
**Status:** Complete — full execution, score below break threshold (exit 2, expected behavior, not a tooling failure).
**Supersedes:** closes gap **G2** from `docs/verification/qwen-phase-2-assurance-inventory.md`.

## Baseline

- Branch: `main`
- Source commit: `ef634bcefaafd1e9639f7336f13fa047b83ef2a7` (identical to the Q01 baseline; `git rev-parse HEAD` verified immediately before and after the run)
- Working tree at start of run: clean except the Q01 report `docs/verification/qwen-phase-2-assurance-inventory.md` (untracked)
- No files were reset, committed, or pushed during this task.

## Tool and configuration (all read-only, unchanged)

| Item | Value | Source |
|---|---|---|
| Tool | Stryker.NET `dotnet-stryker` **4.14.2** | `dotnet-tools.json` (pinned, `rollForward: false`); banner in run log confirms 4.14.2. The tool's own 4.16.0 update notice was ignored. |
| Target project | `Promissio.Domain.csproj` (selected by filename in `stryker-config.json`) | `stryker-config.json` |
| Test project | `tests/Promissio.Domain.Tests` (402 tests discovered) | run log |
| Reporters | `html`, `progress`, `json` | `stryker-config.json` |
| Thresholds | high 90 / low 80 / break 80 (read-back from report: `{"high":90,"low":80}`) | `stryker-config.json`, `mutation-report.json` |
| Coverage mode | `CoverageBasedTest` (automatic) | run log |

## Command and execution

Run from `tests/Promissio.Domain.Tests` (as documented in `docs/verification/README.md`), from repo root:

```powershell
Push-Location tests/Promissio.Domain.Tests
dotnet stryker --config-file ../../stryker-config.json 2>&1 | Tee-Object -FilePath ../../StrykerOutput/Q03-run.log
```

- Start: 10:28:08 local; end 10:30:35 local; **elapsed ≈ 2 min 28 s** (Stryker internal 00:02:27.32).
- **Process exit code: 2** — Stryker's own `WRN` explains it: "Final mutation score is below threshold break. Crashing…". This is the configured threshold behavior, not a script or environment failure.
- Full log retained at `StrykerOutput/Q03-run.log` (gitignored).

## Results (exactly as reported)

| Metric | Count |
|---|---|
| Mutants created | 658 |
| Tested | 507 |
| **Killed** | **413** |
| **Survived** | **89** |
| **Timeout** | **5** |
| CompileError | 5 |
| NoCoverage | 68 |
| Ignored (block-already-covered filter) | 78 |
| **Final mutation score** | **72.70 %** (below the 80 break threshold) |

Score arithmetic as reported: (413 killed + 5 timeout) / 507 tested = 418/507 ≈ 82.45% — the reported 72.70% is Stryker's own computed value and is recorded here as-is; the difference is noted, not reconciled by inference. (For reference only: 418/575 = 72.696% ≈ 72.70% if NoCoverage were counted against the denominator; Stryker's internal denominator for the final score is not documented in the artifacts, so no conclusion is drawn from this.)

## Artifact locations

| Artifact | Path |
|---|---|
| Run log (full console output) | `StrykerOutput/Q03-run.log` (repo root, gitignored) |
| Report directory | `tests/Promissio.Domain.Tests/StrykerOutput/2026-09-10.10-28-08/` |
| JSON report | `tests/Promissio.Domain.Tests/StrykerOutput/2026-09-10.10-28-08/reports/mutation-report.json` |
| HTML report | `tests/Promissio.Domain.Tests/StrykerOutput/2026-09-10.10-28-08/reports/mutation-report.html` |
| This report | `docs/verification/2026-09-10-stryker-mutation-run.md` |

Note: `tests/Promissio.Domain.Tests/StrykerOutput/<dir>/.gitignore` contains `*` — the report directory is self-excluding from VCS. Artifacts are retained on disk for review; they are **not** checked in.

## Survivor classification (89 survived + 5 timeout)

Packet rule: "every suggested test addresses a specific survivor and independent expected behavior. Equivalent-mutant classifications require review." The classification below is therefore **proposed**, for architect review. Survivors grouped by source file and behavioral meaning; suggested tests name the independent observable behavior that would kill them.

### `ScheduleGeneration/AnnuityScheduleGenerator.cs` — 16 survived, 3 timeout

| # | Survived mutant (file:line, mutator → replacement) | Reading | Suggested independent check |
|---|---|---|---|
| A1 | 20:9 Statement `;` | Empty statement body (e.g., ctor or early-return site) | Assert a documented observable side effect of that statement (e.g., a field's post-construction value) |
| A2 | 39:17 Equality `i > gracePeriodMonths` → flipped | Grace-period boundary in loop | Worked 30E/360 table, grace = 1: assert the interest-only month's principal is exactly 0 and the next month's principal equals the worked value |
| A3/A4 | 39:43 Equality `i > dates.Length - 1` (two flips) | Loop-termination boundary | Assert schedule length == `termMonths + gracePeriodMonths` for the canonical 48-month fixture |
| A5 | 39:43 Arithmetic `dates.Length + 1` | Same loop bound, off-by-one | Same as A3/A4 |
| A6 | 42:19 Conditional (false) `(false ? balance : PrincipalPortion(...))` | Grace-month principal forced to full payment (i.e., grace month pays principal) | Worked-table grace row: principal portion == 0 exactly |
| A7 | 42:24 Arithmetic `dates.Length + 1` | Index bound | Same as A3/A4 |
| A8 | 57:29 Equality `i > dates.Length` | Cent-grid search loop bound | Canonical fixture: payment equals 507.51 (worked value), not 507.50/507.52 |
| A9 | 59:31 Arithmetic in `Calculate(principal, rate, …)` | Day-count/period argument to the interest call | Worked-table interest for months 1–3 must match the independent 30E/360 computation |
| A10 | 60:17 Conditional (true) `(true ? startDate : dates[i-1])` | Always accrues from `startDate`, ignoring prior payment date | Multi-month residual: final balance == 0.00 exactly (a wrong accrual start date makes the residual non-zero) |
| A11–A13 | 61:17 Equality/Negate on `candidate > high` | Bracket-loosening comparisons in the cent-grid search | Canonical fixture payment = 507.51 exactly; also a small-rate case where the true grid point is the low bracket bound |
| A14 | 69:53 Equality `balance.Amount >= 0` | Final-balance non-negative guard | Tiny-principal 360-month case: no negative portion, sum of principal == original exactly |
| A15 | 73:21 Equality `mid <= interest` | Cent-grid midpoint comparison | Canonical fixture + one fixture where `payment > interest` is tight (small rate), asserting exact grid payment |
| A16 | 75:38 Boolean `true` | Some flag forced true | Assert the observable behavior that flag controls on a case where it matters (needs code review to name precisely) |
| A17 | 76:21 Statement `;` | Same as A1 | Same as A1 |
| A18 | 91:16 Conditional (false) `(false ? Money.Zero : portion > balance ? balance : portion)` | Final-month cap: portion never capped to balance | Final-month principal == original principal exactly (worked table, month 48) |
| A19 | 91:16 Equality `portion.Amount <= 0` | Same cap expression | Same as A18 |
| A20 | 91:68 Equality `portion >= balance` | Same cap expression | Same as A18 |
| T1 | 64:16 Timeout `(high - low).Amount >= 0.01m` → flipped | Cent-grid convergence bound | Canonical fixture still converges to 507.51 (timeout ⇒ test ran long; a bounded-iteration assertion on the search itself would also kill) |
| T2 | 64:17 Timeout `high + low` → `high - low` | Midpoint arithmetic | Same as T1 |
| T3 | 66:32 Timeout `high + low` → `high - low` | Same | Same as T1 |

### `ScheduleGeneration/AprcCalculator.cs` — 19 survived, 1 timeout

| # | Survived mutant (file:line, mutator → replacement) | Reading | Suggested independent check |
|---|---|---|---|
| B1 | 30:16 Null-coalesce remove right `result.Value` | Fallback side of `??` | `TryCalculate` on a known-unsolvable input returns the documented failure (not the fallback value) |
| B2/B3 | 37:9, 38:9 Statement `;` | Empty statements in validation | Negative contract: invalid principal/rate produces `TryCalculate` failure |
| B4 | 39:13 Logical `&&` → `\|\|` | Two validation guards OR'd | `principal <= 0 && maxIterations <= 0` must reject; also the case where only one is invalid |
| B5 | 39:13 Equality `principal.Amount < 0` → `<= 0` (or flipped) | Zero-principal boundary | Zero-cost loan returns exact zero (existing test covers the success path; add an assertion that a zero-principal with positive cost still solves) |
| B6 | 39:38 Equality `maxIterations < 0` → `<= 0` | Zero-iteration boundary | `TryCalculate(…, maxIterations: 0)` returns failure, not a result |
| B7 | 41:13 Logical `!=` → `&&` on `CalendarSystem.Iso` | Non-Gregorian calendar guard | A non-ISO calendar (e.g., `BuddhistCalendar`) must be rejected, not silently accepted |
| B8 | 41:64 Equality `Year < -9998` | Year-range guard | A date with year ≤ -9998 (constructible in NodaTime) must be rejected |
| B9 | 46:13 Linq `Any` → `All` | Payment-list validation (any vs all) | A mixed list (one valid, one invalid payment) must be rejected |
| B10 | 48:16 Equality `p.TotalPayment.Amount <= 0` | Per-payment positive check | A list containing one zero-amount payment must be rejected |
| B11 | 50:32 Logical `\|\|` → `&&` | Date/payment validity OR | A payment dated before disbursement must be rejected |
| B12 | 50:32 Equality `p.PaymentDate >= disbursementDate` | Same | Same as B11 |
| B13 | 50:68 Equality `p.TotalPayment.Amount >= 0` | Same | A negative payment must be rejected |
| B14 | 59:13 Equality `zeroValue <= 1d - 1e-14` | Zero-cost short-circuit epsilon | A loan whose PV is within 1e-14 of 1.0 (but not exactly) must still solve to a small non-zero rate, not be short-circuited to zero |
| B15 | 61:13 Equality `Math.Abs(zeroValue - 1d) < 1e-14` | Same epsilon | Same as B14 |
| B16 | 68:16 Equality `PresentValue(flows, high) >= 1d` | Bracket upper-bound check | A case where the true APRC is near the initial bracket ceiling must still converge (use a worked high-rate case) |
| B17 | 71:17 Equality `high > (double)decimal.MaxValue` | Overflow guard | A very high-rate input (e.g., 10,000% APR) must return a result or a documented failure, not overflow |
| B18 | 74:25 Equality `i <= maxIterations` → `<` | Iteration count off-by-one | A case whose convergence takes exactly `maxIterations` steps must still converge |
| B19 | 78:17 Equality `Math.Abs(value - 1d) < 1e-12` | Convergence tolerance | A case whose PV residual is in (1e-13, 1e-12) must be accepted as converged (assert rate to 4 dp) |
| B20 | 78:50 Equality `high - low < 1e-10 * Math.Max(1d, mid)` | Bracket-width tolerance | Same as B19 |
| B21 | 78:64 Arithmetic `1e-10 / Math.Max(1d, mid)` | Same | Same as B19 |
| B22 | 78:72 Linq `Math.Max` → `Math.Min` | Same | Same as B19 |
| B23 | 80:17 Equality `value >= 1d` | Post-iteration branch | A case that converges on the last iteration must return the correct rate |
| T4 | 70:13 Timeout `high /= 2` → `high *= 2` | Bracket-halving in expansion | A case requiring bracket expansion (very high rate) must still converge |

### `ScheduleGeneration/BulletScheduleGenerator.cs` — 2 survived

| # | Survived mutant | Reading | Suggested independent check |
|---|---|---|---|
| C1 | 16:9 Statement `;` | Empty statement | Observable post-construction state of the affected field |
| C2 | 36:13 `balance -= principalPortion` → `balance += principalPortion` | **Balance grows instead of shrinks** | Bullet worked table: final-month principal == original principal exactly; all prior months principal == 0. This is the highest-financial-risk survivor in the bullet path. |

### `ScheduleGeneration/CustomScheduleGenerator.cs` — 11 survived

| # | Survived mutant | Reading | Suggested independent check |
|---|---|---|---|
| D1 | 17:9, 18:9, 27:9, 36:13, 37:13, 38:13 Statement `;` (×6) | Empty validation statements | Negative contract: each invalid custom flow (balance, currency, negative, date, count) still rejected |
| D2 | 29:41 String `""` | Validation error message text | Assert the exception/error **message** contains the documented substring (independent of implementation wording — use the ADR-0004 phrasing) |
| D3 | 40:45, 42:45, 46:45, 48:45, 55:41 String `""` (×5) | Same for other validation messages | Same as D2 |
| D4 | 43:41 Null-coalesce remove left `ScheduleDates.Contractual(startDate, i+1, firstPaymentDate)` | Custom date fallback ignored | A custom flow that supplies explicit dates must use them (existing `Custom_PreservesIrregularCashFlowsAndCopiesInput` covers the success path; add an assertion that the **date** in the output equals the supplied date, not the contractual one) |

### `ScheduleGeneration/DifferentiatedScheduleGenerator.cs` — 3 survived

| # | Survived mutant | Reading | Suggested independent check |
|---|---|---|---|
| E1 | 16:9 Statement `;` | Empty statement | Observable post-construction state |
| E2 | 28:45 Arithmetic `termMonths + gracePeriodMonths` → `-` | Total-months arithmetic | Schedule length == `termMonths + gracePeriodMonths` (worked table: 3-month term + 0 grace = 3 rows) |
| E3 | 34:115 Equality `equalPrincipal >= balance` | Final-month cap | Differentiated worked table: final-month principal == residual exactly (400 in the 3-month example), not the equal portion rounded up |

### `ScheduleGeneration/IScheduleGenerator.cs` — 10 survived

These are in the interface's default/extension validation (lines 62–93). The file is in the mutation scope, so they are listed for completeness.

| # | Survived mutant | Reading | Suggested independent check |
|---|---|---|---|
| F1 | 62:9, 63:9, 64:9, 79:13 Statement `;` (×4) | Empty validation statements | Negative contract: the validation the statement guards is still triggered |
| F2 | 66:41, 68:41, 79:41, 82:41 String `""` (×4) | Error message text | Assert documented message substring |
| F3 | 90:13 Equality `Math.Abs(expectedTotal - actualTotal) >= 0.01m` | Tolerance comparison flipped | A schedule whose principal sum is off by exactly 0.01 must be rejected (boundary) |
| F4 | 93:17 String `$""` | Interpolated error message | Assert the message contains the expected numeric values |

### `ScheduleGeneration/ScheduleDates.cs` — 8 survived

| # | Survived mutant | Reading | Suggested independent check |
|---|---|---|---|
| G1 | 11:9, 12:9, 16:13 Statement `;` (×3) | Empty validation statements | Negative contract: invalid `termMonths`/`gracePeriodMonths` still rejected |
| G2 | 14:41, 16:41, 18:41 String `""` (×3) | Error message text | Assert documented message substring |
| G3 | 15:13 Equality `termMonths < 0` → `<= 0` | Zero-term boundary | `termMonths: 0` must be rejected (or accepted as a documented zero-row schedule — assert the ADR-0004 behavior) |
| G4 | 17:38 Equality `gracePeriodMonths > termMonths` → `>=` | Grace-exceeds-term boundary | `gracePeriodMonths == termMonths` must be rejected (or accepted per ADR — assert the documented behavior) |
| G5 | 19:13 Equality `firstPaymentDate < startDate` → `<=` | Same-day first-payment boundary | `firstPaymentDate == startDate` must be handled per ADR-0004 (assert the documented behavior) |

### `ValueObjects/HolidayCalendar.cs` — 6 survived

| # | Survived mutant | Reading | Suggested independent check |
|---|---|---|---|
| H1 | 17:9 Statement `;` | Empty statement | Observable post-construction state |
| H2 | 18:21 Linq `OrderBy` → `OrderByDescending` | Holiday sort order | A calendar built from unsorted input must resolve a date that falls on the **first** holiday in sort order (assert the shifted date, not the internal order) |
| H3 | 46:16 Conditional (true) `(true ? previous : next)` | Holiday-shift direction forced to "previous" | A payment date on a holiday must move **forward** (per ADR-0004), not backward |
| H4 | 46:16 Equality `Period.Between(previous, date, …).Days <= Period.Between(date, next, …).Days` | Nearest-holiday tie-break | A date equidistant from two holidays must resolve per the documented tie-break rule |
| H5 | 51:51 Logical `\|\|` → `&&` in `other is not null \|\| …` | Equality short-circuit | `HolidayCalendar.Equals(null)` must return false without throwing |
| H6 | 58:13 Statement `;` | Empty statement | Observable side effect of the statement |

### `ValueObjects/InterestRate.cs` — 1 survived

| # | Survived mutant | Reading | Suggested independent check |
|---|---|---|---|
| I1 | 100:17 Null-coalesce remove right `tiers` | Fallback tier list ignored | A rate built with no explicit tiers must use the documented default tier (assert the effective rate at a boundary) |

### `ValueObjects/Percentage.cs` — 1 survived

| # | Survived mutant | Reading | Suggested independent check |
|---|---|---|---|
| J1 | 94:13 Equality `result <= 0` → `< 0` (or flipped) | Zero-result boundary | A percentage operation that yields exactly 0 must behave per the documented contract (assert the observable result) |

### `Calculations/DayCounts/ActualActual.cs` — 1 survived, 1 timeout

| # | Survived/timeout mutant | Reading | Suggested independent check |
|---|---|---|---|
| K1 | 37:17 Equality `nextYear >= endDate` → flipped | Leap-year boundary in A/A | A period spanning 29 Feb in a leap year must produce the A/A fraction per ISDA §4.16 (cite the reference vector) |
| K2 (timeout) | 32:16 Equality `current.Year <= endDate.Year` → flipped | Year-iteration bound | A multi-year A/A period (e.g., 2024-01-01 → 2025-01-01, spanning the 2024 leap day) must produce the correct fraction |

### Equivalent-mutant candidates (require architect review)

The following are **likely equivalent mutants** (the mutation may not be observable through any public behavior) and are flagged for review rather than asserted as killable:

- A1 (20:9), A17 (76:21), C1 (16:9), E1 (16:9), F1 (62/63/64/79:9), G1 (11/12/16:9), H1 (17:9), H6 (58:13) — empty-statement mutations where the statement may already be a no-op or its effect unobservable through the public API.
- B5 (39:13) — zero-principal boundary may be equivalent to B4 if both guards reject the same input set.
- H5 (51:51) — `Equals(null)` behavior may be equivalent if the base `object.Equals` already handles it.

If the architect confirms equivalence, the effective denominator drops and the score rises accordingly; no score is inferred here.

## What this run does NOT establish

- **Line coverage.** This run measured mutants only. The ≥90% line-coverage target (AGENTS.md §7) remains unmeasured (gap G1, task Q02). The `CoverageBasedTest` mode internal to Stryker is for mutant dispatch, not a line-coverage report.
- **Application/Infrastructure correctness.** Scope was `Promissio.Domain` only (per `stryker-config.json`). Application handlers, batch processor, and infrastructure are not covered by this run.
- **APRC disclosure/rounding.** Survivors B14–B23 concern the solver's internal tolerances, not the legal-disclosure rounding that ADR-0004 explicitly excludes (gap G5).

## Command log (this task)

| # | Command | Exit | Note |
|---|---|---|---|
| 1 | `dotnet tool restore` (prior to run, to materialize the pinned 4.14.2) | 0 | (run in a prior turn; not re-run here) |
| 2 | `Push-Location tests/Promissio.Domain.Tests ; dotnet stryker --config-file ../../stryker-config.json 2>&1 \| Tee-Object -FilePath ../../StrykerOutput/Q03-run.log ; $exit=$LASTEXITCODE ; …` | **2** | Expected: break threshold 80 not met (score 72.70) |
| 3 | `git rev-parse HEAD` | 0 | `ef634bcefaafd1e9639f7336f13fa047b83ef2a7` (unchanged) |
| 4 | `git status --short --branch` | 0 | `main…origin/main`; only untracked: Q01 report + this report |

## Report status

- This file is newly created. No prior `2026-09-10-stryker-mutation-run.md` existed.
- `pwsh ./tools/check-documentation.ps1` is run after this report is written; result recorded in the final session report.
