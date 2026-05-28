# Day-Count Conventions

## Overview

Day-count conventions determine how to compute the fraction of a year represented by a date interval. The fraction is used to compute interest:

```
Interest = Principal × Rate × Fraction
```

The choice of convention affects the monetary amount of interest accrued. Different conventions are mandated by market practice, regulatory requirements, or contractual terms.

---

## Available Conventions

### Actual/360

**Formula:** `Fraction = Actual Days / 360`

Counts the actual calendar days between the start and end dates, then divides by 360 (not 365). This convention produces a slightly higher interest amount compared to Actual/365.

**Usage:** US money market instruments, LIBOR-based loans, commercial paper.

**Example:** January 1 → January 31 = 30 actual days → Fraction = 30/360 = 0.08333...

---

### Actual/365

**Formula:** `Fraction = Actual Days / 365`

Counts the actual calendar days between the start and end dates, then divides by 365. Leap years do not affect the denominator.

**Usage:** US Treasury bills, some bank loans.

**Example:** January 1 → January 31 = 30 actual days → Fraction = 30/365 = 0.08219...

---

### Actual/Actual

**Formula:** Weighted sum of year segments.

For periods within a single calendar year:
```
Fraction = Actual Days / Days in Year
```
where `Days in Year` is 366 for leap years, 365 otherwise.

For periods spanning multiple years, the fraction is computed as the sum of weighted segments:
```
Fraction = Σ (Days in Segment i / Days in Year i)
```

Each segment covers from the start date to either the end of that calendar year or the end date, whichever comes first. Each segment is divided by the actual number of days in that segment's calendar year.

**Usage:** US Treasury bonds, some interbank markets, IFRS 9 interest computations.

**Example (same year):** September 1, 2023 → March 1, 2024 crosses a year boundary:
- Segment 1 (Sep 1, 2023 → Dec 31, 2023): 121 days / 365 = 0.33151...
- Segment 2 (Jan 1, 2024 → Mar 1, 2024): 61 days / 366 = 0.16667...
- Total: 0.33151 + 0.16667 = 0.49817...

---

### 30/360

**Formula:** `Fraction = Adjusted Days / 360`

Assumes each month has exactly 30 days and each year has 360 days. Dates are adjusted before computing the difference:

1. If D1 (start day) = 31, set D1 = 30.
2. If D2 (end day) = 31 **and** D1 ≥ 30, set D2 = 30.

Adjusted days are computed as:
```
Days = (Y2 - Y1) × 360 + (M2 - M1) × 30 + (D2 - D1)
```

**Usage:** US corporate bonds, municipal bonds, mortgage-backed securities.

**Example:** January 1 → February 1:
- No adjustment needed. Days = (0) × 360 + (1) × 30 + (1 - 1) = 30 → Fraction = 30/360.

**Example with adjustment:** January 31 → March 31:
- D2 = 31, D1 = 31 ≥ 30, so set D2 = 30. Days = (0) × 360 + (2) × 30 + (30 - 30) = 60 → Fraction = 60/360.

---

### 30E/360 (European)

**Formula:** Same as 30/360, with unconditional D2 adjustment.

1. If D1 = 31, set D1 = 30.
2. If D2 = 31, set D2 = 30.

Both adjustments are independent — each date's day is adjusted to 30 if it equals 31, regardless of the other date.

**Usage:** European bonds, ISDA derivatives.

**Example:** March 31 → April 30:
- D1 = 31, so set D1 = 30. D2 = 30 (no change). Days = 30 → Fraction = 30/360.

**Example:** January 31 → March 31:
- D1 = 31, so set D1 = 30. D2 = 31, so set D2 = 30. Days = (0) × 360 + (2) × 30 + (30 - 30) = 60 → Fraction = 60/360.

---

## Reference

All conventions follow ISDA 2006 Definitions, Section 4.16. For authoritative reference cases, consult:

- **ISDA 2006 Definitions, Annex A** — Day count fractions for each convention.
- **ECB (European Central Bank)** — Illustrative examples for Actual/Actual.
- **Federal Reserve** — Day-count conventions for US Treasury securities.

---

## Choosing a Convention

| Consideration | Recommendation |
|---|---|
| US corporate bonds, MBS | 30/360 |
| European bonds, ISDA derivatives | 30E/360 |
| Money market, LIBOR-based loans | Actual/360 |
| US Treasuries | Actual/Actual |
| Regulatory reporting (IFRS 9) | Actual/Actual |
| Contractual requirement | Always follow the contract |

**Rule:** The day-count convention is a contractual term. Never assume a default. If the convention is not specified in the contract, escalate to the business owner before proceeding.
