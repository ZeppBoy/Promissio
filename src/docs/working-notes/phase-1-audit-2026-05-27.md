# Phase 1 Audit — Domain Core: Interest Engine

**Date:** 2026-05-27  
**Branch:** main  
**Last commit:** 032908a — Phase 1 implementation finished  
**Test result:** 228 domain tests pass (`dotnet test tests/Promissio.Domain.Tests`)

---

## Verdict

Phase 1 is **not complete**. Three blocking issues prevent the acceptance criteria from being met. Several significant gaps must be addressed before moving to Phase 2.

---

## ✅ What is implemented and correct

| Item | Notes |
|------|-------|
| `Money` — immutable, currency-guarded arithmetic, value equality, `GetHashCode`, JSON converter | `src/Promissio.Domain/ValueObjects/Money.cs` |
| `Percentage` — basis points / fraction / percent conversions, arithmetic operators | `src/Promissio.Domain/ValueObjects/Percentage.cs` |
| `LoanTerm` — NodaTime-based, `EndDate()`, month and year constructors | `src/Promissio.Domain/ValueObjects/LoanTerm.cs` |
| `InterestRate` abstract + `FixedRate`, `FloatingRate`, `TieredRate`, `EffectiveRate` | `src/Promissio.Domain/ValueObjects/InterestRate.cs` |
| `DayCountConvention` abstract class + `Actual360`, `Actual365`, `ActualActual`, `Thirty360`, `Thirty360European` | `src/Promissio.Domain/Calculations/DayCounts/` |
| `IInterestCalculator` interface + `InterestCalculator` implementation | `src/Promissio.Domain/Calculations/` |
| Property-based tests via FsCheck for `Money`, `Percentage`, `LoanTerm`, `InterestRate` | All algebraic invariants covered |
| `InterestCalculatorTests` — 30+ scenarios covering all rate types, edge cases, multi-period, rounding | `tests/Promissio.Domain.Tests/Calculations/InterestCalculatorTests.cs` |
| BenchmarkDotNet file with hot-path benchmarks (structure) | `benchmarks/Promissio.Benchmarks/Program.cs` |

---

## 🔴 Blocking issues

### 1. All day-count `[Theory]` tests are `private` — they never run

**File:** `tests/Promissio.Domain.Tests/Calculations/DayCountConventionTests.cs`

Every `[Theory]` method across all five convention test classes is declared `private`. xUnit only discovers `public` test methods. **All 114 parametrized day-count test vectors are silently skipped.** The plan requires ≥ 20 verified reference cases per convention.

**Fix:** Change all five method signatures from `private void` to `public void`.

---

### 2. `Thirty360.AdjustDay` end-date condition is inverted

**File:** `src/Promissio.Domain/Calculations/DayCounts/Thirty360.cs`, line 49

Current code:
```csharp
if (day == 31 && otherDate.Day < 30) day = 30;
```

**ISDA 2006 §4.16 rule:** set D2 = 30 if D2 = 31 **and D1 ≥ 30**. The condition is inverted.

**Example of incorrect output:** Jan 31 → Mar 31 yields 61 days; correct result is 60 days.

This bug is invisible today because no test case has D2 = 31 (masked by issue #1 — Theory tests don't run).

**Fix:**
```csharp
if (day == 31 && otherDate.Day >= 30) day = 30;
```

---

### 3. Benchmark project does not compile

**File:** `benchmarks/Promissio.Benchmarks/Program.cs`

Three separate problems:

**a) Invalid null-forgiving syntax on field declarations:**
```csharp
// Invalid — not legal C#
private IInterestCalculator _calculator!;

// Correct
private IInterestCalculator _calculator = null!;
```

**b) Extra `_convention` parameter in `Calculate` call:**
```csharp
// Won't compile — interface takes (Money, InterestRate, LocalDate, LocalDate)
return _calculator.Calculate(_principal, _rate, _convention, _startDate, _endDate);

// Correct — convention is encapsulated inside the rate
return _calculator.Calculate(_principal, _rate, _startDate, _endDate);
```

**c) Wrong project reference path in `.csproj`:**
```xml
<!-- Resolves to a non-existent path from benchmarks/Promissio.Benchmarks/ -->
<ProjectReference Include="../src/Promissio.Domain/Promissio.Domain.csproj" />

<!-- Correct relative path -->
<ProjectReference Include="../../src/Promissio.Domain/Promissio.Domain.csproj" />
```

---

## 🟡 Significant gaps

### 4. `docs/domain/day-count-conventions.md` does not exist

The plan (Week 3, task 4) requires this document. The `DayCountConvention.cs` XML doc comment already references `/docs/domain/day-count-conventions.md` but the file is absent. Acceptance criterion: "Documentation is reviewable by a non-developer banking analyst."

---

### 5. No benchmark results checked in

Week 4 acceptance criterion: benchmark results checked into `benchmarks/results/` and tracked across commits. The directory does not exist. Results must be committed after issues #1–#3 are fixed and benchmarks are executed.

---

### 6. `ActualActual` cross-year test vectors have wrong expected values

**File:** `tests/Promissio.Domain.Tests/Calculations/DayCountConventionTests.cs`, `ActualActualTests`

Several `[InlineData]` entries span two calendar years (e.g., `2023-09-01 → 2024-03-01`). The test computes the expected fraction as `days / daysInStartYear`, but `ActualActual` correctly weights each year segment separately. Those expected values are wrong for cross-year inputs.

Currently dormant because the Theory methods are private (issue #1). Will produce failures once the visibility is fixed.

---

### 7. `FloatingRate.resetSchedule` is unmodeled

`00-core.md` specifies `FloatingRate(referenceRate, margin, resetSchedule)`. The current constructor is `FloatingRate(baseRate, margin, convention)` — no reset schedule, no re-pricing logic. The current implementation is structurally identical to `FixedRate` with an added margin. This is acceptable as a stub but should be explicitly marked as `// TODO: Phase N — add reset schedule` rather than implied complete.

---

### 8. No Stryker.NET configuration

The plan requires ≥ 80% mutation score on calculator code. No `stryker-config.json` exists in the repository. Mutation testing should be set up now so it can be run and reported, not left as a retroactive step.

---

### 9. `Percentage` validation caps at 100% / 10 000 bps

`FromPercent` throws if `percent > 100`; `FromBasisPoints` throws if `basisPoints > 10000`. Penalty rates in delinquency scenarios and intermediate calculation values can legitimately exceed these bounds. Either document the deliberate constraint with a business justification or remove the upper-bound guard.

---

## Acceptance criteria checklist

| Criterion | Status |
|---|---|
| Value objects immutable, value-equal, correct `GetHashCode` | ✅ |
| Property-based tests cover algebraic invariants | ✅ |
| All day-count conventions match reference values (≥ 20 test cases each) | 🔴 Test vectors exist but never run (private methods) |
| `docs/domain/day-count-conventions.md` exists and is analyst-readable | 🔴 File missing |
| 30+ `InterestCalculator` scenario tests with known correct outputs | ✅ |
| Benchmark results in `benchmarks/results/` | 🔴 Directory missing; benchmark build broken |
| Mutation testing ≥ 80% (Stryker.NET) | 🔴 Not set up |
| 90%+ line coverage on value object code | Not measured (tooling not configured) |

---

## Recommended fix order

1. Fix Theory method visibility in `DayCountConventionTests.cs` (5 min) — unblocks all subsequent verification.
2. Fix `Thirty360` inversion bug (2 min) — then re-run tests to confirm fix.
3. Fix `ActualActual` cross-year expected values (15 min) — requires working out correct weighted fractions.
4. Fix benchmark project: field declarations, `Calculate` call signature, project reference path (10 min).
5. Run benchmarks, commit results to `benchmarks/results/`.
6. Write `docs/domain/day-count-conventions.md`.
7. Add `stryker-config.json` and run mutation testing; iterate until ≥ 80%.

---

*Audit performed by Claude Code. Financial math findings should be independently verified against ISDA 2006 §4.16 before closing.*
