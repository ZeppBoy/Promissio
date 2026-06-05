# Phase 2 Audit (Post-Fix) — Payment Schedule Generator & APRC
**Date:** 2026-06-05
**Scope:** `03-phase-02-schedules-aprc.md` + `00-core.md`
**Supersedes:** `phase-2-audit-2026-06-05.md` (verifies which findings were addressed)
**Audited by:** Claude Code

---

## 0. Summary Since Last Audit

The code was modified after the prior audit. **All four bugs from the prior audit are now fixed**, the `IAprcCalculator` interface was added, and a test file (`ScheduleGenerationTests.cs`, 8 tests) now exists. Domain tests: **298 passing** (up from 290).

However, the acceptance criteria are still **not fully met**: the tests are example-based (not property-based over 100 configs), there are no Verify.Xunit snapshot tests, APRC is not validated against EU reference examples to four decimal places, and `DayCountConvention` is still absent. Two **new edge-case crashes** were introduced by the grace-period fixes.

---

## 1. Prior Findings — Verification

| Prior finding | Status now | Evidence |
|---------------|-----------|----------|
| Bug 1 — annuity grace breaks payment amount | ✅ **Fixed** | `AnnuityScheduleGenerator.cs:32-33` now computes `amortizationPeriods = termMonths - gracePeriodMonths` and sizes `p` over that window |
| Bug 2 — differentiated grace breaks equal portions | ✅ **Fixed** | `DifferentiatedScheduleGenerator.cs:25-26` now divides principal by `amortizationPeriods` |
| Bug 3 — APRC simple annualization | ✅ **Fixed** | `AprcCalculator.cs:75` and `:136` now use `Math.Pow(1.0 + mid, 12.0) - 1.0` (compound) |
| Bug 4 — APRC assumes equal payments | ⚠️ **Partially fixed** | A correct overload `Calculate(Money, IEnumerable<PaymentScheduleItem>, int)` was **added** (`:86-138`), discounting each actual `TotalPayment` by its period. But the **flawed `(Money totalCost, int termMonths)` overload still exists** (`:23-77`) and remains public and callable |
| Missing `IAprcCalculator` interface | ✅ **Added** | `IAprcCalculator.cs` exists; `AprcCalculator : IAprcCalculator` |
| No schedule tests | ⚠️ **Partially addressed** | `ScheduleGenerationTests.cs` adds 8 example tests; see §3 for what is still missing |

---

## 2. New Issues Introduced by the Fixes

### New Bug A — Zero-interest annuity throws `DivideByZeroException`

**File:** `AnnuityScheduleGenerator.cs:33`

```csharp
decimal p = (monthlyRate * principal.Amount) / (1m - (decimal)Math.Pow((double)(1m + monthlyRate), -amortizationPeriods));
```

When `monthlyRate == 0` (a 0% loan), the denominator is `1m - 1m = 0m` and the numerator is `0m`, so this is `0m / 0m` → **`DivideByZeroException`** (decimal division by zero always throws).

This matters: `00-core.md` lists **"Grace — special rate (often zero)"** as a supported rate type. A 0% promotional loan is a legitimate product and currently crashes the annuity generator. Pre-existing in spirit, but now on the critical path.

**Fix:** guard `monthlyRate == 0` → straight-line principal `p = principal / amortizationPeriods`, interest 0.

### New Bug B — `gracePeriodMonths >= termMonths` throws

**Files:** `AnnuityScheduleGenerator.cs:33`, `DifferentiatedScheduleGenerator.cs:26`

When `gracePeriodMonths == termMonths`, `amortizationPeriods == 0`:
- Differentiated: `principal.Amount / 0` → **`DivideByZeroException`**.
- Annuity: `Math.Pow(x, -0) = 1` → denominator `0m` → **`DivideByZeroException`** (for non-zero rate).

When `gracePeriodMonths > termMonths`, `amortizationPeriods` is negative and produces nonsensical (negative) principal portions silently.

**Fix:** validate `0 <= gracePeriodMonths < termMonths` at entry and throw `ArgumentOutOfRangeException` with a clear message, or clamp per a documented business rule.

---

## 3. Acceptance Criteria — Still Not Met

### From `03-phase-02-schedules-aprc.md`

| Criterion | Status | Note |
|-----------|--------|------|
| Invariant checks pass for **≥ 100 random** loan configurations | ❌ **Not met** | Tests are 8 fixed examples; no property-based / randomized generation (CsCheck/FsCheck per `00-core.md` stack) |
| **Verify.Xunit snapshot** tests for canonical examples | ❌ **Not met** | No `.verified.txt` snapshots; no `Verifier.Verify` calls |
| APRC matches **EU reference examples to four decimal places** | ❌ **Not met** | `AprcCalculator_Annuity_MatchesReference` asserts `BeApproximately(0.10m, 0.01m)` — tolerance is 1 percentage point, not 4 dp; and it checks against the loan's own nominal rate, not a published EU example |
| All generators produce balanced schedules (`Σ principal == original`) | ⚠️ **Tested with loose tolerance** | Balance tests exist but use `BeApproximately(..., 0.01m)`; acceptable for rounding, but not an exact-conservation invariant |

### From `00-core.md` — Critical services

| Required | Status |
|----------|--------|
| `DayCountConvention` parameter on generators | ❌ **Still missing** — generators hardcode `annualRate / 12m`; no use of the existing `Calculations/DayCounts/*` or `IInterestCalculator` (confirmed: no references) |
| `IAprcCalculator` interface | ✅ Added |

### From `03-phase-02-schedules-aprc.md` — Week 5, Task 4

| Edge case | Status |
|-----------|--------|
| Short first period | ❌ Not implemented (`startDate.PlusMonths(i)` only) |
| Long first period | ❌ Not implemented |
| Holiday calendar adjustment | ❌ Not implemented |

---

## 4. APRC Schedule Overload — Correctness Note

The new `Calculate(Money, IEnumerable<PaymentScheduleItem>, int)` overload (`:86-138`) is a genuine improvement: it discounts each actual `TotalPayment` by `(1 + mid)^Period`, so bullet and differentiated schedules are now handled correctly in principle. Two caveats:

1. **Period index, not actual day-count.** It discounts by integer `item.Period`, not by the actual year-fraction between `startDate` and `item.PaymentDate`. The code comment admits this. The EU directive uses actual time in years; with equal monthly periods the result is close, but short/long first periods will be slightly off. Four-decimal-place agreement with EU examples is unlikely until this uses real dated cash flows.
2. **`schedule` is enumerated 100× (once per bisection iteration).** If a lazy `IEnumerable` is passed (the generators `return` a materialized `List`, so currently safe), this would re-execute generation each iteration. Recommend materializing with `.ToList()` once at method entry.

---

## 5. What Is Correct

- All four prior bugs fixed; grace-period math is now correct for `0 < grace < term`.
- `IScheduleGenerator` / `IAprcCalculator` signatures clean — NodaTime, `Money`, `Percentage`; no banned types.
- Bullet and Custom generators unchanged and correct.
- Compound annualization now matches the EU directive's annualization step.
- 298 domain tests pass; `dotnet test` green.

---

## 6. Remaining Work — Priority Order

| Priority | Item | Rationale |
|----------|------|-----------|
| 1 | Guard zero-rate (New Bug A) and `grace >= term` (New Bug B) | Crashes on legitimate inputs |
| 2 | Remove or `[Obsolete]` the flawed `(Money totalCost, …)` APRC overload | Leaves a wrong path public; the interface only exposes the correct one |
| 3 | Validate APRC against ≥1 published EU 2008/48/EC example to 4 dp | Acceptance criterion; current test only checks against nominal rate at 1pp tolerance |
| 4 | Add property-based tests (≥100 random configs) for the balance invariant | Acceptance criterion; CsCheck/FsCheck per stack |
| 5 | Add Verify.Xunit snapshot tests for canonical schedules | Acceptance criterion |
| 6 | APRC: discount by actual year-fraction (`startDate`→`PaymentDate`) and `.ToList()` the schedule once | Precision + avoids re-enumeration |
| 7 | Add `DayCountConvention` parameter to generators | Required before Phase 3 wires real loan terms |
| 8 | Short/long first period + holiday calendar | Week 5 Task 4; may defer to Phase 3 if documented |

---

## 7. Open Questions (carried from prior audit)

1. **Grace semantics** — post-grace recompute keeps the stated term (current behavior). Confirm this is the intended product vs. term-extension. *(Now resolved in code to "keep stated term"; needs sign-off.)*
2. **APRC fee scope** — does the EU APRC here include origination fees, or interest only? The schedule-based overload currently sees only schedule cash flows; fees would need to be injected as an additional outflow at t=0.
3. **Day-count for APRC** — confirm APRC should discount by actual `PaymentDate` year-fractions rather than integer period index.
4. **Holiday calendar source** — TARGET2 vs. local; needed before business-day adjustment.
