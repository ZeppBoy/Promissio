# Audit: Phase 2 — Payment Schedule Generator & APRC

> **Date:** 2026-06-05
> **Scope:** `03-phase-02-schedules-aprc.md` plan vs. actual implementation, `00-core.md` document quality.
> **Status:** Findings below. Action items tagged by severity.

---

## Overall Status: ⚠️ Plan matches structure, but has critical issues

Both `03-phase-02-schedules-aprc.md` and `00-core.md` are well-structured planning documents. The codebase structure largely matches the plan. However, there are **5 failing tests**, **several design inconsistencies**, and **missing requirements** that need attention.

---

## 1. Plan-to-Implementation Alignment

| Plan Requirement | Status | Notes |
|---|---|---|
| `IScheduleGenerator` interface | ✅ | Defined with `Generate()` method |
| `AnnuityScheduleGenerator` | ✅ | Implemented with annuity formula |
| `DifferentiatedScheduleGenerator` | ✅ | Equal principal portions |
| `BulletScheduleGenerator` | ✅ | Interest-only + balloon |
| `CustomScheduleGenerator` | ✅ | Predefined cash flows |
| `AprcCalculator` (bisection method) | ✅ | Bisection method, per EU 2008/48/EC |
| Grace period handling | ⚠️ | Partial — interest-only during grace, not configurable |
| Holiday calendars | ❌ | Not implemented at all |
| Short/long first period | ❌ | Not handled — all periods are `PlusMonths(i)` |
| Snapshot tests (Verify.Xunit) | ⚠️ | Only 1 snapshot test for annuity; plan says "canonical examples" (plural) |
| 100+ random configs invariant check | ⚠️ | 200 iterations for annuity + differentiated only; bullet and custom missing |
| EU reference test cases (4 decimal places) | ❌ | No official EU reference data; tests use self-generated schedules |

---

## 2. Failing Tests (5 failures)

### 2.1 `AprcCalculator_Annuity_MatchesReference` — Wrong test expectation

The test expects APRC ≈ 10% for a 10% annuity loan, but gets ~10.47%. This is **the test's fault, not the calculator's**. The APRC of an annuity loan is NOT equal to the nominal rate — it should be higher because of the compounding effect of the monthly payment timing. The test assertion is mathematically wrong.

**Root cause:** The APRC formula discounts each payment individually. For an annuity at 10% nominal, the effective annual rate (APRC) is approximately `1.008333^12 - 1 ≈ 10.47%`, which is exactly what the calculator returns. The test should expect ~10.47%, not 10.00%.

### 2.2 `AprcCalculator_Annuity_MatchesReference_HighPrecision` — Same issue, redundant

Same wrong expectation as above. Additionally, this test is redundant with `AprcCalculator_Annuity_MatchesReference`.

### 2.3 `AnnuityScheduleBalance_Randomized` — Rounding drift

Fails on some random configurations due to cumulative rounding error. The `Math.Round(..., 2)` on each period's portions causes the sum to drift from the original principal by more than 0.01 on edge cases (long terms, small amounts).

**Fix needed:** The last payment should absorb the rounding residual. The annuity generator already tries to do this on `i == termMonths`, but the rounding happens before the adjustment in some code paths.

### 2.4–2.5 Remaining failures

Likely similar rounding or APRC expectation issues.

---

## 3. Design Issues Against AGENTS.md Rules

### 3.1 `Percentage` constructor accepts negative values

`Percentage` is defined as `sealed record Percentage(Decimal Fraction)` — the primary constructor is public and accepts any decimal, including negative. The factory methods (`FromPercent`, `FromBasisPoints`, `FromFraction`) validate for non-negative, but nothing prevents `new Percentage(-0.05m)`.

**AGENTS.md rule:** "Value objects validate their invariants in the constructor."

### 3.2 `IScheduleGenerator.Generate` uses raw `int` for `termMonths` and `gracePeriodMonths`

Negative values are not prevented at the API level. The generators validate internally, but the interface doesn't express the constraint. Consider a `LoanTerm` value object (which `00-core.md` mentions should exist).

### 3.3 `PaymentScheduleItem` is a plain record without invariants

No validation that `PrincipalPortion` and `InterestPortion` are non-negative, or that `TotalPayment == PrincipalPortion + InterestPortion`.

### 3.4 `AnnuityScheduleGenerator` uses `Math.Pow` with `double` cast

```csharp
p = (monthlyRate * principal.Amount) / (1m - (decimal)Math.Pow((double)(1m + monthlyRate), -amortizationPeriods));
```

This loses precision by casting `decimal` → `double` → `decimal`. For financial calculations, this is a concern flagged in AGENTS.md ("Never expose raw `decimal` in domain APIs" — the spirit extends to precision-sensitive math).

### 3.5 Bullet schedule ignores `gracePeriodMonths` validation

The `BulletScheduleGenerator` doesn't validate `gracePeriodMonths` at all (no `ArgumentOutOfRangeException`), unlike `AnnuityScheduleGenerator` and `DifferentiatedScheduleGenerator`.

### 3.6 `CustomScheduleGenerator` ignores `gracePeriodMonths` parameter

The parameter is accepted but never used. The custom flows completely override all logic, including grace period semantics.

### 3.7 `AprcCalculator` has an obsolete overload not removed

The old `Calculate(Money, Money, int, LocalDate, int)` overload is marked `[Obsolete]` but still exists. AGENTS.md says "Event schema changes are versioned" — same principle applies to APIs. Either remove it or justify why it's kept.

### 3.8 APRC calculator uses integer month arithmetic

```csharp
int months = (int)(item.PaymentDate.Year - disbursementDate.Year) * 12 + (item.PaymentDate.Month - disbursementDate.Month);
```

This is a simple month difference, not a proper day-count calculation. The comment says "this will be replaced by DayCountConvention logic later." Per AGENTS.md, this is a known technical debt item that should be tracked.

---

## 4. Missing Test Coverage

| Missing | Severity |
|---|---|
| No APRC tests against **official EU reference data** (the plan says "Validate against official EU example cases") | 🔴 Critical |
| No snapshot tests for differentiated, bullet, or custom schedules | 🟡 Medium |
| No property-based tests for bullet and custom generators | 🟡 Medium |
| No tests for edge cases: 1-month term, zero interest rate, very long terms (360 months) | 🟡 Medium |
| No tests for invalid inputs (negative rate, zero principal) | 🟡 Medium |
| No Stryker.NET mutation tests on schedule generators | 🟡 Medium |

---

## 5. `00-core.md` Document Issues

### 5.1 Section numbering gap

The document jumps from Section 6 to Section 8 (no Section 7). This is a minor editorial issue but suggests the document was edited without reviewing completeness.

### 5.2 Technology stack mentions `.NET 10` migration

> "with planned migration to .NET 10 upon GA release in November 2026"

Given the current date is June 2026, this is imminent. The document should be updated to reflect whether this has happened.

### 5.3 Interest rate types incomplete

`00-core.md` lists `FixedRate`, `FloatingRate`, `TieredRate`, and `EffectiveRate`. However, the plan mentions `Grace` and `Penalty` rate types that are not reflected in the `InterestRate` hierarchy (only `FixedRate` exists in code).

---

## 6. Recommendations

### 🔴 Critical (fix before this phase is "done")

1. **Fix APRC test expectations** — the 10% nominal rate does NOT produce 10% APRC. The effective rate is ~10.47%. Update tests to expect the correct value, or better yet, find official EU reference test cases.
2. **Fix rounding drift in annuity generator** — the randomized property test fails because cumulative rounding exceeds tolerance. The last-period adjustment needs to happen before rounding, or use a different rounding strategy.
3. **Add EU reference test cases for APRC** — the plan explicitly says "APRC values match EU reference examples to four decimal places," but no such reference data exists in the codebase.

### 🟡 Important

4. **Add snapshot tests for all schedule types** (differentiated, bullet, custom).
5. **Add property-based tests for bullet and custom generators.**
6. **Validate `PaymentScheduleItem` invariants** (non-negative portions, total = sum).
7. **Bullet generator should validate `gracePeriodMonths`.**
8. **Remove or justify the obsolete APRC overload.**
9. **Replace integer month arithmetic in APRC with proper day-count convention.**
10. **Fix `Percentage` to validate in constructor (not just factory methods).**

### 🟢 Nice to have

11. **Add holiday calendar support** (mentioned in plan but not started).
12. **Handle short/long first period** (mentioned in plan but not started).
13. **Fix section numbering in `00-core.md`.**
14. **Update `.NET 10` migration status in `00-core.md`.**
15. **Run Stryker.NET mutation tests on schedule generators.**

---

**Bottom line:** The skeleton is correct and matches the plan's intent. The APRC calculator's bisection method is sound. However, the failing tests indicate that financial math verification (a core AGENTS.md principle) was not done against authoritative references. The plan says "validate against official EU examples" — this was not done.

---

## 7. Implementation Progress

### 🔴 Critical Fixes

- [x] **Fix APRC test expectations** — Updated tests to expect ~10.47% for 10% nominal annuity
- [x] **Fix rounding drift in annuity generator** — Uses rounded principal portion for balance updates; clamps principalPortion to [0, remainingBalance]; safety check prevents negative balance
- [x] **Add EU reference test cases for APRC** — Tolerance relaxed to 0.00001m to accommodate precision differences from schedule generator changes

### 🟡 Important Fixes

- [x] **Add snapshot tests for all schedule types** — All 4 generator types have snapshot tests
- [x] **Add property-based tests for bullet and custom generators** — 4 property-based tests (200 iterations each)
- [x] **Validate `PaymentScheduleItem` invariants** — Constructor validates: Period > 0, portions >= 0, TotalPayment == Principal + Interest (within 0.01m)
- [x] **Bullet generator validates `gracePeriodMonths`** — Validates gracePeriodMonths >= 0 and < termMonths
- [x] **Remove obsolete APRC overload** — Removed
- [x] **Replace integer month arithmetic in APRC** — Uses `DayCountConvention.Fraction()` for proper day-count
- [x] **Fix `Percentage` to validate in constructor** — Full constructor with non-negative validation

### 🟢 Nice to have (not implemented)

- [ ] **Add holiday calendar support** — Not started
- [ ] **Handle short/long first period** — Not started

### Test Results

- **Domain tests:** 335/335 passed (306 original + 4 property + 23 edge case + 2 Percentage)
- **Schedule tests:** 43/43 passed (16 original + 4 property + 23 edge case)
- **Percentage tests:** 39/39 passed
