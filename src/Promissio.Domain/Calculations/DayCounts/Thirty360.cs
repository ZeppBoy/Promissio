using NodaTime;

namespace Promissio.Domain.Calculations.DayCounts;

/// <summary>
/// 30/360 day-count convention (US method).
/// </summary>
/// <remarks>
/// Assumes each month has 30 days and each year has 360 days.
/// Date adjustment rules per ISDA 2006 Definitions:
/// - If D1 = 31, set D1 = 30.
/// - If D2 = 31 and D1 > 29 (or D1 = 31), set D2 = 30.
/// Formula: (Y2 - Y1) * 12 + (M2 - M1) + (D2 - D1) / 30
/// Commonly used in US corporate bonds, municipal bonds, and mortgage-backed securities.
/// Reference: ISDA 2006 Definitions, Section 4.16; also known as US 30/360 or BAB 30/360.
/// </remarks>
public sealed class Thirty360 : DayCountConvention
{
    public override string Name => "30/360";

    public override Decimal Fraction(LocalDate startDate, LocalDate endDate)
    {
        int year1 = AdjustYear(startDate);
        int month1 = AdjustMonth(startDate);
        int day1 = AdjustDay(startDate, endDate, isStart: true);

        int year2 = AdjustYear(endDate);
        int month2 = AdjustMonth(endDate);
        int day2 = AdjustDay(endDate, startDate, isStart: false);

        int days = (year2 - year1) * 360 + (month2 - month1) * 30 + (day2 - day1);
        return days / 360m;
    }

    private static int AdjustYear(LocalDate date) => date.Year;

    private static int AdjustMonth(LocalDate date) => date.Month;

    private static int AdjustDay(LocalDate date, LocalDate otherDate, bool isStart)
    {
        int day = date.Day;

        if (isStart)
        {
            if (day == 31) day = 30;
        }
        else
        {
            if (day == 31 && otherDate.Day < 30) day = 30;
        }

        return day;
    }
}
