using NodaTime;

namespace Promissio.Domain.Calculations.DayCounts;

/// <summary>
/// 30/360 European day-count convention (ISDA European method).
/// </summary>
/// <remarks>
/// Similar to 30/360 but with different end-of-month handling.
/// Date adjustment rules per ISDA 2006 Definitions:
/// - If D1 = 31, set D1 = 30.
/// - If D2 = 31, set D2 = 30 (unconditional).
/// Formula: (Y2 - Y1) * 360 + (M2 - M1) * 30 + (D2 - D1) / 360
/// Used in European bonds and ISDA derivatives.
/// Reference: ISDA 2006 Definitions, Section 4.16; also known as 30E/360 or Euro 30/360.
/// </remarks>
public sealed class Thirty360European : DayCountConvention
{
    public override string Name => "30E/360";

    public override Decimal Fraction(LocalDate startDate, LocalDate endDate)
    {
        int year1 = startDate.Year;
        int month1 = startDate.Month;
        int day1 = AdjustDay(startDate);

        int year2 = endDate.Year;
        int month2 = endDate.Month;
        int day2 = AdjustDay(endDate);

        int days = (year2 - year1) * 360 + (month2 - month1) * 30 + (day2 - day1);
        return days / 360m;
    }

    private static int AdjustDay(LocalDate date)
    {
        int day = date.Day;

        if (day == 31)
            day = 30;

        return day;
    }
}
