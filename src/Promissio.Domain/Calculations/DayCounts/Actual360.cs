using NodaTime;

namespace Promissio.Domain.Calculations.DayCounts;

/// <summary>
/// Actual/360 day-count convention.
/// </summary>
/// <remarks>
/// Counts actual days in the period, divides by 360.
/// Commonly used in money markets and US corporate bonds.
/// Reference: ISDA 2006 Definitions, Section 4.16.
/// </remarks>
public sealed class Actual360 : DayCountConvention
{
    public override string Name => "Actual/360";

    public override Decimal Fraction(LocalDate startDate, LocalDate endDate)
    {
        int days = (endDate.ToDateTimeUnspecified() - startDate.ToDateTimeUnspecified()).Days;
        return days / 360m;
    }
}
