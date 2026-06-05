# Phase 2 Audit — Payment Schedule Generator & APRC
**Date:** 2026-06-05  
**Scope:** `03-phase-02-schedules-aprc.md` + `00-core.md`  
**Audited by:** Claude Code

---

## 1. Files Audited

| File | Exists |
|------|--------|
| `IScheduleGenerator.cs` | ✅ |
| `AnnuityScheduleGenerator.cs` | ✅ |
| `DifferentiatedScheduleGenerator.cs` | ✅ |
| `BulletScheduleGenerator.cs` | ✅ |
| `CustomScheduleGenerator.cs` | ✅ |
| `AprcCalculator.cs` | ✅ |
| `IAprcCalculator.cs` | ❌ Missing |
| `/tests/.../ScheduleGeneration/` (any test files) | ❌ Empty directory |

All 290 existing domain tests pass. None cover schedule generation.

---

## 2. Bugs in the Implementation

### Bug 1 — Grace period breaks annuity payment amount

**File:** `AnnuityScheduleGenerator.cs:32`

The annuity payment `p` is computed using `termMonths` (the full loan term):

```csharp
decimal p = (monthlyRate * principal.Amount) / (1m - (decimal)Math.Pow((double)(1m + monthlyRate), -termMonths));
```

But principal is only amortized during `termMonths - gracePeriodMonths` periods. The payment `p` is therefore sized to amortize over the full term, making it too small for the actual amortization window. The last payment becomes a balloon correction instead of a normal annuity payment — the "constant periodic payment" invariant is violated whenever `gracePeriodMonths > 0`.

**Fix:** After grace months complete, recompute `p` using `remainingPrincipal` and the count of remaining amortization periods:
```csharp
int amortizationPeriods = termMonths - gracePeriodMonths;
decimal p = (monthlyRate * principal.Amount) / (1m - (decimal)Math.Pow((double)(1m + monthlyRate), -amortizationPeriods));
```

---

### Bug 2 — Grace period breaks differentiated equal-portions invariant

**File:** `DifferentiatedScheduleGenerator.cs:25`

```csharp
decimal principalPortion = principal.Amount / termMonths;
```

During grace months, `currentPrincipalPortion = 0` is applied. For a 12-month loan with 3 grace months, the 9 amortization periods each pay `principal / 12`, and the final period pays the remaining `principal × 4/12` — four times what the others do. This violates the "equal principal portions" definition of a differentiated schedule.

**Fix:**
```csharp
int amortizationPeriods = termMonths - gracePeriodMonths;
decimal principalPortion = principal.Amount / amortizationPeriods;
```

---

### Bug 3 — APRC annualization uses simple multiplication instead of compounding

**File:** `AprcCalculator.cs:75`

```csharp
decimal annualRate = (decimal)(mid * 12.0);  // WRONG
```

The bisection solver finds a monthly rate `mid`. The current code converts it to annual by multiplying by 12 (simple interest annualization). The EU Consumer Credit Directive 2008/48/EC requires compound annualization:

```
annual_rate = (1 + monthly_rate)^12 - 1
```

**Numerical impact:** At a monthly rate of 0.8333% (≈ 10% stated annual), simple gives **10.00%** APRC; compound gives **10.47%**. The acceptance criterion "match EU reference examples to four decimal places" is unreachable with this formula.

**Fix:**
```csharp
decimal annualRate = (decimal)(Math.Pow(1.0 + mid, 12.0) - 1.0);
```

---

### Bug 4 — APRC assumes equal payments; wrong for bullet and differentiated loans

**File:** `AprcCalculator.cs:39–40`

```csharp
decimal totalPayment = principal.Amount + totalCost.Amount;
decimal periodicPayment = totalPayment / termMonths;  // assumes annuity
```

The current signature accepts only `totalCost` and divides it equally across `termMonths`. This is only valid for a perfect annuity with no fees.

- For a **bullet loan**, all principal is returned in the last period — `periodicPayment` is a fiction.
- For a **differentiated loan**, each payment decreases — averaging them produces a different rate.
- The EU directive formula requires summing the present value of each **actual** dated cash flow.

The EU formula:
```
Σ C_k / (1 + i)^(t_k) = Σ D_m / (1 + i)^(s_m)
```
where `t_k` and `s_m` are time in **years** from the disbursement date.

**Fix:** Change the method signature to accept `IEnumerable<PaymentScheduleItem>` (the actual schedule output), and solve for `i` over real dated cash flows. The current `(Money totalCost, int termMonths)` parameters should be removed.

---

## 3. Missing Items — Per Specification

### From `00-core.md` — Critical services

| Required item | Status |
|---------------|--------|
| `IAprcCalculator` interface | **Missing** — `AprcCalculator` is a concrete class with no interface |
| `DayCountConvention` parameter on schedule generators | **Missing** — all generators hardcode `annualRate / 12`, ignoring Actual/360, Actual/365, 30/360, etc. |

### From `03-phase-02-schedules-aprc.md` — Week 5, Task 4

| Edge case | Status |
|-----------|--------|
| Short first period | **Not implemented** |
| Long first period | **Not implemented** |
| Holiday calendar adjustment | **Not implemented** |

Payment dates are computed as `startDate.PlusMonths(i)` with no adjustment for non-standard first periods or business day calendars.

### From `03-phase-02-schedules-aprc.md` — Acceptance criteria

| Criterion | Status |
|-----------|--------|
| Property-based invariant tests for ≥ 100 random loan configurations | **Missing** — test directory is empty |
| Verify.Xunit snapshot tests for canonical examples | **Missing** |
| APRC values match EU reference examples to four decimal places | **Impossible** with current formula (see Bugs 3 and 4) |
| All schedule generators produce balanced schedules (`Σ principal portions == original principal`) | **Not tested** |

---

## 4. What Is Correct

| Item | Assessment |
|------|------------|
| `IScheduleGenerator` signature | Clean — uses `NodaTime.LocalDate`, `Money`, `Percentage`; no banned types |
| Annuity formula (no-grace case) | Mathematically correct |
| Differentiated generator (no-grace case) | Correct equal-portion behavior |
| `BulletScheduleGenerator` | Correctly holds principal at full balance; balloon fires at `i == termMonths` |
| `CustomScheduleGenerator` | Correctly delegates to caller-supplied cash flows |
| Payment date computation | `startDate.PlusMonths(i)` — NodaTime, correct |
| Bisection method structure | Structurally valid; direction of search is correct |
| Grace period interest-only logic | Correct: `principalPortion = 0`, interest still accrues on full balance |
| Per-period rounding (`Math.Round(..., 2)`) | Acceptable; last-period correction handles drift |

---

## 5. Priority Order for Fixes

| Priority | Item | Rationale |
|----------|------|-----------|
| 1 | Fix APRC annualization (Bug 3) | Acceptance criterion is unreachable without this |
| 2 | Fix APRC signature to accept actual schedule (Bug 4) | Acceptance criterion is unreachable without this; also add `IAprcCalculator` |
| 3 | Fix grace period in annuity generator (Bug 1) | Invariant violation; wrong payments for real loan products |
| 4 | Fix grace period in differentiated generator (Bug 2) | Invariant violation; equal-portions contract broken |
| 5 | Write schedule generation tests | Acceptance criteria require them; current confidence is zero |
| 6 | Add `DayCountConvention` parameter | Required before Phase 3 uses schedules with real loan terms |
| 7 | Short/long first period and holiday calendar | Lower urgency; can be deferred to Phase 3 if documented |

---

## 6. Open Questions for Human Review

1. **Grace period semantics**: should the post-grace payments recompute to the original payment size (keeping the stated term), or should the term extend to maintain the original `p`? Both are valid products. The current code does neither correctly.

2. **APRC scope**: the EU formula for APRC includes fees, not just interest. Does `totalCost` in the current signature include origination fees? If so, the interface needs documenting. If not, the parameter name is misleading.

3. **Day-count for APRC**: the EU directive uses actual days between cash flow dates expressed as fractions of a year. This conflicts with the current monthly-period model. Confirm whether the APRC calculator should operate on `PaymentScheduleItem.PaymentDate` values directly.

4. **Holiday calendar source**: no calendar data source is referenced. Confirm what calendar to use (TARGET2, local central bank, etc.) before implementing business day adjustment.
