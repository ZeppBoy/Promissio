using System;
using System.Collections.Generic;
using NodaTime;
using Promissio.Domain.Calculations;
using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.ScheduleGeneration;

/// <summary>
/// Generates payment schedules from predefined cash flows.
/// Useful for loans with irregular payment patterns.
/// </summary>
public class CustomScheduleGenerator : IScheduleGenerator
{
    private readonly List<CustomCashFlow> _customFlows;

    public CustomScheduleGenerator(List<CustomCashFlow> customFlows)
    {
        _customFlows = customFlows ?? throw new ArgumentNullException(nameof(customFlows));
    }

    public IEnumerable<PaymentScheduleItem> Generate(
        Money principal,
        InterestRate interestRate,
        int termMonths,
        LocalDate startDate,
        int gracePeriodMonths = 0)
    {
        if (gracePeriodMonths < 0)
            throw new ArgumentException("Grace period cannot be negative.", nameof(gracePeriodMonths));

        if (gracePeriodMonths >= termMonths)
            throw new ArgumentException("Grace period must be less than total term.", nameof(gracePeriodMonths));

        if (principal.Amount <= 0)
            throw new ArgumentException("Principal must be positive.", nameof(principal));

        if (interestRate.Rate.Fraction < 0)
            throw new ArgumentException("Interest rate must be non-negative.", nameof(interestRate));

        if (termMonths <= 0)
            throw new ArgumentException("Term must be positive.", nameof(termMonths));

        // Custom schedules are defined by the caller; gracePeriodMonths is validated but not enforced
        // since the caller provides the exact cash flows.

        var currency = principal.Currency;
        var items = new List<PaymentScheduleItem>();

        for (int i = 1; i <= termMonths; i++)
        {
            var paymentDate = startDate.PlusMonths(i);

            if (i <= _customFlows.Count)
            {
                var flow = _customFlows[i - 1];
                items.Add(new PaymentScheduleItem(
                    i, paymentDate, flow.PrincipalPortion, flow.InterestPortion,
                    flow.PrincipalPortion + flow.InterestPortion));
            }
            else
            {
                items.Add(new PaymentScheduleItem(
                    i, paymentDate, Money.Zero(currency), Money.Zero(currency), Money.Zero(currency)));
            }
        }

        return items;
    }

    public record CustomCashFlow(
        Money PrincipalPortion,
        Money InterestPortion);
}
