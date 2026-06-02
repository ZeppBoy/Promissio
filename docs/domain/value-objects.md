# Value Objects in Promissio.Domain

## Overview

Value objects are immutable domain concepts that represent values without identity. They are fundamental to the domain model and ensure data integrity.

## Money

### Description
Immutable representation of a monetary amount with an associated currency.

### Properties
- `Amount`: The monetary value (decimal)
- `Currency`: ISO currency code (string)

### Key Features
- Equality based on value, not reference identity
- All arithmetic operations enforce same-currency constraint
- Uses decimal internally to avoid floating-point rounding issues
- Supports standard arithmetic operators (+, -, *, /)
- Implements comparison operators (<, >, <=, >=)

### Usage
```csharp
var amount1 = new Money(1000.00m, "USD");
var amount2 = new Money(500.00m, "USD");
var sum = amount1 + amount2; // Returns Money with Amount 1500.00 and Currency "USD"
```

## Percentage

### Description
Immutable representation of a percentage value with explicit unit.

### Properties
- `Fraction`: Stored internally as decimal fraction (e.g., 5% = 0.05m)
- `AsPercent`: Returns percentage as decimal (e.g., 0.05 → 5.0)
- `AsBasisPoints`: Returns basis points value (e.g., 0.05 → 500)

### Key Features
- Multiple conversion methods: FromPercent, FromBasisPoints, FromFraction
- Supports arithmetic operations (+, -, *, /)
- Implements equality and comparison operators

### Usage
```csharp
var rate = Percentage.FromPercent(5.25m); // 5.25%
var basisPoints = rate.AsBasisPoints; // 525
var fraction = rate.Fraction; // 0.0525
```

## LoanTerm

### Description
Immutable representation of a loan term using NodaTime Period.

### Properties
- `TotalMonths`: Total months in the term
- `Years`: Years component of the term
- `Months`: Months component of the term (remaining after years)

### Key Features
- Constructor validates that terms are positive
- Provides `EndDate(LocalDate startDate)` to calculate end date
- Implements equality based on total months

### Usage
```csharp
var term = LoanTerm.FromYears(5); // 5 years
var endDate = term.EndDate(startDate);
```

## InterestRate

### Description
Abstract base class for interest rate representations. Different concrete implementations support various rate structures.

### Subtypes

#### FixedRate
A fixed interest rate that does not change over the life of the loan.
- Encapsulates day-count convention
- Implements standard interest calculation formula

#### FloatingRate
An interest rate based on a reference rate plus a fixed margin.
- Combines base rate and fixed margin
- Supports future reset schedule implementation

#### TieredRate
An interest rate that applies different rates based on balance bands or time periods.
- Supports multiple tiers with upper limits
- Calculates effective rate based on current conditions

#### EffectiveRate
Represents the APRC (Annual Percentage Rate of Charge) calculated using iterative solver per EU Consumer Credit Directive.
- Uses specified day-count convention
- Implements advanced calculation methods

### Key Features
- Each rate type encapsulates its own calculation logic
- All rates support a common interface for interest calculation
- Day-count conventions are associated with each rate type

### Usage
```csharp
var fixedRate = new FixedRate(Percentage.FromPercent(5.25m), new Actual360());
var interest = fixedRate.CalculateInterest(principal, startDate, endDate);
```

## Design Principles

1. **Immutability**: All value objects are immutable to ensure consistency and prevent unintended changes
2. **Value-Based Equality**: Equality is based on property values rather than object identity
3. **Type Safety**: Strong typing prevents invalid operations (e.g., adding different currencies)
4. **Financial Correctness**: Designed with financial domain requirements in mind
5. **Domain Integrity**: Enforces business rules and constraints at the value object level

## References

- ISDA 2006 Definitions - Financial terminology and conventions
- EU Consumer Credit Directive - APRC calculation standards
- IFRS 9 Financial Instruments - Interest recognition principles