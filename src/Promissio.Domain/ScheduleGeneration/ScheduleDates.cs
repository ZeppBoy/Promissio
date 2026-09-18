using NodaTime;
using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.ScheduleGeneration;

internal static class ScheduleDates
{
    internal static void Validate(Money principal, InterestRate rate, int termMonths,
        LocalDate startDate, int gracePeriodMonths, LocalDate? firstPaymentDate)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(rate);
        if (principal.Amount <= 0)
            throw new ArgumentException("Principal must be positive.", nameof(principal));
        if (termMonths <= 0)
            throw new ArgumentException("Term must be positive.", nameof(termMonths));
        if (gracePeriodMonths < 0 || gracePeriodMonths >= termMonths)
            throw new ArgumentException("Grace must be non-negative and shorter than the term.", nameof(gracePeriodMonths));
        if (firstPaymentDate <= startDate)
            throw new ArgumentException("First payment must follow disbursement.", nameof(firstPaymentDate));
    }

    internal static LocalDate Contractual(LocalDate startDate, int period, LocalDate? firstPaymentDate) =>
        firstPaymentDate is LocalDate first ? first.PlusMonths(period - 1) : startDate.PlusMonths(period);

    internal static LocalDate Payable(LocalDate contractualDate, HolidayCalendar? calendar) =>
        calendar?.NextBusinessDay(contractualDate) ?? contractualDate;
}
