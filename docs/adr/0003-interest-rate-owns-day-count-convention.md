# ADR-0003: Interest Rate Type Owns Its Day-Count Convention

**Status:** Accepted  
**Date:** 2026-06-03  
**Deciders:** Development team

---

## Context

Interest rate calculations require two inputs beyond principal and dates: the rate value and the day-count convention. These can be associated in different ways:

1. **Rate owns convention** — `new FixedRate(5%, Actual360)`. The convention is part of the rate's identity.
2. **Convention passed at call time** — `rate.CalculateInterest(principal, start, end, Actual360)`. The convention is separate.
3. **Convention on the loan contract** — the `Loan` aggregate holds the convention, and passes it to the calculator.

## Decision

Each `InterestRate` subtype holds its `DayCountConvention` as a constructor parameter. The `CalculateInterest(Money, LocalDate, LocalDate)` method uses the convention stored on the rate instance.

The `IInterestCalculator` interface signature is `Calculate(Money principal, InterestRate rate, LocalDate start, LocalDate end)` — no convention parameter because the rate already knows its convention.

## Rationale

In practice, the day-count convention is a contractual term tied to the rate type, not to individual calculation calls. A LIBOR-based floating rate uses Actual/360; a US Treasury uses Actual/Actual. Encoding the convention in the rate object matches how contracts are written and prevents miscalculation by passing the wrong convention at call time.

This also simplifies `IInterestCalculator`: callers provide principal and dates; the rate encapsulates how to convert dates to a fraction.

## Consequences

**Positive:**
- `FixedRate(5%, Actual360)` is a complete, self-describing value object — equality works naturally (two identical rate+convention pairs are equal)
- `IInterestCalculator` stays clean; no convention plumbing at each call site
- A rate can be serialised/deserialised without loss of its calculation semantics

**Negative / Trade-offs:**
- Creating a `FixedRate` requires knowing the convention upfront — there is no "default" convention. This is intentional: AGENTS.md §domain states that convention is a contractual term and must never be assumed.
- Changing the convention for an existing rate requires creating a new rate instance (since records are immutable). This is correct behaviour — a change in convention is a new contractual term, not a mutation.

## Alternatives Considered

**Convention on IInterestCalculator call site:** Discarded. This makes it easy to accidentally pass the wrong convention and decouples information that should travel together.

**Convention on the Loan aggregate:** Discarded for Phase 1. In future phases, when a `Loan` aggregate holds multiple rate-period combinations, the convention may be on the loan contract rather than the rate. This ADR will be revisited at that point.
