# Phase 1 Audit — Final Summary & Recommendations
**Date:** 2026-05-27  
**Branch:** main (working tree, uncommitted changes)  
**Baseline commit:** b16c7a1 — Audit for Phase 1  
**Test result:** 232 pass, 0 fail (`dotnet test tests/Promissio.Domain.Tests`)  
**Benchmark build:** ✅ 0 errors, 0 warnings

This document is the final audit pass for Phase 1. It supersedes both `phase-1-audit-2026-05-27.md` and `phase-1-audit-post-fix-2026-05-27.md`.

---

## Overall Verdict

**Phase 1 is not complete. Two blockers remain before Phase 2 entry is permitted.**

All three original blockers are resolved. All six original significant gaps are resolved or addressed. Two Phase 2 entry gates — benchmark results and mutation testing — remain unmet.

---

## What Is Done

| Area | Status | Notes |
|---|---|---|
| Value objects (`Money`, `Percentage`, `LoanTerm`) | ✅ | Immutable, value-equal, correct `GetHashCode` |
| `InterestRate` hierarchy (Fixed, Floating, Tiered, Effective) | ✅ | All four types implemented |
| `FloatingRate` reset schedule placeholder | ✅ | `TODO: Phase N` comment in XML doc |
| `Percentage` upper-bound guards removed | ✅ | Only rejects negatives; >100% allowed |
| `FromPercent_Above100Percent_AllowsValue` test | ✅ | Present in `PercentageTests.cs` |
| Property-based tests (CsCheck/FsCheck) | ✅ | Algebraic invariants covered |
| Day-count conventions (all 5) | ✅ | Implemented and tested |
| `Thirty360.AdjustDay` inversion bug | ✅ | Fixed: `>= 30` condition correct |
| D2=31, D1≥30 boundary test vector | ✅ | `[InlineData(2023, 1, 31, 2023, 3, 31, 60)]` |
| `Thirty360European` D2 rule — doc | ✅ | Correctly states unconditional adjustment |
| `Thirty360European` D2 rule — XML doc | ✅ | `If D2 = 31, set D2 = 30 (unconditional)` |
| `docs/domain/day-count-conventions.md` | ✅ | Accurate, analyst-readable |
| Day-count test visibility (`public void`) | ✅ | All `[Theory]` methods are public |
| `InterestCalculator` scenario tests | ✅ | 30+ scenarios with known outputs |
| Benchmark project compiles | ✅ | 0 errors, 0 warnings |
| `DayCountConventionTests.cs` formatting | ✅ | No indentation irregularities |

---

## Blockers — Phase 2 Entry Gates

### 🔴 Blocker A — Benchmark results not committed

`benchmarks/results/` does not exist. The acceptance criterion for Week 4 is explicit: *"benchmark results checked into `benchmarks/results/` and tracked across commits."* The benchmark project builds and the runner is complete — this is purely an execution and commit step.

**Effort:** ~20 minutes.

**Steps:**
```
dotnet run --project benchmarks/Promissio.Benchmarks -c Release -- --filter "*"
```
After completion, BenchmarkDotNet writes reports to `BenchmarkDotNet.Artifacts/results/`. Copy or move those files to `benchmarks/results/` and commit.

---

### 🔴 Blocker B — Mutation testing not run; score unknown

Two sub-issues compound each other:

**Sub-issue 1 — Stryker.NET not installed.**  
`dotnet tool list -g` shows no stryker installation. No `.config/dotnet-tools.json` exists in the repo. The tool cannot be invoked.

**Sub-issue 2 — `stryker-config.json` missing outer wrapper and `threshold-break`.**  
Current file:
```json
{
  "solution": "Promissio.sln",
  "project": "src/Promissio.Domain/Promissio.Domain.csproj",
  "reporters": ["html", "progress"],
  "threshold-high": 90,
  "threshold-low": 80,
  "log-level": "info"
}
```
Stryker.NET requires the `"stryker-config"` wrapper. Without it, all keys are silently ignored and the tool runs with defaults, producing no meaningful scope restriction.

**Correct config:**
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

**Effort:** ~30 minutes (install + fix config + first run + iterate if score < 80%).

**Steps:**
```
# 1. Add local tool manifest (one-time repo setup)
dotnet new tool-manifest

# 2. Install Stryker
dotnet tool install dotnet-stryker

# 3. Fix stryker-config.json (see above)

# 4. Run from repo root
dotnet stryker

# 5. Commit StrykerOutput/reports/ or a results summary to benchmarks/results/
```

The acceptance criterion (≥ 80% mutation score on calculator code) cannot be marked complete until results exist.

---

## Non-Blocking Gap

### 🟡 Gap — ActualActual cross-year test vectors are self-referential

The cross-year test cases in `ActualActualTests` (e.g., `2023-09-01 → 2024-03-01`, `2023-10-15 → 2024-04-15`, `2023-11-30 → 2024-05-30`) compute their expected values at test-run time using the same weighted-segment algorithm as the implementation. If the `ActualActual` implementation has a systematic error, these tests would still pass — they verify internal consistency, not correctness against an authoritative source.

`AGENTS.md §8` requires: *"Each convention has at least 20 test vectors with values sourced from ISDA documentation or ECB illustrative examples."*

**The documentation already provides the derivation:**
> Sep 1, 2023 → Mar 1, 2024: 121/365 + 61/366 ≈ 0.49817...

That derivation should appear as a pinned `decimal` literal in the test, not computed at test time.

**Fix required:** Replace the dynamic computation block in `ActualActualTests.Fraction_WithinSameYear_ReturnsCorrectValue` for cross-year cases with 3–5 tests that use manually verified expected values. Example:

```csharp
[InlineData(2023, 9, 1, 2024, 3, 1, "0.498...")]  // 121/365 + 61/366, from ECB example
```

The exact decimal should be computed once by hand (or from the ECB document), verified, and hard-coded. Not derived from the implementation at test time.

**Effort:** ~15 minutes.  
**This is not a Phase 2 entry blocker** per the plan, but the PR should not close with it unresolved.

---

## Acceptance Criteria Checklist

| Criterion (from `02-phase-01-interest-engine.md`) | Status |
|---|---|
| Value objects immutable, value-equal, correct `GetHashCode` | ✅ |
| Property-based tests cover algebraic invariants | ✅ |
| All conventions match reference values, ≥ 20 test cases each | 🟡 Actual/Actual cross-year vectors are circular |
| `docs/domain/day-count-conventions.md` exists, analyst-readable | ✅ |
| 30+ `InterestCalculator` scenario tests with known correct outputs | ✅ |
| Benchmark results in `benchmarks/results/`, tracked across commits | 🔴 Directory missing |
| Mutation testing ≥ 80% via Stryker.NET | 🔴 Tool not installed; score unknown |
| 90%+ line coverage on value object code | Not measured |

---

## Recommended Fix Order

| # | Task | Effort | Blocks Phase 2? |
|---|---|---|---|
| 1 | Fix `stryker-config.json` — add outer wrapper and `threshold-break` | 2 min | Yes |
| 2 | Install Stryker: `dotnet new tool-manifest && dotnet tool install dotnet-stryker` | 2 min | Yes |
| 3 | Run `dotnet stryker`, iterate until ≥ 80% mutation score | 20 min | Yes |
| 4 | Run benchmarks in Release mode, commit results to `benchmarks/results/` | 20 min | Yes |
| 5 | Pin 3–5 ActualActual cross-year vectors from ECB/ISDA derivation in doc | 15 min | No (before PR close) |

**Total estimated effort to Phase 2 entry: ~45 minutes.**

---

## Notes for the Next Session

- After completing items 1–4, all Phase 1 acceptance criteria will be met and Phase 2 entry is permitted.
- Item 5 (pinned ActualActual vectors) must be done before merging; it does not block Phase 2 work from starting in parallel.
- The `90%+ line coverage` criterion has not been measured at any point during Phase 1. Consider adding Coverlet to the test project and running `dotnet test --collect:"XPlat Code Coverage"` as part of the CI pipeline setup in Phase 2.
- All financial math in Phase 1 has been implemented with NodaTime (`LocalDate`) and `decimal` arithmetic throughout. No `System.DateTime` or `double` found in domain code.

---

*Audit performed by Claude Code. ActualActual cross-year expected values and Stryker.NET mutation score require human verification against ISDA 2006 Annex A before the PR is closed.*
