# Interest Calculation Engine

## Overview

The interest calculation engine is the core domain logic for computing interest in Promissio. It provides a single, authoritative path for all interest computations, ensuring consistency and correctness across the platform.

## Architecture

### Core Components

1. **IInterestCalculator Interface** - Defines the contract for interest calculations
2. **InterestCalculator Implementation** - The main implementation that delegates to appropriate rate types
3. **InterestRate Abstract Base Class** - Encapsulates different rate types (Fixed, Floating, Tiered, Effective)
4. **DayCountConvention System** - Implements various day-count conventions used in interest calculations

### Value Objects

1. **Money** - Immutable monetary amount with currency
2. **Percentage** - Percentage value with multiple representations (percent, basis points, fraction)
3. **LoanTerm** - Duration of loan contracts using NodaTime Period
4. **InterestRate Variants** - Fixed, Floating, Tiered, and Effective rates

## Key Features

### Rate Types

#### FixedRate
A fixed interest rate that remains constant throughout the life of the loan.
- Uses a specified day-count convention (Actual/360, Actual/365, etc.)
- Simple calculation: `Interest = Principal × Rate × Fraction`

#### FloatingRate
An interest rate that varies based on a reference rate plus a margin.
- Combines base rate and fixed margin
- Can incorporate reset schedules (planned for future implementation)
- Uses specified day-count convention

#### TieredRate
An interest rate that applies different rates based on balance bands or time periods.
- Supports multiple tiers with upper limits
- Calculates effective rate based on current balance or period
- Uses specified day-count convention

#### EffectiveRate
Represents the Annual Percentage Rate of Charge (APRC), calculated using iterative solver per EU Consumer Credit Directive.
- Uses specified day-count convention
- Calculated via iterative approximation methods

### Day-Count Conventions

All rate types support various day-count conventions:
- Actual/360
- Actual/365  
- Actual/Actual
- 30/360 (US)
- 30E/360 (European)

## Usage Pattern

The interest calculation engine is accessed through the `IInterestCalculator` interface:

```csharp
var calculator = new InterestCalculator();
var interest = calculator.Calculate(principal, rate, startDate, endDate);
```

All financial calculations in the domain must go through this interface - inline mathematical expressions are forbidden.

## Design Principles

1. **Single Source of Truth**: All interest computation flows through `IInterestCalculator`
2. **Rate Encapsulation**: Each rate type encapsulates its own logic and day-count convention
3. **Domain-Driven Design**: The engine is designed for the specific needs of loan servicing
4. **Financial Correctness**: Implements industry-standard formulas with reference test cases
5. **Immutability**: All value objects are immutable to ensure consistency

## References

- ISDA 2006 Definitions, Section 4.16 - Day count conventions
- EU Consumer Credit Directive 2008/48/EC (as amended) - APRC calculation
- IFRS 9 Financial Instruments standard - Interest recognition