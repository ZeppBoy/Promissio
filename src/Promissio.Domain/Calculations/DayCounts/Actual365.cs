using NodaTime;

namespace Promissio.Domain.Calculations.DayCounts;

/// <summary>
/// Actual/365 day-count convention.
/// </summary>
/// <remarks>
/// Counts actual days in the period, divides by 365.
/// Used in UK government bonds and some money market instruments.
/// Reference: ISDA 2006 Definitions, Section 4.16.
/// </remarks>
public sealed class Actual365 : DayCountConvention
{
    public override string Name => "Actual/365";

    public override Decimal Fraction(LocalDate startDate, LocalDate endDate)
    {
        int days = Period.Between(startDate, endDate, PeriodUnits.Days).Days;
        return days / 365m;
    }
}
