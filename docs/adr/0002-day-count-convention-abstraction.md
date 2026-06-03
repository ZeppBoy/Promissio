# ADR-0002: Day-Count Convention as Abstract Class Hierarchy

**Status:** Accepted  
**Date:** 2026-06-03  
**Deciders:** Development team

---

## Context

Interest calculations require a day-count fraction: a decimal representing what fraction of a year a given period covers. The ISDA 2006 Definitions specify five conventions with distinct algorithms:

- Actual/360, Actual/365, Actual/Actual (ISDA), 30/360 (US), 30E/360 (European)

Each convention has different handling of leap years, month-end rules, and year boundaries. The abstraction must:

1. Allow passing a convention as a parameter to interest rate types
2. Support equality comparison (two `Actual360` instances must be equal so that `FixedRate` equality works correctly)
3. Allow the `Days()` helper to be shared without duplication

## Decision

`DayCountConvention` is an `abstract class` (not `abstract record` or `interface`) with:

- `abstract string Name { get; }` — convention name, used for equality
- `abstract Decimal Fraction(LocalDate, LocalDate)` — the core calculation
- `virtual int Days(LocalDate, LocalDate)` — shared NodaTime-native helper using `Period.Between`
- `Equals`/`GetHashCode` implemented using `Name` for value-based equality

Concrete types (`Actual360`, `Actual365`, `ActualActual`, `Thirty360`, `Thirty360European`) are `sealed class`.

## Why Abstract Class, Not Interface

An interface would require each implementer to independently implement `Days()`. Since `Days()` is identical for all conventions, an abstract class avoids duplication while keeping the implementation shareable.

Shared equality logic (`Equals`/`GetHashCode` by `Name`) also lives in the abstract class. With an interface, this would be a default interface method or repeated in each implementer.

## Why Name-Based Equality

Two `Actual360` instances created independently must compare equal so that `FixedRate(5%, new Actual360())` equals another `FixedRate(5%, new Actual360())`. Name-based equality satisfies this without requiring singleton conventions.

## Why Not Records

`DayCountConvention` types are stateless (no per-instance data). Converting to `abstract record` would synthesise equality over an empty property set, which would make all conventions equal to each other. Name-based equality in the abstract class is more explicit and correct.

## Consequences

- Equality between `DayCountConvention` instances is name-based. Two different convention types with accidentally identical names would be erroneously equal — this is considered an acceptable risk given names are constants defined in code.
- `DayCountConvention` is not a value object in the record sense; it is an identity-light domain service. This is consistent with AGENTS.md, which distinguishes value objects from domain services.
