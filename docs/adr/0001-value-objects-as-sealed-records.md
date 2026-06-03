# ADR-0001: Value Objects as Sealed Records

**Status:** Accepted  
**Date:** 2026-06-03  
**Deciders:** Development team

---

## Context

Domain value objects in Promissio (Money, Percentage, LoanTerm, interest rate types) require:

- Value-based equality (two `Money(100, "EUR")` instances must compare equal)
- Immutability after construction
- Clear, minimal boilerplate
- Compiler-enforced conventions

The original implementation used `sealed class` with manually implemented `IEquatable<T>`, `Equals(object?)`, `GetHashCode()`, and `operator ==`/`!=`. This is error-prone: each value object required ~10 lines of equality boilerplate, any of which could be subtly wrong.

## Decision

All domain value objects are implemented as `sealed record` (or `abstract record` for hierarchies). Specifically:

- `Money` — non-positional record, `{ get; }` properties, custom constructor to enforce banker's rounding invariant
- `Percentage` — positional record `record Percentage(Decimal Fraction)`
- `LoanTerm` — non-positional record, private constructor (factory methods only), computed `Years`/`Months` as expression-body properties excluded from equality
- `InterestRate` — `abstract record`; concrete subtypes are `sealed record`
- `TieredRate` — sealed record with custom `Equals`/`GetHashCode` to enforce sequence equality on the `Tiers` list

## Consequences

**Positive:**
- Compiler synthesises correct value equality, `GetHashCode`, and `==`/`!=`
- `Deconstruct` support for free on positional records
- `with` expression support where properties are `{ get; init; }`
- Intent is explicit: `record` communicates "value object" at a glance

**Negative / Trade-offs:**
- For `Money`, `{ get; }` properties (not `init`) prevent `with` expressions — this is intentional because the constructor enforces rounding, and bypassing it via `with { Amount = x }` would violate the invariant
- `TieredRate` still needs manual `Equals`/`GetHashCode` because `IReadOnlyList<Tier>` has reference-based default equality; switching to `ImmutableArray<Tier>` would eliminate this but introduces a dependency on `System.Collections.Immutable`

## Alternatives Considered

**Keep as sealed class:** Discarded. Manual equality boilerplate is error-prone and produces no functional advantage over records for immutable value types.

**Use struct instead of record:** Discarded. `struct` causes boxing in generic collections, has copy semantics that can be surprising for larger value objects (Money, TieredRate), and doesn't support inheritance for the InterestRate hierarchy.
