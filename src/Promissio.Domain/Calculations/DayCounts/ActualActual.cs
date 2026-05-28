using NodaTime;

namespace Promissio.Domain.Calculations.DayCounts;

/// <summary>
/// Actual/Actual day-count convention.
/// </summary>
/// <remarks>
/// Counts actual days in the period and divides by the actual number of days in the year(s).
/// Handles leap years correctly: 366 for leap years, 365 otherwise.
/// For periods spanning multiple years, uses a weighted average approach.
/// Used in US Treasury bonds and some interbank markets.
/// Reference: ISDA 2006 Definitions, Section 4.16.
/// </remarks>
public sealed class ActualActual : DayCountConvention
{
    public override string Name => "Actual/Actual";

    public override Decimal Fraction(LocalDate startDate, LocalDate endDate)
    {
        int totalDays = (endDate.ToDateTimeUnspecified() - startDate.ToDateTimeUnspecified()).Days;
        if (totalDays == 0) return 0m;

        if (startDate.Year == endDate.Year)
        {
            int daysInYear = IsLeapYear(startDate.Year) ? 366 : 365;
            return totalDays / (Decimal)daysInYear;
        }

        Decimal fraction = 0m;
        LocalDate current = startDate;
        while (current.Year < endDate.Year)
        {
            int daysInYear = IsLeapYear(current.Year) ? 366 : 365;
            LocalDate nextYear = current.PlusYears(1);

            if (nextYear > endDate)
                nextYear = endDate;

            int daysInSegment = (nextYear.ToDateTimeUnspecified() - current.ToDateTimeUnspecified()).Days;
            fraction += daysInSegment / (Decimal)daysInYear;

            current = nextYear;
        }

        return fraction;
    }

    private static bool IsLeapYear(int year) =>
        (year % 4 == 0 && year % 100 != 0) || year % 400 == 0;
}
