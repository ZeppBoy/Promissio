# Day-Count Conventions

Day-count conventions define how to convert the elapsed time between two dates into a fraction of a year for interest computation. The choice of convention affects accrued interest, yield calculations, and pricing.

## Conventions Implemented

### Actual/360

- **Numerator:** Actual calendar days between start and end dates.
- **Denominator:** 360.
- **Formula:** `D / 360` where `D = endDate - startDate` (calendar days).
- **Usage:** US money market instruments, LIBOR-based loans, some US corporate bonds.
- **Reference:** ISDA 2006 Definitions, Section 4.16.

### Actual/365

- **Numerator:** Actual calendar days between start and end dates.
- **Denominator:** 365 (fixed, even in leap years).
- **Formula:** `D / 365` where `D = endDate - startDate` (calendar days).
- **Usage:** UK gilt markets, some European bonds.
- **Reference:** ISDA 2006 Definitions, Section 4.16.

### Actual/Actual

- **Numerator:** Actual calendar days between start and end dates.
- **Denominator:** 365 in common years, 366 in leap years.
- **Formula:** For periods within a single year: `D / AY` where `AY` is the actual number of days in the year (365 or 366). For multi-year periods, uses segment weighting per ISDA methodology.
- **Usage:** US Treasury bonds, IBORs in some jurisdictions.
- **Reference:** ISDA 2006 Definitions, Section 4.16; Treasury Regulation § 1.163-11(c)(2).

### 30/360 (US)

- **Numerator:** Adjusted days assuming each month has 30 days.
- **Denominator:** 360.
- **Date adjustment rules:**
  1. If `D1 = 31`, set `D1 = 30`.
  2. If `D2 = 31` and `D1 > 29` (or equivalently `D1 = 31`), set `D2 = 30`.
- **Formula:** `(Y2 - Y1) × 360 + (M2 - M1) × 30 + (D2 - D1)`
- **Usage:** US corporate bonds, municipal bonds, mortgage-backed securities.
- **Reference:** ISDA 2006 Definitions, Section 4.16; also known as US 30/360 or BAB 30/360.

### 30E/360 (European)

- **Numerator:** Adjusted days assuming each month has 30 days.
- **Denominator:** 360.
- **Date adjustment rules:**
  1. If `D1 = 31`, set `D1 = 30`.
  2. If `D2 = 31`, set `D2 = 30`.
- **Formula:** `(Y2 - Y1) × 360 + (M2 - M1) × 30 + (D2 - D1)`
- **Key difference from US 30/360:** The European version always adjusts the end date if it is the 31st, regardless of the start date. The US method only adjusts the end date if the start date is also at month-end (D1 > 29).
- **Usage:** European bonds, ISDA derivatives in Europe.
- **Reference:** ISDA 2006 Definitions, Section 4.16; also known as Euro 30/360 or 30E/360.

## Example Calculations

| Convention | Start Date | End Date | Days | Fraction |
|---|---|---|---|---|
| Actual/360 | 2023-01-01 | 2023-04-15 | 104 | 104/360 = 0.2889 |
| Actual/365 | 2023-01-01 | 2023-04-15 | 104 | 104/365 = 0.2849 |
| Actual/Actual | 2023-01-01 | 2023-04-15 | 104 | 104/365 = 0.2849 |
| 30/360 (US) | 2023-01-01 | 2023-04-15 | 104 | 104/360 = 0.2889 |
| 30E/360 | 2023-01-01 | 2023-04-15 | 104 | 104/360 = 0.2889 |

## Implementation Notes

- All conventions use NodaTime `LocalDate` for date arithmetic, avoiding timezone issues.
- The `DayCountConvention` base class provides the `Days()` method for actual calendar day counts.
- Each convention overrides `Fraction()` to apply its specific numerator/denominator logic.
- Multi-year Actual/Actual uses ISDA segment weighting for accuracy across year boundaries.
