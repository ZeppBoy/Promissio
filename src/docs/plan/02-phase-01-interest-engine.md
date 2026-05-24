# Phase 1 — Domain Core: Interest Engine (Weeks 2–4)

> **Companion:** всегда подгружать вместе с `00-core.md`.
> **Weeks:** 2, 3, 4
> **Goal:** Build the financial mathematics foundation of the platform.

---

## Week 2 — Value Objects and Base Types

### Tasks

1. Implement `Money` value object with currency, equality, arithmetic operators, and JSON converter.
2. Implement `Percentage` value object supporting basis points, fractions, and percent representations.
3. Implement `LoanTerm` using NodaTime types.
4. Define `InterestRate` abstract base and four concrete implementations: `FixedRate`, `FloatingRate`, `TieredRate`, `EffectiveRate`.
5. Write property-based tests for all value objects using CsCheck or FsCheck.

### Acceptance criteria

- All value objects are immutable, have value-based equality, and override `GetHashCode` correctly.
- Property-based tests cover algebraic invariants (e.g., addition associativity for `Money` with same currency).
- 90%+ line coverage on value object code.

---

## Week 3 — Day-Count Conventions

### Tasks

1. Define `IDayCountConvention` interface.
2. Implement `Actual360`, `Actual365`, `ActualActual`, `Thirty360`, `Thirty360European` conventions.
3. Source reference test cases from ISDA documentation; encode at least 20 test vectors per convention.
4. Write `/docs/domain/day-count-conventions.md` with mathematical formulas, business context, and links to source materials.

### Acceptance criteria

- All conventions match reference values to the cent for at least 20 test cases each.
- Documentation is reviewable by a non-developer banking analyst.

---

## Week 4 — Interest Calculation Engine

### Tasks

1. Implement `InterestCalculator` accepting principal, rate, convention, start date, end date.
2. Handle leap years, partial periods, grace periods, and month-end conventions.
3. Add 30+ scenario tests with known correct outputs.
4. Add BenchmarkDotNet benchmarks for hot paths.

### Acceptance criteria

- All scenarios match reference calculations.
- Benchmark results checked into `/benchmarks/results/` and tracked across commits.
- Mutation testing via Stryker.NET achieves at least 80% mutation score on calculator code.

---

## AI delegation notes (для всей фазы)

This phase is ideal for pair programming with Claude Code or Cursor. The author specifies the formula and edge cases; the AI generates the implementation skeleton and test scaffolding. The author validates against reference data manually for at least three to five cases per implementation. LLMs occasionally introduce subtle off-by-one errors in financial math — **never accept AI-generated financial code without independent verification.**
