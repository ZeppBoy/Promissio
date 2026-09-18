using NodaTime;
using Promissio.Domain.Calculations;
using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.ScheduleGeneration;

/// <summary>Generates level payments using the same dated accrual as the contractual interest calculator.</summary>
/// <remarks>
/// Finds the smallest cent-denominated payment that covers interest and retires the balance by maturity.
/// Interest-only grace instalments precede amortization; the last payment absorbs the residual.
/// See ADR-0004 and docs/domain/payment-schedules.md for the balance recurrence.
/// </remarks>
public sealed class AnnuityScheduleGenerator : IScheduleGenerator
{
    private readonly IInterestCalculator _interestCalculator;

    /// <summary>Creates a generator using the contractual interest calculator.</summary>
    public AnnuityScheduleGenerator(IInterestCalculator interestCalculator)
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
        LocalDate[] dates = Enumerable.Range(1, termMonths)
            .Select(i => ScheduleDates.Contractual(startDate, i, firstPaymentDate)).ToArray();
        Money payment = FindPayment(principal, interestRate, dates, startDate, gracePeriodMonths);
        Money balance = principal;
        List<PaymentScheduleItem> items = [];
        LocalDate previousDate = startDate;
        for (int i = 0; i < dates.Length; i++)
        {
            Money interest = _interestCalculator.Calculate(balance, interestRate, previousDate, dates[i]);
            if (i >= gracePeriodMonths && i < dates.Length - 1 && payment < interest)
                throw new ArgumentException("This schedule would require negative amortization, which is not supported.", nameof(interestRate));
            Money portion = i < gracePeriodMonths ? Money.Zero(principal.Currency)
                : i == dates.Length - 1 ? balance : PrincipalPortion(payment, interest, balance);
            items.Add(new PaymentScheduleItem(i + 1, ScheduleDates.Payable(dates[i], holidayCalendar),
                portion, interest, portion + interest, dates[i]));
            balance -= portion;
            previousDate = dates[i];
        }
        return items;
    }

    private Money FindPayment(Money principal, InterestRate rate, LocalDate[] dates,
        LocalDate startDate, int grace)
    {
        Money low = Money.Zero(principal.Currency);
        Money high = principal;
        // Paying principal plus the largest full-balance interest charge is an upper bound.
        for (int i = grace; i < dates.Length; i++)
        {
            Money candidate = principal + _interestCalculator.Calculate(principal, rate,
                i == 0 ? startDate : dates[i - 1], dates[i]);
            if (candidate > high)
                high = candidate;
        }
        while ((high - low).Amount > 0.01m)
        {
            Money mid = low + (high - low) / 2m;
            Money balance = principal;
            bool coversInterest = true;
            for (int i = grace; i < dates.Length && balance.Amount > 0; i++)
            {
                Money interest = _interestCalculator.Calculate(balance, rate,
                    i == 0 ? startDate : dates[i - 1], dates[i]);
                if (mid < interest)
                {
                    coversInterest = false;
                    break;
                }
                balance -= PrincipalPortion(mid, interest, balance);
            }
            if (!coversInterest || balance.Amount > 0)
                low = mid;
            else
                high = mid;
        }
        return high;
    }

    private static Money PrincipalPortion(Money payment, Money interest, Money balance)
    {
        Money portion = payment - interest;
        return portion.Amount < 0 ? Money.Zero(balance.Currency) : portion > balance ? balance : portion;
    }
}
