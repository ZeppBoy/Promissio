# Phase 1 Audit — 2026-05-28

**Date:** 2026-05-28  
**Branch:** main (working tree, uncommitted changes)  
**Baseline commit:** b16c7a1 — Audit for Phase 1  
**Test result:** 1 fail, 257 pass, 258 total (`dotnet test tests/Promissio.Domain.Tests`)  
**Benchmark build:** ✅ 0 errors, 0 warnings  
**Supersedes:** `phase-1-audit-final-2026-05-27.md`

---

## Overall Verdict

**Phase 1 is not complete. Three blockers remain.**

Since the final audit on 2026-05-27, one new failing test has been introduced that exposes a real domain correctness bug in `InterestRate` equality. Benchmark results and mutation testing remain unexecuted. Everything else from previous audits is resolved.

---

## What Is Done

| Area | Status | Notes |
|---|---|---|
| `Money` — immutable, currency-guarded arithmetic, value equality, `GetHashCode`, JSON converter | ✅ | |
| `Percentage` — bps/fraction/percent conversions, arithmetic operators, >100% allowed | ✅ | Both `FromPercent` and `FromBasisPoints` >100% tests present |
| `LoanTerm` — NodaTime-based, `EndDate()`, month and year constructors | ✅ | |
| `InterestRate` abstract + `FixedRate`, `FloatingRate`, `TieredRate`, `EffectiveRate` | 🔴 | Equality bug — see Blocker A |
| `FloatingRate` reset schedule placeholder | ✅ | `TODO: Phase N` comment in XML doc |
| Property-based tests (FsCheck) for `Money`, `Percentage`, `LoanTerm`, `InterestRate` | ✅ | Algebraic invariants covered |
| All 5 day-count conventions implemented | ✅ | `Actual360`, `Actual365`, `ActualActual`, `Thirty360`, `Thirty360European` |
| Day-count `[Theory]` tests public and running | ✅ | All 5 test classes, 23 vectors each |
| `Thirty360.AdjustDay` inversion bug | ✅ | Fixed: `>= 30` condition correct |
| `Thirty360` D2=31, D1≥30 boundary test vector | ✅ | `[InlineData(2023, 1, 31, 2023, 3, 31, 60)]` present |
| `Thirty360European` D2 rule — code and XML doc | ✅ | Unconditional adjustment correctly documented |
| `docs/domain/day-count-conventions.md` | ✅ | Accurate, analyst-readable, correct 30E/360 description |
| `InterestCalculator` scenario tests | ✅ | 30+ scenarios with known outputs |
| Benchmark project compiles | ✅ | 0 errors, 0 warnings |
| `stryker-config.json` outer wrapper | ✅ | `"stryker-config"` wrapper present |
| `dotnet-stryker` installed | ✅ | v4.14.2 in `dotnet-tools.json` |

---

## Blockers — Phase 2 Entry Gates

### 🔴 Blocker A — `InterestRate` equality does not include `DayCountConvention`

**File:** [src/Promissio.Domain/ValueObjects/InterestRate.cs](src/Promissio.Domain/ValueObjects/InterestRate.cs), line 15  
**Failing test:** `InterestRateTests.FixedRate_Equality_DifferentDayCount_NotEqual`

The base-class `Equals` method:
```csharp
public bool Equals(InterestRate? other)
    => other != null && this.GetType() == other.GetType() && this.Rate == other.Rate;
```
compares only runtime type and the rate percentage. It ignores `DayCountConvention`. Two `FixedRate` instances with the same percentage but different day-count conventions will produce different monetary interest amounts — they are not equal. The same flaw applies to `GetHashCode`, `FloatingRate`, `TieredRate`, and `EffectiveRate`.

**Fix:** Override `Equals` and `GetHashCode` in each concrete rate class to include the `DayCountConvention`. For `TieredRate`, include the `Tiers` collection. Example for `FixedRate`:
```csharp
public override bool Equals(object? obj) => obj is FixedRate other
    && Rate == other.Rate
    && DayCountConvention.Name == other.DayCountConvention.Name;

public override int GetHashCode() => HashCode.Combine(Rate, DayCountConvention.Name);
```

Alternatively, add `DayCountConvention` to the abstract base equality by making it abstract or by adding a virtual `EqualityComponents` method. Whatever the approach, `InterestRate.GetHashCode` must also be updated.

**Effort:** ~15 minutes.

---

### 🔴 Blocker B — Benchmark results not committed

`benchmarks/results/` does not exist. The Week 4 acceptance criterion is explicit: *"benchmark results checked into `benchmarks/results/` and tracked across commits."*

The benchmark project builds and the runner is complete. This is purely an execution and commit step.

**Steps:**
```
dotnet run --project benchmarks/Promissio.Benchmarks -c Release -- --filter "*"
```
After completion, BenchmarkDotNet writes reports to `BenchmarkDotNet.Artifacts/results/`. Move those files to `benchmarks/results/` and commit.

**Effort:** ~20 minutes.

---

### 🔴 Blocker C — Mutation testing has not been run; score unknown

Stryker.NET 4.14.2 is installed as a local tool (`dotnet-tools.json`). `stryker-config.json` has the required `"stryker-config"` outer wrapper and uses the correct v4 nested-threshold format:

```json
{
  "stryker-config": {
    "thresholds": { "high": 90, "low": 80, "break": 0 }
  }
}
```

This configuration is valid for Stryker.NET 4.x. **The tool has never been executed.** No mutation score exists.

**Steps:**
```
dotnet stryker
```
Run from the repo root. Iterate until ≥ 80% mutation score on `Promissio.Domain`.

**Effort:** ~30 minutes (first run + iteration if score is below threshold).

---

## Non-Blocking Gaps

### 🟡 Gap 1 — `ActualActual` cross-year test vectors are self-referential

**File:** [tests/Promissio.Domain.Tests/Calculations/DayCountConventionTests.cs](tests/Promissio.Domain.Tests/Calculations/DayCountConventionTests.cs), lines 178–203

The `Fraction_WithinSameYear_ReturnsCorrectValue` test uses a dynamic computation block for cross-year date pairs that mirrors the implementation's own weighted-segment algorithm. If `ActualActual` has a systematic error, these tests would still pass.

`AGENTS.md §8` requires: *"Each convention has at least 20 test vectors with values sourced from ISDA documentation or ECB illustrative examples."*

The documentation already provides the derivation for `Sep 1, 2023 → Mar 1, 2024: 121/365 + 61/366 ≈ 0.49817...`. That should be a pinned `decimal` literal in the test, not computed at test time.

**Fix:** Replace the dynamic computation block for cross-year cases with 3–5 tests using manually verified, hard-coded expected values derived from the ECB example in `docs/domain/day-count-conventions.md`. Example:
```csharp
// Sep 1, 2023 → Mar 1, 2024: 121/365 + 61/366, ECB derivation in day-count-conventions.md
[InlineData(2023, 9, 1, 2024, 3, 1, "0.498...")]
```

**Effort:** ~15 minutes. **Not a Phase 2 entry blocker**, but must be resolved before the PR is closed.

### 🟡 Gap 2 — Line coverage not measured

The Week 2 acceptance criterion requires 90%+ line coverage on value object code. No Coverlet configuration exists. This has never been measured.

**Recommendation:** Add Coverlet to the test project and run `dotnet test --collect:"XPlat Code Coverage"` as part of CI. Defer to Phase 2 CI pipeline setup per the final audit's guidance.

---

## Stryker Config — Format Confirmed Correct for v4.x

The previous audit (2026-05-27) incorrectly suggested changing to flat `threshold-high`/`threshold-low`/`threshold-break` keys. That format applied to Stryker.NET v1. The installed version is **4.14.2**, which uses the nested `thresholds` object. The current `stryker-config.json` is valid. No changes needed to the config.

---

## Acceptance Criteria Checklist

| Criterion (from `02-phase-01-interest-engine.md`) | Status |
|---|---|
| Value objects immutable, value-equal, correct `GetHashCode` | 🔴 `InterestRate` equality excludes `DayCountConvention` |
| Property-based tests cover algebraic invariants | ✅ |
| All conventions match reference values, ≥ 20 test cases each | 🟡 `ActualActual` cross-year vectors are self-referential |
| `docs/domain/day-count-conventions.md` exists and is analyst-readable | ✅ |
| 30+ `InterestCalculator` scenario tests with known correct outputs | ✅ |
| Benchmark results in `benchmarks/results/`, tracked across commits | 🔴 Directory missing; benchmarks never executed |
| Mutation testing ≥ 80% via Stryker.NET | 🔴 Not run; score unknown |
| 90%+ line coverage on value object code | Not measured |

---

## Recommended Fix Order

| # | Task | Effort | Blocks Phase 2? |
|---|---|---|---|
| 1 | Fix `InterestRate` equality to include `DayCountConvention` in all concrete types | 15 min | Yes |
| 2 | Run `dotnet stryker`, iterate until ≥ 80% mutation score | 30 min | Yes |
| 3 | Run benchmarks in Release mode, commit results to `benchmarks/results/` | 20 min | Yes |
| 4 | Pin 3–5 `ActualActual` cross-year vectors from ECB derivation in doc | 15 min | No (before PR close) |

**Total estimated effort to Phase 2 entry: ~65 minutes.**

---

*Audit performed by Claude Code. InterestRate equality fix and ActualActual cross-year expected values require human verification against ISDA 2006 Annex A before the PR is closed.*
