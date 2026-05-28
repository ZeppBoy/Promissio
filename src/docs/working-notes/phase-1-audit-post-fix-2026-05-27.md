# Phase 1 Audit — Post-Fix Review
**Date:** 2026-05-27  
**Branch:** main (working tree, uncommitted changes)  
**Baseline commit:** b16c7a1 — Audit for Phase 1  
**Test result:** 229 domain tests pass, 0 fail (`dotnet test tests/Promissio.Domain.Tests`)  
**Benchmark build:** ✅ 0 errors, 0 warnings  

This audit re-examines the three blocking issues and six significant gaps from the original `phase-1-audit-2026-05-27.md` against the proposed working-tree changes.

---

## Verdict

Phase 1 is **closer but not complete.** All three original blockers are resolved. Two new blockers remain: benchmark results are still not committed, and the Stryker.NET configuration is almost certainly malformed and has never been run.

---

## Original blockers — resolution status

### ✅ Blocker 1 — Theory test visibility fixed

The five `[Theory]` methods in `DayCountConventionTests.cs` are now `public void`. All 115 parametrized test vectors are running. The DayCount test count is **126** (5 × 23 InlineData + 11 Facts). Total suite: **228 → 229** (net +1 because Percentage tests also changed; see below).

Verified: committed HEAD runs 228; working tree with all changes runs 229.

---

### ✅ Blocker 2 — `Thirty360.AdjustDay` inversion fixed

`src/Promissio.Domain/Calculations/DayCounts/Thirty360.cs` line 49:

```csharp
// Before (wrong):
if (day == 31 && otherDate.Day < 30) day = 30;

// After (correct):
if (day == 31 && otherDate.Day >= 30) day = 30;
```

This now correctly implements ISDA 2006 §4.16: set D2 = 30 if D2 = 31 **and** D1 ≥ 30. All 23 Thirty360 theory vectors pass.

**Residual gap:** There is no test vector for the exact case that contained the bug — D2 = 31 with D1 ≥ 30 (e.g., Jan 31 → Mar 31, expected 60 days). The fix is correct but the test suite does not explicitly verify the boundary that was previously broken. This should be added.

---

### ✅ Blocker 3 — Benchmark project compiles

All three sub-issues are resolved:

| Sub-issue | Before | After |
|---|---|---|
| Field declaration syntax | `private IInterestCalculator _calculator!;` (invalid) | `_calculator = null!;` (valid) |
| `Calculate()` signature | passed extra `_convention` argument | correct 4-arg call |
| Project reference path | `../src/...` (wrong nesting) | `../../src/...` (correct) |

`dotnet build benchmarks/Promissio.Benchmarks/` completes with 0 errors, 0 warnings.

---

## Remaining blocking issues

### 🔴 Benchmark results still not committed

`benchmarks/results/` does not exist. Week 4 acceptance criterion: "benchmark results checked into `benchmarks/results/` and tracked across commits."

The benchmark project now builds and the runner is structurally complete. The results need to be generated and committed. Run benchmarks in Release mode:

```
dotnet run --project benchmarks/Promissio.Benchmarks -c Release -- --filter "*"
```

Then commit the generated Markdown/HTML reports from `BenchmarkDotNet.Artifacts/results/` into `benchmarks/results/`.

---

### 🔴 Stryker.NET config is malformed — mutation score unknown

`stryker-config.json` exists (✅) but uses field names that do not match the Stryker.NET configuration schema:

| Key in file | Expected key |
|---|---|
| `"solutionPath"` | `"solution"` |
| `"projectNameArg"` | (not a valid key) |
| `"projectPathArg"` | `"project"` (path to `.csproj`) |
| `"mutateProjectReferences"` | (not a valid key) |
| `"optimizations"` | (not a valid key) |
| `"thresholds": { "high": 90 }` | varies by version — may be `"threshold-high": 90` |
| `"logLevel"` | `"log-level"` (kebab-case) |

Stryker.NET will likely silently ignore unknown keys and run with defaults, producing no useful scope restriction. The mutation score is unknown — it has not been run.

Replace with a working configuration. Minimal viable config:

```json
{
  "stryker-config": {
    "solution": "Promissio.sln",
    "project": "src/Promissio.Domain/Promissio.Domain.csproj",
    "reporters": ["html", "progress"],
    "threshold-high": 90,
    "threshold-low": 80,
    "threshold-break": 0,
    "log-level": "info"
  }
}
```

Run with `dotnet stryker` from the repo root. The acceptance criterion (≥ 80% mutation score) cannot be marked complete until results exist.

---

## Significant gaps — resolution status

### ✅ Gap 4 — `docs/domain/day-count-conventions.md` created

The file exists and covers all five conventions with formulas, usage contexts, and the reference selection guide. Suitable for a non-developer banking analyst.

**One inaccuracy in the 30E/360 description:**

The doc states:
> If D2 = 31 and D1 = 31 (after adjustment, D1 is now 30), set D2 = 30.

This is wrong. The 30E/360 rule is that D2 is adjusted **unconditionally** — if D2 = 31, set D2 = 30, regardless of D1. The implementation (`Thirty360European.AdjustDay`) is correct (it adjusts any day = 31 to 30 independently). The documentation does not match the code. Fix the doc.

The XML doc comment in `Thirty360European.cs` contains the same error:
```csharp
/// - If D2 = 31 and D1 = 30 (or D1 > 29), set D2 = 30.
```
Should be:
```csharp
/// - If D2 = 31, set D2 = 30.
```

---

### 🟡 Gap 5 — Benchmark results not committed

Still 🔴 — see blocking issues above.

---

### 🟡 Gap 6 — `ActualActual` cross-year expected values remain circular

The previous fix replaced hard-coded wrong expected values with a dynamically computed expectation that mirrors the implementation's own algorithm. The tests now pass, but they are **self-referential**: if the `ActualActual` implementation has a systematic error, the tests would still pass.

`AGENTS.md` §8 requires: *"Each convention has at least 20 test vectors with values sourced from ISDA documentation or ECB illustrative examples."*

The Actual/Actual tests that cross a year boundary (e.g., `2023-09-01 → 2024-03-01`, `2023-10-15 → 2024-04-15`, `2023-11-30 → 2024-05-30`) compute their expected values at test time using the same weighted-segment algorithm. This does not satisfy the reference-value requirement.

**Fix required:** At minimum, lock in 3–5 cross-year vectors against manually verified reference values. Example (already in the doc):

- Sep 1, 2023 → Mar 1, 2024: 121/365 + 61/366 ≈ 0.49817 (doc provides this derivation — use it as a pinned expected value, not a computed one)

---

### ✅ Gap 7 — `FloatingRate.resetSchedule` explicitly marked

`InterestRate.cs` now has `// TODO: Phase N — add reset schedule parameter for re-pricing logic (per 00-core.md spec).` in the `FloatingRate` XML doc. Acceptable for Phase 1.

---

### ✅ Gap 9 — `Percentage` upper-bound guards removed

`FromPercent` and `FromBasisPoints` now only reject negative values. The `FromBasisPoints_Above100Percent_AllowsValue` test confirms 15 000 bps = 150% is accepted.

Minor gap: no corresponding `FromPercent_Above100Percent_AllowsValue` test. Add one alongside the basis-points test for symmetry.

---

## New findings (not in original audit)

### 🟡 New — `DayCountConventionTests.cs` indentation is not formatted

Lines 247, 250, 306–307 have inconsistent leading whitespace that would fail `dotnet format`. This is a pre-commit requirement per `AGENTS.md` §5. Run `dotnet format` before committing.

---

## Acceptance criteria checklist

| Criterion | Status |
|---|---|
| Value objects immutable, value-equal, correct `GetHashCode` | ✅ |
| Property-based tests cover algebraic invariants | ✅ |
| All day-count conventions match reference values (≥ 20 test cases each) | 🟡 Tests run (126 total); Actual/Actual cross-year vectors are circular, not from authoritative source |
| `docs/domain/day-count-conventions.md` exists and is analyst-readable | 🟡 Exists; 30E/360 D2 rule described incorrectly |
| 30+ `InterestCalculator` scenario tests with known correct outputs | ✅ |
| Benchmark results in `benchmarks/results/` | 🔴 Directory missing; benchmarks never executed |
| Mutation testing ≥ 80% (Stryker.NET) | 🔴 Config malformed; score unknown |
| 90%+ line coverage on value object code | Not measured |

---

## Recommended fix order

1. Fix `stryker-config.json` field names (5 min), run `dotnet stryker`, iterate until ≥ 80%.
2. Run benchmarks in Release mode, commit results to `benchmarks/results/` (20 min).
3. Add test vector for D2 = 31, D1 ≥ 30 in `Thirty360Tests` — e.g., Jan 31 → Mar 31 = 60 days (2 min).
4. Fix 30E/360 description in `day-count-conventions.md` and in `Thirty360European.cs` XML doc (5 min).
5. Add 3–5 pinned cross-year `ActualActual` vectors from the ECB/ISDA example in the doc (15 min).
6. Run `dotnet format` on `DayCountConventionTests.cs` (1 min).
7. Add `FromPercent_Above100Percent_AllowsValue` test to `PercentageTests` (2 min).

Items 3–7 are not blockers for Phase 2 entry but should be resolved before the PR is closed.

---

*Audit performed by Claude Code. Cross-year ActualActual expected values and Stryker.NET score require human verification against ISDA 2006 Annex A before closing.*
