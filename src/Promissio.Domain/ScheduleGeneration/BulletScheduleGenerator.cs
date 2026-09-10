using NodaTime;
using Promissio.Domain.Calculations;
using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.ScheduleGeneration;

/// <summary>Generates interest-only instalments with the entire principal due at maturity.</summary>
/// <remarks>Accrual uses contractual dates; holidays move only payment dates. See ADR-0004.</remarks>
public sealed class BulletScheduleGenerator : IScheduleGenerator
{
    private readonly IInterestCalculator _interestCalculator;

    /// <summary>Creates a generator using the contractual interest calculator.</summary>
    public BulletScheduleGenerator(IInterestCalculator interestCalculator)
    {
        ArgumentNullException.ThrowIfNull(interestCalculator);
        _interestCalculator = interestCalculator;
    }

    /// <inheritdoc />
    public IEnumerable<PaymentScheduleItem> Generate(Money principal, InterestRate interestRate,
        int termMonths, LocalDate startDate, int gracePeriodMonths = 0,
        HolidayCalendar? holidayCalendar = null, LocalDate? firstPaymentDate = null)
    {
        ScheduleDates.Validate(principal, interestRate, termMonths, startDate, gracePeriodMonths, firstPaymentDate);
        List<PaymentScheduleItem> items = [];
        Money balance = principal;
        LocalDate previousDate = startDate;
        for (int i = 1; i <= termMonths; i++)
        {
            LocalDate contractualDate = ScheduleDates.Contractual(startDate, i, firstPaymentDate);
            Money interest = _interestCalculator.Calculate(balance, interestRate, previousDate, contractualDate);
            Money principalPortion = i == termMonths ? balance : Money.Zero(principal.Currency);
            items.Add(new PaymentScheduleItem(i, ScheduleDates.Payable(contractualDate, holidayCalendar),
                principalPortion, interest, principalPortion + interest, contractualDate));
            balance -= principalPortion;
            previousDate = contractualDate;
        }
        return items;
    }
}
