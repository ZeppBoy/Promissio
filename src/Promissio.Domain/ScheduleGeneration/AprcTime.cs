using NodaTime;

namespace Promissio.Domain.ScheduleGeneration;

internal static class AprcTime
{
    // SWD(2012)128 section 4.1.1: count whole periods backwards, then residual days.
    internal static double Years(LocalDate start, LocalDate end, AprcPeriodUnit unit)
    {
        int count = unit switch
        {
            AprcPeriodUnit.Months => (end.Year - start.Year) * 12 + end.Month - start.Month,
            AprcPeriodUnit.Years => end.Year - start.Year,
            _ => Period.Between(start, end, PeriodUnits.Days).Days / 7
        };
        LocalDate boundary = Back(end, count, unit);
        if (boundary < start)
            boundary = Back(end, --count, unit);
        int days = Period.Between(start, boundary, PeriodUnits.Days).Days;
        int denominator = Period.Between(boundary.PlusYears(-1), boundary, PeriodUnits.Days).Days;
        double periodsPerYear = unit switch
        {
            AprcPeriodUnit.Months => 12d,
            AprcPeriodUnit.Years => 1d,
            _ => 52d
        };
        return count / periodsPerYear + (double)days / denominator;
    }

    private static LocalDate Back(LocalDate end, int count, AprcPeriodUnit unit) => unit switch
    {
        AprcPeriodUnit.Months => end.PlusMonths(-count),
        AprcPeriodUnit.Years => end.PlusYears(-count),
        _ => end.PlusDays(-7 * count)
    };
}
